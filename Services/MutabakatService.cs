using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static EMutabakat.Models.Mutabakat;

namespace EMutabakat.Services
{
    public class MutabakatService : IMutabakatService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly ISdService _sdService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogService _logService;

        public MutabakatService(
            IDbContextFactory<AppDbContext> contextFactory,
            IEmailService emailService,
            ISdService sdService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogService logService)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _sdService = sdService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logService = logService;
        }

        public async Task<List<Mutabakat>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var query = context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .ThenInclude(c => c.CariGrup)
                .Include(x => x.DovizKodu)
                .OrderByDescending(x => x.MutabakatTarihi)
                .ThenByDescending(x => x.MutabakatId)
                .AsQueryable();

            if (allowedFirmaIds != null)
            {
                if (allowedFirmaIds.Count == 0)
                    return new List<Mutabakat>();

                query = query.Where(x => allowedFirmaIds.Contains(x.FirmaId));
            }

            return await query.ToListAsync();
        }

        public async Task<Mutabakat?> GetByIdAsync(string id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var mutabakat = await context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.MutabakatId == id);

            if (mutabakat == null) return null;

            if (allowedFirmaIds != null && !allowedFirmaIds.Contains(mutabakat.FirmaId))
                return null;

            return mutabakat;
        }

        public async Task<Mutabakat> AddAsync(Mutabakat mutabakat, string? cariMail = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            mutabakat.MutabakatId = (mutabakat.MutabakatId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatId))
                throw new Exception("Mutabakat ID zorunludur.");

            mutabakat.CariId = (mutabakat.CariId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.CariId))
                throw new Exception("Cari seçimi zorunludur.");

            if (mutabakat.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            var cariEntity = await context.Cariler
                .FirstOrDefaultAsync(c => c.CariId == mutabakat.CariId && c.FirmaId == mutabakat.FirmaId);

            if (cariEntity == null)
                throw new Exception("Seçilen firma ve cari eşleşen kaydı bulunamadı.");

            if (!string.IsNullOrWhiteSpace(cariMail))
            {
                cariEntity.CariYetkiliMail = cariMail.Trim().ToLowerInvariant();
            }

            mutabakat.MutabakatTarihi = DateTime.SpecifyKind(
                mutabakat.MutabakatTarihi.Date,
                DateTimeKind.Utc);

            mutabakat.MutabakatDovizKodu = (mutabakat.MutabakatDovizKodu ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                mutabakat.MutabakatDovizKodu = cariEntity.CariDovizKodu ?? "TL";

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                throw new Exception("Döviz kodu zorunludur.");

            var dovizExists = await context.DovizKodlari
                .AnyAsync(x => x.TCMB == mutabakat.MutabakatDovizKodu);

            if (!dovizExists)
                throw new Exception("Geçerli bir döviz kodu seçiniz.");

            mutabakat.MutabakatBakiye = Math.Abs(mutabakat.MutabakatBakiye);

            mutabakat.MutabakatAciklama = string.IsNullOrWhiteSpace(mutabakat.MutabakatAciklama)
                ? null
                : mutabakat.MutabakatAciklama.Trim();

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");

            mutabakat.Status = MutabakatStatus.Kaydedildi;

            var existingMutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x =>
                    x.FirmaId == mutabakat.FirmaId &&
                    x.CariId == mutabakat.CariId &&
                    x.MutabakatTarihi == mutabakat.MutabakatTarihi);

            if (existingMutabakat != null)
            {
                if (existingMutabakat.MutabakatBakiye == mutabakat.MutabakatBakiye)
                    throw new Exception("Aynı firma, cari, tarih ve bakiye için kayıt zaten mevcut.");

                await ArchiveDeletedReconciliationAsync(context, existingMutabakat);

                await _logService.AddAsync(
                    "Uyarı",
                    "Mutabakat",
                    $"Eski mutabakat arşivlendi ve silindi. MutabakatId: {existingMutabakat.MutabakatId}, CariId: {existingMutabakat.CariId}, FirmaId: {existingMutabakat.FirmaId}",
                    GetUserEmail()
                );

                context.Mutabakatlar.Remove(existingMutabakat);
                await context.SaveChangesAsync();
            }

            var duplicateMutabakatId = await context.Mutabakatlar
                .AnyAsync(x => x.MutabakatId == mutabakat.MutabakatId);

            if (duplicateMutabakatId)
                throw new Exception("Bu Mutabakat ID zaten mevcut.");

            context.Mutabakatlar.Add(mutabakat);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Yeni mutabakat eklendi. MutabakatId: {mutabakat.MutabakatId}, CariId: {mutabakat.CariId}, FirmaId: {mutabakat.FirmaId}, Tarih: {mutabakat.MutabakatTarihi:yyyy-MM-dd}, Bakiye: {mutabakat.MutabakatBakiye}",
                GetUserEmail()
            );

            return mutabakat;
        }

        public async Task<Mutabakat?> UpdateAsync(Mutabakat mutabakat, string? cariMail = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            mutabakat.MutabakatId = (mutabakat.MutabakatId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatId))
                throw new Exception("Mutabakat ID zorunludur.");

            mutabakat.CariId = (mutabakat.CariId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.CariId))
                throw new Exception("Cari seçimi zorunludur.");

            if (mutabakat.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            var cariEntity = await context.Cariler
                .FirstOrDefaultAsync(c => c.CariId == mutabakat.CariId && c.FirmaId == mutabakat.FirmaId);

            if (cariEntity == null)
                throw new Exception("Seçilen firma ve cari eşleşen kaydı bulunamadı.");

            if (!string.IsNullOrWhiteSpace(cariMail))
            {
                cariEntity.CariYetkiliMail = cariMail.Trim().ToLowerInvariant();
            }

            var newFirmaId = mutabakat.FirmaId;

            mutabakat.MutabakatTarihi = DateTime.SpecifyKind(
                mutabakat.MutabakatTarihi.Date,
                DateTimeKind.Utc);

            mutabakat.MutabakatDovizKodu = (mutabakat.MutabakatDovizKodu ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                mutabakat.MutabakatDovizKodu = cariEntity.CariDovizKodu ?? "TL";

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                throw new Exception("Döviz kodu zorunludur.");

            var dovizExists = await context.DovizKodlari
                .AnyAsync(x => x.TCMB == mutabakat.MutabakatDovizKodu);

            if (!dovizExists)
                throw new Exception("Geçerli bir döviz kodu seçiniz.");

            mutabakat.MutabakatBakiye = Math.Abs(mutabakat.MutabakatBakiye);

            var lookupMutabakatId = string.IsNullOrWhiteSpace(mutabakat.OriginalMutabakatId)
                ? mutabakat.MutabakatId
                : mutabakat.OriginalMutabakatId.Trim();

            var existingMutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatId == lookupMutabakatId);

            if (existingMutabakat == null)
                return null;

            if (!string.Equals(mutabakat.MutabakatId, lookupMutabakatId, StringComparison.OrdinalIgnoreCase))
            {
                var duplicateMutabakatId = await context.Mutabakatlar
                    .AnyAsync(x => x.MutabakatId == mutabakat.MutabakatId);

                if (duplicateMutabakatId)
                    throw new Exception("Bu Mutabakat ID zaten mevcut.");
            }

            var anotherRecordExists = await context.Mutabakatlar.AnyAsync(x =>
                x.MutabakatId != lookupMutabakatId &&
                x.FirmaId == newFirmaId &&
                x.CariId == mutabakat.CariId &&
                x.MutabakatTarihi == mutabakat.MutabakatTarihi);

            if (anotherRecordExists)
                throw new Exception("Aynı firma, tarih ve cari için başka bir mutabakat kaydı zaten mevcut.");

            var oldMutabakatId = existingMutabakat.MutabakatId;
            var oldCariId = existingMutabakat.CariId;
            var oldFirmaId = existingMutabakat.FirmaId;
            var oldTarih = existingMutabakat.MutabakatTarihi;
            var oldBakiye = existingMutabakat.MutabakatBakiye;
            var oldDoviz = existingMutabakat.MutabakatDovizKodu;
            var oldBakiyeTipi = existingMutabakat.MutabakatBakiyeTipi;
            var oldAciklama = existingMutabakat.MutabakatAciklama;

            var keyChanged =
                existingMutabakat.FirmaId != newFirmaId ||
                existingMutabakat.CariId != mutabakat.CariId ||
                existingMutabakat.MutabakatTarihi != mutabakat.MutabakatTarihi;

            if (!keyChanged)
            {
                existingMutabakat.MutabakatId = mutabakat.MutabakatId;
                existingMutabakat.MutabakatDovizKodu = mutabakat.MutabakatDovizKodu;
                existingMutabakat.MutabakatBakiye = mutabakat.MutabakatBakiye;
                existingMutabakat.MutabakatBakiyeTipi = mutabakat.MutabakatBakiyeTipi;
                existingMutabakat.MutabakatAciklama = string.IsNullOrWhiteSpace(mutabakat.MutabakatAciklama)
                    ? null
                    : mutabakat.MutabakatAciklama.Trim();

                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Mutabakat",
                    $"Mutabakat güncellendi. MutabakatId: {oldMutabakatId}->{existingMutabakat.MutabakatId}, CariId: {oldCariId}->{existingMutabakat.CariId}, FirmaId: {oldFirmaId}->{existingMutabakat.FirmaId}, Tarih: {oldTarih:yyyy-MM-dd}->{existingMutabakat.MutabakatTarihi:yyyy-MM-dd}, Döviz: {oldDoviz}->{existingMutabakat.MutabakatDovizKodu}, BakiyeTipi: {oldBakiyeTipi}->{existingMutabakat.MutabakatBakiyeTipi}, Bakiye: {oldBakiye}->{existingMutabakat.MutabakatBakiye}, Açıklama: {oldAciklama}->{existingMutabakat.MutabakatAciklama}",
                    GetUserEmail()
                );

                return existingMutabakat;
            }

            var newMutabakat = new Mutabakat
            {
                MutabakatId = mutabakat.MutabakatId,
                FirmaId = newFirmaId,
                CariId = mutabakat.CariId,
                MutabakatTarihi = mutabakat.MutabakatTarihi,
                MutabakatDovizKodu = mutabakat.MutabakatDovizKodu,
                MutabakatBakiye = mutabakat.MutabakatBakiye,
                MutabakatBakiyeTipi = mutabakat.MutabakatBakiyeTipi,
                MutabakatAciklama = string.IsNullOrWhiteSpace(mutabakat.MutabakatAciklama)
                    ? null
                    : mutabakat.MutabakatAciklama.Trim(),
                MutabakatToken = existingMutabakat.MutabakatToken,
                Status = existingMutabakat.Status,
                MutabakatGonderimTarihSaat = existingMutabakat.MutabakatGonderimTarihSaat,
                MutabakatCevapTarihSaat = existingMutabakat.MutabakatCevapTarihSaat,
                MutabakatCevapMail = existingMutabakat.MutabakatCevapMail,
                MutabakatCevapAdSoyad = existingMutabakat.MutabakatCevapAdSoyad,
                MutabakatCevapGsm = existingMutabakat.MutabakatCevapGsm,
                MutabakatCevapAciklama = existingMutabakat.MutabakatCevapAciklama,
                MutabakatReceiveStoragePath = existingMutabakat.MutabakatReceiveStoragePath
            };

            context.Mutabakatlar.Remove(existingMutabakat);
            await context.SaveChangesAsync();

            context.Mutabakatlar.Add(newMutabakat);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Uyarı",
                "Mutabakat",
                $"Mutabakat anahtar alanlarıyla birlikte güncellendi. MutabakatId: {oldMutabakatId}->{newMutabakat.MutabakatId}, CariId: {oldCariId}->{newMutabakat.CariId}, FirmaId: {oldFirmaId}->{newMutabakat.FirmaId}, Tarih: {oldTarih:yyyy-MM-dd}->{newMutabakat.MutabakatTarihi:yyyy-MM-dd}, Döviz: {oldDoviz}->{newMutabakat.MutabakatDovizKodu}, BakiyeTipi: {oldBakiyeTipi}->{newMutabakat.MutabakatBakiyeTipi}, Bakiye: {oldBakiye}->{newMutabakat.MutabakatBakiye}, Açıklama: {oldAciklama}->{newMutabakat.MutabakatAciklama}",
                GetUserEmail()
            );

            return newMutabakat;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatId == id);

            if (mutabakat == null)
                return false;

            var filePath = mutabakat.MutabakatReceiveStoragePath;

            context.Mutabakatlar.Remove(mutabakat);
            await context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                await _sdService.DeleteMutabakatResponseFileAsync(filePath);
            }

            await _logService.AddAsync(
                "Uyarı",
                "Mutabakat",
                $"Mutabakat silindi. MutabakatId: {mutabakat.MutabakatId}, CariId: {mutabakat.CariId}, FirmaId: {mutabakat.FirmaId}",
                GetUserEmail()
            );

            return true;
        }

        public async Task<List<SilinenMutabakat>> GetAllDeletedAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.SilinenMutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .OrderByDescending(x => x.SilinmeTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<SilinenMutabakat?> GetDeletedByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.SilinenMutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        private static async Task ArchiveDeletedReconciliationAsync(
            AppDbContext context,
            Mutabakat existing)
        {
            var silinen = new SilinenMutabakat
            {
                MutabakatId = existing.MutabakatId,
                MutabakatTarihi = existing.MutabakatTarihi,
                FirmaId = existing.FirmaId,
                CariId = existing.CariId,
                MutabakatDovizKodu = existing.MutabakatDovizKodu,
                MutabakatBakiye = existing.MutabakatBakiye,
                MutabakatBakiyeTipi = existing.MutabakatBakiyeTipi,
                MutabakatAciklama = existing.MutabakatAciklama,
                MutabakatGonderimTarihSaat = existing.MutabakatGonderimTarihSaat == default(DateTime)
                    ? null
                    : existing.MutabakatGonderimTarihSaat,
                Status = existing.Status,
                MutabakatCevapTarihSaat = existing.MutabakatCevapTarihSaat,
                MutabakatCevapMail = existing.MutabakatCevapMail,
                MutabakatCevapAdSoyad = existing.MutabakatCevapAdSoyad,
                MutabakatCevapGsm = existing.MutabakatCevapGsm,
                MutabakatCevapAciklama = existing.MutabakatCevapAciklama,
                MutabakatToken = existing.MutabakatToken,
                MutabakatReceiveStoragePath = existing.MutabakatReceiveStoragePath,
                SilinmeTarihi = DateTime.UtcNow,
            };

            await context.SilinenMutabakatlar.AddAsync(silinen);
        }

        public async Task<bool> DeleteDeletedAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var silinenMutabakat = await context.SilinenMutabakatlar
                .FirstOrDefaultAsync(x => x.Id == id);

            if (silinenMutabakat == null)
                return false;

            var filePath = silinenMutabakat.MutabakatReceiveStoragePath;

            context.SilinenMutabakatlar.Remove(silinenMutabakat);
            await context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                await _sdService.DeleteMutabakatResponseFileAsync(filePath);
            }

            await _logService.AddAsync(
                "Uyarı",
                "SilinenMutabakat",
                $"Silinen mutabakat kaydı kalıcı olarak silindi. Id: {silinenMutabakat.Id}, MutabakatId: {silinenMutabakat.MutabakatId}",
                GetUserEmail()
            );

            return true;
        }

        public async Task<bool> SendMailAsync(string mutabakatId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (mutabakat.Status == MutabakatStatus.Mutabik ||
                    mutabakat.Status == MutabakatStatus.MutabikDegil)
                    return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var email = user.Identity.Name;

            var kullanici = await context.Kullanicilar
                .Include(x => x.Firmalar)
                .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null)
                return false;

            var firma = kullanici.Firmalar.FirstOrDefault(f => f.FirmaId == mutabakat.FirmaId)
                       ?? kullanici.Firmalar.FirstOrDefault();

            if (firma == null)
                return false;

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7017";

            var approveUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=reject";

            var ok = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                kullanici,
                approveUrl,
                rejectUrl,
                false);

            if (!ok)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Mutabakat",
                    $"Mutabakat mail gönderimi başarısız. MutabakatId: {mutabakat.MutabakatId}, CariMail: {mutabakat.Cari?.CariYetkiliMail}",
                    GetUserEmail()
                );

                return false;
            }

            mutabakat.MutabakatGonderimTarihSaat = DateTime.SpecifyKind(
                DateTime.UtcNow.Date,
                DateTimeKind.Utc);
            mutabakat.Status = MutabakatStatus.Gonderildi;

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Mutabakat maili gönderildi. MutabakatId: {mutabakat.MutabakatId}, CariId: {mutabakat.CariId}",
                GetUserEmail()
            );

            return true;
        }

        public async Task<bool> SendReminderAsync(string mutabakatId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (mutabakat.Status == MutabakatStatus.Mutabik ||
                   mutabakat.Status == MutabakatStatus.MutabikDegil)
                   return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var email = user.Identity.Name;

            var kullanici = await context.Kullanicilar
               .Include(x => x.Firmalar)
               .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null)
                return false;

            var firma = kullanici.Firmalar.FirstOrDefault(f => f.FirmaId == mutabakat.FirmaId)
                       ?? kullanici.Firmalar.FirstOrDefault();

            if (firma == null)
                return false;

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7017";

            var approveUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=reject";

            var ok = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                kullanici,
                approveUrl,
                rejectUrl,
                true);

            if (!ok)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Mutabakat",
                    $"Mutabakat hatırlatma maili gönderilemedi. MutabakatId: {mutabakat.MutabakatId}, CariMail: {mutabakat.Cari?.CariYetkiliMail}",
                    GetUserEmail()
                );

                return false;
            }

            mutabakat.MutabakatGonderimTarihSaat = DateTime.SpecifyKind(
                DateTime.UtcNow.Date,
                DateTimeKind.Utc);
            mutabakat.Status = MutabakatStatus.Hatirlatma;

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Mutabakat hatırlatma maili gönderildi. MutabakatId: {mutabakat.MutabakatId}, CariId: {mutabakat.CariId}",
                GetUserEmail()
            );

            return true;
        }

        public async Task<(int successCount, int failCount, List<string> errors)> SendPendingMailsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;

            var pendingMutabakatlar = await context.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Where(x =>
                     x.Status == MutabakatStatus.Kaydedildi)
                .ToListAsync();

            foreach (var item in pendingMutabakatlar)
            {
                try
                {
                    var result = await SendMailAsync(item.MutabakatId);

                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"MutabakatId {item.MutabakatId}: mail gönderilemedi.");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"MutabakatId {item.MutabakatId}: {ex.Message}");
                }
            }

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Toplu gönderim tamamlandı. Başarılı: {successCount}, Hatalı: {failCount}",
                GetUserEmail()
            );

            return (successCount, failCount, errors);
        }

        public async Task<(int successCount, int failCount, List<string> errors)> SendSelectedMailsAsync(List<string> mutabakatIds)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;

            if (mutabakatIds == null || !mutabakatIds.Any())
                return (0, 0, errors);

            var selectedMutabakatlar = await context.Mutabakatlar
    .Include(x => x.Firma)
    .Include(x => x.Cari)
    .Where(x =>
        mutabakatIds.Contains(x.MutabakatId) &&
        x.Status == Mutabakat.MutabakatStatus.Kaydedildi)
    .ToListAsync();

            foreach (var item in selectedMutabakatlar)
            {
                try
                {
                    var result = await SendMailAsync(item.MutabakatId);

                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"MutabakatId {item.MutabakatId}: mail gönderilemedi.");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"MutabakatId {item.MutabakatId}: {ex.Message}");
                }
            }

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Seçili mutabakatların toplu gönderimi tamamlandı. Başarılı: {successCount}, Hatalı: {failCount}",
                GetUserEmail()
            );

            return (successCount, failCount, errors);
        }

        public async Task<Mutabakat?> GetByTokenAsync(string token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var mutabakat = await context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null) return null;

            if (allowedFirmaIds != null && !allowedFirmaIds.Contains(mutabakat.FirmaId))
                return null;

            return mutabakat;
        }

        private async Task<List<int>?> GetAllowedFirmaIdsAsync(AppDbContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            var mail = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(mail))
                return null;

            var kullanici = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            if (kullanici == null)
                return null;


            return kullanici.Firmalar
                .Select(uf => uf.FirmaId)
                .Distinct()
                .Where(i => i > 0)
                .ToList();
        }

        public async Task<bool> ApproveAsync(string token, string mail, string adSoyad, string gsm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null)
                return false;

            if (mutabakat.Status == MutabakatStatus.Mutabik ||
                mutabakat.Status == MutabakatStatus.MutabikDegil)
                return false;

            mutabakat.Status = MutabakatStatus.Mutabik;
            mutabakat.MutabakatCevapTarihSaat = DateTime.SpecifyKind(
                DateTime.UtcNow.Date,
                DateTimeKind.Utc);
            mutabakat.MutabakatCevapMail = mail;
            mutabakat.MutabakatCevapAdSoyad = adSoyad;
            mutabakat.MutabakatCevapGsm = gsm;

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Mutabakat onaylandı. MutabakatId: {mutabakat.MutabakatId}, CevapMail: {mail}",
                mail
            );

            return true;
        }

        public async Task<bool> RejectAsync(string token, string mail, string adSoyad, string gsm, string? aciklama, string? filePath)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null)
                return false;

            if (mutabakat.Status == MutabakatStatus.Mutabik ||
                   mutabakat.Status == MutabakatStatus.MutabikDegil)
                   return false;

            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            mutabakat.Status = MutabakatStatus.MutabikDegil;
            mutabakat.MutabakatCevapTarihSaat = DateTime.SpecifyKind(
                DateTime.UtcNow.Date,
                DateTimeKind.Utc);
            mutabakat.MutabakatCevapMail = mail;
            mutabakat.MutabakatCevapAdSoyad = adSoyad;
            mutabakat.MutabakatCevapGsm = gsm;
            mutabakat.MutabakatCevapAciklama = string.IsNullOrWhiteSpace(aciklama)
                ? null
                : aciklama.Trim();
            mutabakat.MutabakatReceiveStoragePath = filePath;

            if (mutabakat.Cari != null)
            {
                if (!string.IsNullOrWhiteSpace(mail))
                {
                    mutabakat.Cari.CariYetkiliMail = mail;
                }

                if (!string.IsNullOrWhiteSpace(adSoyad))
                {
                    mutabakat.Cari.CariYetkiliAdiSoyadi = adSoyad;
                }

                if (!string.IsNullOrWhiteSpace(gsm))
                {
                    mutabakat.Cari.CariYetkiliGsm = gsm;
                }
            }

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Uyarı",
                "Mutabakat",
                $"Mutabakat reddedildi. MutabakatId: {mutabakat.MutabakatId}, CevapMail: {mail}, Dosya: {filePath}",
                mail
            );

            return true;
        }

        private static decimal ParseDecimalCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return 0m;
            if (cell.CellType == CellType.Numeric) return Convert.ToDecimal(cell.NumericCellValue);
            var s = cell.ToString();
            return decimal.TryParse(s, out var v) ? v : 0m;
        }

        private static DateTime ParseDateCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return DateTime.Today;
            if (cell.CellType == CellType.Numeric)
            {
                if (DateUtil.IsCellDateFormatted(cell)) return cell.DateCellValue ?? DateTime.Today;
                return DateTime.FromOADate(cell.NumericCellValue);
            }
            var s = cell.ToString();
            return DateTime.TryParse(s, out var d) ? d : DateTime.Today;
        }

        private static string? GetStringCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            return cell?.ToString();
        }

        private static int ParseIntCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return 0;
            if (cell.CellType == CellType.Numeric) return Convert.ToInt32(cell.NumericCellValue);
            var s = cell.ToString();
            return int.TryParse(s, out var v) ? v : 0;
        }

        public async Task<(int created, int mailsSent, List<string> errors)> ImportFromExcelAsync(
            Stream stream,
            string fileName,
            bool sendMail)
        {
            var errors = new List<string>();
            var createdCount = 0;
            var mailSentCount = 0;

            await _logService.AddAsync(
                "Bilgi",
                "Mutabakat",
                $"Excel import başladı. Dosya: {fileName}, Mail Gönder: {sendMail}",
                GetUserEmail()
            );

            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                IWorkbook workbook;
                var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
                if (ext == ".xlsx")
                    workbook = new XSSFWorkbook(stream);
                else
                    workbook = new HSSFWorkbook(stream);

                var sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    errors.Add("Excel sayfası bulunamadı.");
                    return (0, 0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    return (0, 0, errors);
                }

                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell == null) continue;

                    var text = cell.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        headerMap[text] = i;
                }

                string[] required = new[] { "CariId", "FirmaId", "MutabakatId", "MutabakatTarihi", "DovizKodu", "MutabakatBakiye", "MutabakatBakiyeTipi" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        if (h == "MutabakatTarihi" && headerMap.ContainsKey("MutabakatDonemi"))
                            continue;

                        if (h == "DovizKodu" && headerMap.ContainsKey("TCMP"))
                            continue;

                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        return (0, 0, errors);
                    }
                }

                await using var tx = await context.Database.BeginTransactionAsync();

                try
                {
                    var importedRecords = new List<Mutabakat>();

                    for (int r = 1; r <= sheet.LastRowNum; r++)
                    {
                        var row = sheet.GetRow(r);
                        if (row == null) continue;

                        try
                        {
                            var cariId = (GetStringCell(row, headerMap["CariId"]) ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(cariId))
                            {
                                errors.Add($"Satır {r + 1}: CariId boş olamaz.");
                                continue;
                            }

                            var firmaIdText = (GetStringCell(row, headerMap["FirmaId"]) ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(firmaIdText) || !int.TryParse(firmaIdText, out var firmaId) || firmaId <= 0)
                            {
                                errors.Add($"Satır {r + 1}: FirmaId geçerli bir sayı olmalıdır.");
                                continue;
                            }

                            var tarihColumn = headerMap.ContainsKey("MutabakatTarihi") ? "MutabakatTarihi" : "MutabakatDonemi";
                            var dovizColumn = headerMap.ContainsKey("DovizKodu") ? "DovizKodu" : "TCMP";

                            var donem = DateTime.SpecifyKind(
                                ParseDateCell(row, headerMap[tarihColumn]).Date,
                                DateTimeKind.Utc);

                            var doviz = (GetStringCell(row, headerMap[dovizColumn]) ?? string.Empty).Trim().ToUpperInvariant();
                            var bakiye = Math.Abs(ParseDecimalCell(row, headerMap["MutabakatBakiye"]));
                            var bakiyeTipi = (GetStringCell(row, headerMap["MutabakatBakiyeTipi"]) ?? "B").Trim();
                            var aciklama = headerMap.ContainsKey("MutabakatAciklama")
                                ? GetStringCell(row, headerMap["MutabakatAciklama"])
                                : string.Empty;

                            var cari = await context.Cariler
                                .FirstOrDefaultAsync(c => c.CariId == cariId && c.FirmaId == firmaId);

                            if (cari == null)
                            {
                                errors.Add($"Satır {r + 1}: Bu FirmaId {firmaId} ve CariId {cariId} ile eşleşen cari bulunamadı.");
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(doviz))
                                doviz = cari.CariDovizKodu ?? "TL";

                            var dovizExists = await context.DovizKodlari.AnyAsync(d => d.TCMB == doviz);
                            if (!dovizExists)
                            {
                                errors.Add($"Satır {r + 1}: DovizKodu/TCMB {doviz} geçerli değil.");
                                continue;
                            }

                            var mutabakatId = (GetStringCell(row, headerMap["MutabakatId"]) ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(mutabakatId))
                            {
                                errors.Add($"Satır {r + 1}: MutabakatId boş olamaz.");
                                continue;
                            }

                            var existing = await context.Mutabakatlar
                                .FirstOrDefaultAsync(m =>
                                    m.FirmaId == firmaId &&
                                    m.CariId == cariId &&
                                    m.MutabakatTarihi == donem);

                            if (existing != null)
                            {
                                if (existing.MutabakatBakiye == bakiye)
                                {
                                    continue;
                                }

                                await ArchiveDeletedReconciliationAsync(context, existing);

                                await _logService.AddAsync(
                                    "Uyarı",
                                    "Mutabakat",
                                    $"Excel import sırasında eski kayıt arşivlendi. MutabakatId: {existing.MutabakatId}, CariId: {existing.CariId}, FirmaId: {existing.FirmaId}",
                                    GetUserEmail()
                                );

                                context.Mutabakatlar.Remove(existing);
                                await context.SaveChangesAsync();
                            }

                            var duplicateIdInDb = await context.Mutabakatlar
                                .AnyAsync(m => m.MutabakatId == mutabakatId);

                            if (duplicateIdInDb)
                            {
                                errors.Add($"Satır {r + 1}: MutabakatId {mutabakatId} zaten mevcut.");
                                continue;
                            }

                            var duplicateIdInBatch = importedRecords.Any(m => m.MutabakatId == mutabakatId);
                            if (duplicateIdInBatch)
                            {
                                errors.Add($"Satır {r + 1}: Excel içinde aynı MutabakatId birden fazla kez kullanılmış ({mutabakatId}).");
                                continue;
                            }

                            var previousImported = importedRecords.FirstOrDefault(m =>
                                m.FirmaId == firmaId &&
                                m.CariId == cariId &&
                                m.MutabakatTarihi == donem);

                            if (previousImported != null)
                            {
                                if (previousImported.MutabakatBakiye == bakiye)
                                {
                                    continue;
                                }

                                var previousTracked = await context.Mutabakatlar
                                    .FirstOrDefaultAsync(m => m.MutabakatId == previousImported.MutabakatId);

                                if (previousTracked != null)
                                {
                                    await ArchiveDeletedReconciliationAsync(context, previousTracked);

                                    context.Mutabakatlar.Remove(previousTracked);
                                    await context.SaveChangesAsync();
                                }

                                importedRecords.Remove(previousImported);
                                createdCount--;
                            }

                            var mutabakat = new Mutabakat
                            {
                                MutabakatId = mutabakatId,
                                FirmaId = firmaId,
                                CariId = cariId,
                                MutabakatTarihi = donem,
                                MutabakatDovizKodu = doviz,
                                MutabakatBakiye = bakiye,
                                MutabakatBakiyeTipi = bakiyeTipi,
                                MutabakatAciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                                MutabakatToken = Guid.NewGuid().ToString("N"),
                                Status = MutabakatStatus.Kaydedildi
                            };

                            context.Mutabakatlar.Add(mutabakat);
                            await context.SaveChangesAsync();

                            importedRecords.Add(mutabakat);
                            createdCount++;
                        }
                        catch (Exception exRow)
                        {
                            var detail = exRow.Message;
                            var inner = exRow.InnerException;
                            while (inner != null)
                            {
                                detail += " -> " + inner.Message;
                                inner = inner.InnerException;
                            }

                            errors.Add($"Satır {r + 1}: {detail}");
                        }
                    }

                    if (errors.Count > 0)
                    {
                        await tx.RollbackAsync();

                        await _logService.AddAsync(
                            "Hata",
                            "Mutabakat",
                            $"Excel import rollback oldu. Dosya: {fileName}, Hata sayısı: {errors.Count}",
                            GetUserEmail()
                        );

                        return (0, 0, errors);
                    }

                    if (sendMail)
                    {
                        foreach (var mutabakat in importedRecords)
                        {
                            var full = await context.Mutabakatlar
                                .Include(m => m.Cari)
                                .Include(m => m.Firma)
                                .FirstOrDefaultAsync(m => m.MutabakatId == mutabakat.MutabakatId);

                            if (full == null)
                            {
                                errors.Add($"MutabakatId {mutabakat.MutabakatId}: kayıt daha sonra değiştirildiği için mail gönderim listesine alınmadı.");
                                continue;
                            }

                            if (full.Cari == null)
                            {
                                errors.Add($"MutabakatId {full.MutabakatId}: cari bilgisi bulunamadı.");
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(full.Cari.CariYetkiliMail))
                            {
                                errors.Add($"MutabakatId {full.MutabakatId}: cari yetkili mail bilgisi boş.");
                                continue;
                            }

                            var sender = await context.Kullanicilar
                                .Include(k => k.Firmalar)
                                .FirstOrDefaultAsync(k => k.Firmalar.Any(f => f.FirmaId == full.FirmaId));



                            if (sender == null)
                            {
                                if (full.Firma == null)
                                {
                                    errors.Add($"MutabakatId {full.MutabakatId}: firma bilgisi bulunamadı.");
                                    continue;
                                }

                                sender = new Kullanici
                                {
                                    KullaniciMail = full.Firma.FirmaMail ?? string.Empty,
                                    Firmalar = new List<Firma> { full.Firma }
                                };
                            }

                            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7017";
                            var approveUrl = $"{baseUrl}/reconciliations/response/{full.MutabakatToken}?durum=approve";
                            var rejectUrl = $"{baseUrl}/reconciliations/response/{full.MutabakatToken}?durum=reject";

                            var ok = await _emailService.SendMutabakatMailAsync(full, sender, approveUrl, rejectUrl, false);
                            if (!ok)
                            {
                                errors.Add($"MutabakatId {full.MutabakatId}: mail gönderilemedi. Cari maili veya firma SMTP bilgilerini kontrol edin.");
                                continue;
                            }

                            full.MutabakatGonderimTarihSaat = DateTime.UtcNow;
                            full.Status = MutabakatStatus.Gonderildi;

                            mailSentCount++;
                        }

                        if (errors.Count > 0)
                        {
                            await tx.RollbackAsync();

                            await _logService.AddAsync(
                                "Hata",
                                "Mutabakat",
                                $"Excel import mail aşamasında rollback oldu. Dosya: {fileName}, Hata sayısı: {errors.Count}",
                                GetUserEmail()
                            );

                            return (0, 0, errors);
                        }

                        await context.SaveChangesAsync();
                    }

                    await tx.CommitAsync();

                    await _logService.AddAsync(
                        "Bilgi",
                        "Mutabakat",
                        $"Excel import tamamlandı. Dosya: {fileName}, Oluşturulan: {createdCount}, Gönderilen Mail: {mailSentCount}",
                        GetUserEmail()
                    );

                    return (createdCount, mailSentCount, errors);
                }
                catch (Exception exMailAny)
                {
                    await tx.RollbackAsync();

                    var detail = exMailAny.Message;
                    var inner = exMailAny.InnerException;
                    while (inner != null)
                    {
                        detail += " -> " + inner.Message;
                        inner = inner.InnerException;
                    }

                    errors.Add($"Import sırasında hata oluştu: {detail}");

                    await _logService.AddAsync(
                        "Hata",
                        "Mutabakat",
                        $"Excel import genel işlem hatası: {detail}",
                        GetUserEmail()
                    );

                    return (0, 0, errors);
                }
            }
            catch (Exception ex)
            {
                var detail = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    detail += " -> " + inner.Message;
                    inner = inner.InnerException;
                }

                errors.Add($"İşlem sırasında hata oluştu: {detail}");

                await _logService.AddAsync(
                    "Hata",
                    "Mutabakat",
                    $"Mutabakat import dış hata: {detail}",
                    GetUserEmail()
                );

                return (0, 0, errors);
            }
        }

        public async Task<string> GenerateNextMutabakatIdAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = await context.Mutabakatlar
                .AsNoTracking()
                .Select(x => x.MutabakatId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToListAsync();

            var maxNumeric = 0;
            foreach (var id in ids)
            {
                var match = System.Text.RegularExpressions.Regex.Match(id ?? string.Empty, "\\d+");
                if (match.Success && int.TryParse(match.Value, out var number) && number > maxNumeric)
                {
                    maxNumeric = number;
                }
            }

            return $"P{maxNumeric + 1}";
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