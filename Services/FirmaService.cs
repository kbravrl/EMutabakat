using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMutabakat.Services
{
    public class FirmaService : IFirmaService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FirmaService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Firma>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return new List<Firma>();

            var mail = user.Identity?.Name;

            if (string.IsNullOrWhiteSpace(mail))
                return new List<Firma>();

            var kullanici = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            if (kullanici == null)
                return new List<Firma>();

            var allowedIds = kullanici.Firmalar
              .Select(f => f.FirmaId)
              .Distinct()
              .ToList();

            if (allowedIds.Count == 0)
                return new List<Firma>();

            return await context.Firmalar
                .Where(f => allowedIds.Contains(f.FirmaId))
                .AsNoTracking()
                .OrderBy(x => x.FirmaAdi)
                .ToListAsync();

        }

        public async Task<Firma?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var kullanici = await GetCurrentKullaniciAsync(context);

            if (kullanici == null)
                return null;

            if (kullanici.Rol == KullaniciRolleri.Admin)
            {
                return await context.Firmalar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FirmaId == id);
            }

            return await context.Firmalar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaId == id);
        }

        public async Task<Firma> AddAsync(Firma firma)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var current = await GetCurrentKullaniciAsync(context);
            if (current == null)
                throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

            if (current.Rol != KullaniciRolleri.Admin)
                throw new UnauthorizedAccessException("Yalnızca adminler firma ekleyebilir.");

            try
            {
                firma.FirmaMail = firma.FirmaMail?.Trim().ToLower();
                firma.FirmaSmtpUser = firma.FirmaSmtpUser?.Trim().ToLower();

                context.Firmalar.Add(firma);
                await context.SaveChangesAsync();

                var currentUser = await context.Kullanicilar
                    .Include(k => k.Firmalar)
                    .FirstOrDefaultAsync(k => k.KullaniciId == current.KullaniciId);

                if (currentUser != null &&
                    !currentUser.Firmalar.Any(f => f.FirmaId == firma.FirmaId))
                {
                    currentUser.Firmalar.Add(firma);
                    await context.SaveChangesAsync();
                }

                await _logService.AddAsync(
                    "Bilgi",
                    "Firma",
                    $"Yeni firma eklendi. Firma Id: {firma.FirmaId}, Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return firma;
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma ekleme hatası. Firma Adı: {firma.FirmaAdi}, Hata: {ex.Message}",
                    GetUserEmail()
                );

                throw;
            }
        }

        public async Task<Firma?> UpdateAsync(Firma firma)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                var existingFirma = await context.Firmalar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FirmaId == firma.FirmaId);

                if (existingFirma == null)
                    return null;

                var kullanici = await GetCurrentKullaniciAsync(context);

                if (kullanici == null)
                    return null;

                if (kullanici.Rol != KullaniciRolleri.Admin)
                {
                    var allowedIds = kullanici.Firmalar.Select(f => f.FirmaId).ToList();
                    if (!allowedIds.Contains(firma.FirmaId))
                        return null;
                }

                firma.FirmaMail = firma.FirmaMail?.Trim().ToLower();
                firma.FirmaSmtpUser = firma.FirmaSmtpUser?.Trim().ToLower();

                var updated = await context.Firmalar
                    .Where(x => x.FirmaId == firma.FirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.FirmaAdi, firma.FirmaAdi)
                        .SetProperty(x => x.FirmaUnvan, firma.FirmaUnvan)
                        .SetProperty(x => x.FirmaAdres, firma.FirmaAdres)
                        .SetProperty(x => x.FirmaIlce, firma.FirmaIlce)
                        .SetProperty(x => x.FirmaIl, firma.FirmaIl)
                        .SetProperty(x => x.FirmaVergiDairesi, firma.FirmaVergiDairesi)
                        .SetProperty(x => x.FirmaVergiNumarasi, firma.FirmaVergiNumarasi)
                        .SetProperty(x => x.FirmaMersisNumarasi, firma.FirmaMersisNumarasi)
                        .SetProperty(x => x.FirmaWebAdresi, firma.FirmaWebAdresi)
                        .SetProperty(x => x.FirmaYetkiliAdiSoyadi, firma.FirmaYetkiliAdiSoyadi)
                        .SetProperty(x => x.FirmaMail, firma.FirmaMail) // 🔥 normalized
                        .SetProperty(x => x.FirmaTelefon, firma.FirmaTelefon)
                        .SetProperty(x => x.FirmaGsm, firma.FirmaGsm)
                        .SetProperty(x => x.FirmaSmtpHost, firma.FirmaSmtpHost)
                        .SetProperty(x => x.FirmaSmtpPort, firma.FirmaSmtpPort)
                        .SetProperty(x => x.FirmaSmtpUser, firma.FirmaSmtpUser) // 🔥 normalized
                        .SetProperty(x => x.FirmaSmtpPassword, firma.FirmaSmtpPassword)
                        .SetProperty(x => x.FirmaSmtpSecure, firma.FirmaSmtpSecure)
                        .SetProperty(x => x.FirmaAktifPasif, firma.FirmaAktifPasif));

                if (updated == 0)
                    return null;

                await _logService.AddAsync(
                    "Uyarı",
                    "Firma",
                    $"Firma güncellendi. Firma Id: {firma.FirmaId}, Firma Adı: {existingFirma.FirmaAdi} -> {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return await GetByIdAsync(firma.FirmaId);
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma güncelleme hatası. Firma Id: {firma.FirmaId}, Hata: {ex.Message}",
                    GetUserEmail()
                );

                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var firma = await context.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);

            if (firma == null)
                return false;

            var kullanici = await GetCurrentKullaniciAsync(context);

            if (kullanici == null)
                return false;

            if (kullanici.Rol != KullaniciRolleri.Admin)
            {
                return false;
            }

            try
            {
                context.Firmalar.Remove(firma);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Firma",
                    $"Firma silindi. Firma Id: {firma.FirmaId}, Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return true;
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma silinemedi: Firma Id: {firma.FirmaId} Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );
                throw new Exception("Bu firma kaydı başka kayıtlarda kullanıldığı için silinemez.");
            }
        }

        private async Task<Kullanici?> GetCurrentKullaniciAsync(AppDbContext context)
        {
            var mail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(mail))
                return null;

            return await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);
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