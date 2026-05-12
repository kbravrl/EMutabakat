using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EMutabakat.Services
{
    public class DovizKoduService : IDovizKoduService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DovizKoduService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<DovizKodu>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.DovizKodlari
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<List<DovizKodu>> GetActiveAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.DovizKodlari
                .AsNoTracking()
                .Where(x => x.DovizKoduAktifPasif == 1)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<DovizKodu?> GetByTcmbAsync(string tcmb)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalized = (tcmb ?? string.Empty).Trim().ToUpperInvariant();
            return await context.DovizKodlari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TCMB == normalized);
        }



        public async Task<DovizKodu> AddAsync(DovizKodu dovizKodu)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            dovizKodu.TCMB = (dovizKodu.TCMB ?? string.Empty).Trim().ToUpperInvariant();
            dovizKodu.Name = (dovizKodu.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(dovizKodu.TCMB))
                throw new Exception("TCMB kodu zorunludur.");

            if (string.IsNullOrWhiteSpace(dovizKodu.Name))
                throw new Exception("Döviz adı zorunludur.");

            if (dovizKodu.DovizKoduAktifPasif != 0 && dovizKodu.DovizKoduAktifPasif != 1)
                throw new Exception("Aktif/Pasif değeri geçersiz.");

            if (dovizKodu.DovizKoduAktifPasif != 0 && dovizKodu.DovizKoduAktifPasif != 1)
                throw new Exception("Aktif/Pasif değeri geçersiz.");

            var exists = await context.DovizKodlari.AnyAsync(x => x.TCMB == dovizKodu.TCMB);
            if (exists)
                throw new Exception("Bu TCMB kodu zaten mevcut.");

            context.DovizKodlari.Add(dovizKodu);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "DovizKodu",
                $"Yeni döviz kodu eklendi | TCMB: {dovizKodu.TCMB}, Adı: {dovizKodu.Name}",
                GetUserEmail()
            );

            return dovizKodu;
        }

        public async Task<DovizKodu?> UpdateAsync(DovizKodu dovizKodu, string originalTcmb)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedOriginal = (originalTcmb ?? string.Empty).Trim().ToUpperInvariant();
            dovizKodu.TCMB = (dovizKodu.TCMB ?? string.Empty).Trim().ToUpperInvariant();
            dovizKodu.Name = (dovizKodu.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(dovizKodu.TCMB))
                throw new Exception("TCMB kodu zorunludur.");

            if (string.IsNullOrWhiteSpace(dovizKodu.Name))
                throw new Exception("Döviz adı zorunludur.");

            var existing = await context.DovizKodlari
                .FirstOrDefaultAsync(x => x.TCMB == normalizedOriginal);

            if (existing == null)
                return null;

            if (!string.Equals(normalizedOriginal, dovizKodu.TCMB, StringComparison.Ordinal))
            {
                var newCodeExists = await context.DovizKodlari.AnyAsync(x => x.TCMB == dovizKodu.TCMB);
                if (newCodeExists)
                    throw new Exception("Bu TCMB kodu zaten mevcut.");

                var newEntity = new DovizKodu
                {
                    TCMB = dovizKodu.TCMB,
                Name = dovizKodu.Name,
                DovizKoduAktifPasif = dovizKodu.DovizKoduAktifPasif
                };

                context.DovizKodlari.Add(newEntity);
                await context.SaveChangesAsync();

                await context.Cariler
                    .Where(c => c.CariDovizKodu == normalizedOriginal)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.CariDovizKodu, dovizKodu.TCMB));

                await context.Mutabakatlar
                    .Where(m => m.MutabakatDovizKodu == normalizedOriginal)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.MutabakatDovizKodu, dovizKodu.TCMB));

                await context.SilinenMutabakatlar
                    .Where(sm => sm.MutabakatDovizKodu == normalizedOriginal)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(sm => sm.MutabakatDovizKodu, dovizKodu.TCMB));

                context.DovizKodlari.Remove(existing);
                await context.SaveChangesAsync();

                await _logService.AddChangeAsync(
                    "DovizKodu",
                    $"TCMB: {dovizKodu.TCMB}",
                    new
                    {
                        TCMB = normalizedOriginal,
                        existing.Name,
                        existing.DovizKoduAktifPasif
                    },
                    new
                    {
                        dovizKodu.TCMB,
                        dovizKodu.Name,
                        dovizKodu.DovizKoduAktifPasif
                    },
                    GetUserEmail()
                );

                return await context.DovizKodlari
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TCMB == dovizKodu.TCMB);
            }

            var oldSnapshot = new { existing.Name, existing.DovizKoduAktifPasif };

            existing.Name = dovizKodu.Name;
            existing.DovizKoduAktifPasif = dovizKodu.DovizKoduAktifPasif;
            await context.SaveChangesAsync();

            await _logService.AddChangeAsync(
                "DovizKodu",
                $"TCMB: {dovizKodu.TCMB}",
                oldSnapshot,
                new { dovizKodu.Name, dovizKodu.DovizKoduAktifPasif },
                GetUserEmail()
            );

            return existing;
        }

        public async Task<bool> DeleteAsync(string tcmb)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalized = (tcmb ?? string.Empty).Trim().ToUpperInvariant();

            var entity = await context.DovizKodlari
                .FirstOrDefaultAsync(x => x.TCMB == normalized);

            if (entity == null)
                return false;

            try
            {
                context.DovizKodlari.Remove(entity);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "DovizKodu",
                    $"Döviz kodu silindi | TCMB: {entity.TCMB}, Adı: {entity.Name}",
                    GetUserEmail()
                );

                return true;
            }
            catch (Exception ex)
            {
                var detail = ex.Message;
                var inner = ex.InnerException;
                while (inner != null) { detail += " → " + inner.Message; inner = inner.InnerException; }

                await _logService.AddAsync(
                    "Hata",
                    "DovizKodu",
                    $"Döviz kodu silinemedi | TCMB: {entity.TCMB}, Adı: {entity.Name}",
                    GetUserEmail(),
                    detail
                );

                throw new Exception("Bu döviz kodu başka kayıtlarda kullanıldığı için silinemez.");
            }
        }

        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .Name;
        }
    }
}