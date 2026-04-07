using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMutabakat.Services
{
    public class MutabakatService : IMutabakatService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly ISdService _sdService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MutabakatService(
            IDbContextFactory<AppDbContext> contextFactory,
            IEmailService emailService,
            ISdService sdService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _sdService = sdService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Mutabakat>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .OrderByDescending(x => x.MutabakatDonemi)
                .ThenByDescending(x => x.MutabakatId)
                .ToListAsync();
        }

        public async Task<Mutabakat?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.MutabakatId == id);
        }

        public async Task<Mutabakat> AddAsync(Mutabakat mutabakat)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            mutabakat.CariId = (mutabakat.CariId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.CariId))
                throw new Exception("Cari seçimi zorunludur.");

            var matchedCariler = await context.Cariler
                .Where(c => c.CariId == mutabakat.CariId)
                .Select(c => new { c.CariId, c.FirmaId, c.CariDovizKodu })
                .ToListAsync();

            if (matchedCariler.Count == 0)
                throw new Exception("Seçilen cari bulunamadı.");

            if (matchedCariler.Count > 1)
                throw new Exception("Aynı CariId birden fazla firmada bulundu. Lütfen cari ID'yi benzersiz kullanın.");

            var selectedCari = matchedCariler[0];
            mutabakat.FirmaId = selectedCari.FirmaId;

            mutabakat.MutabakatDovizKodu = (mutabakat.MutabakatDovizKodu ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
            {
                mutabakat.MutabakatDovizKodu = selectedCari.CariDovizKodu ?? "TL";
            }

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                throw new Exception("Döviz kodu zorunludur.");

            var dovizExists = await context.DovizKodlari.AnyAsync(x => x.TCMB == mutabakat.MutabakatDovizKodu);
            if (!dovizExists)
                throw new Exception("Geçerli bir döviz kodu seçiniz.");

            mutabakat.MutabakatDonemi = DateTime.SpecifyKind(
                mutabakat.MutabakatDonemi,
                DateTimeKind.Utc);

            mutabakat.MutabakatAciklama = string.IsNullOrWhiteSpace(mutabakat.MutabakatAciklama)
                ? null
                : mutabakat.MutabakatAciklama.Trim();

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            if (mutabakat.MutabakatDurum == 0)
            {
                mutabakat.MutabakatDurum = 3;
            }

            if (mutabakat.MutabakatGonderimDurumu == 0)
            {
                mutabakat.MutabakatGonderimDurumu = 1;
            }

            context.Mutabakatlar.Add(mutabakat);
            await context.SaveChangesAsync();

            return mutabakat;
        }

        public async Task<Mutabakat?> UpdateAsync(Mutabakat mutabakat)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            mutabakat.CariId = (mutabakat.CariId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mutabakat.CariId))
                throw new Exception("Cari seçimi zorunludur.");

            var matchedCariler = await context.Cariler
                .Where(c => c.CariId == mutabakat.CariId)
                .Select(c => new { c.CariId, c.FirmaId, c.CariDovizKodu })
                .ToListAsync();

            if (matchedCariler.Count == 0)
                throw new Exception("Seçilen cari bulunamadı.");

            if (matchedCariler.Count > 1)
                throw new Exception("Aynı CariId birden fazla firmada bulundu. Lütfen cari ID'yi benzersiz kullanın.");

            var selectedCari = matchedCariler[0];
            mutabakat.FirmaId = selectedCari.FirmaId;

            mutabakat.MutabakatDovizKodu = (mutabakat.MutabakatDovizKodu ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
            {
                mutabakat.MutabakatDovizKodu = selectedCari.CariDovizKodu ?? "TL";
            }

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatDovizKodu))
                throw new Exception("Döviz kodu zorunludur.");

            var dovizExists = await context.DovizKodlari.AnyAsync(x => x.TCMB == mutabakat.MutabakatDovizKodu);
            if (!dovizExists)
                throw new Exception("Geçerli bir döviz kodu seçiniz.");

            var existingMutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakat.MutabakatId);

            if (existingMutabakat == null)
                return null;

            var mailGonderildiMi = existingMutabakat.MutabakatGonderimTarihSaat != default(DateTime);

            if (!mailGonderildiMi)
            {
                existingMutabakat.FirmaId = selectedCari.FirmaId;
                existingMutabakat.CariId = mutabakat.CariId;
                existingMutabakat.MutabakatDonemi = DateTime.SpecifyKind(
                    mutabakat.MutabakatDonemi,
                    DateTimeKind.Utc);
                existingMutabakat.MutabakatDovizKodu = mutabakat.MutabakatDovizKodu;
                existingMutabakat.MutabakatBakiye = mutabakat.MutabakatBakiye;
                existingMutabakat.MutabakatBakiyeTipi = mutabakat.MutabakatBakiyeTipi;
            }

            existingMutabakat.MutabakatAciklama = string.IsNullOrWhiteSpace(mutabakat.MutabakatAciklama)
                ? null
                : mutabakat.MutabakatAciklama.Trim();

            await context.SaveChangesAsync();
            return existingMutabakat;
        }

        public async Task<bool> DeleteAsync(int id)
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

            return true;
        }

        public async Task<bool> SendMailAsync(int mutabakatId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
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
               .Include(x => x.Firma)
               .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null)
                return false;

            if (kullanici.Firma == null)
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
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 1;

            if (mutabakat.MutabakatDurum == 0)
            {
                mutabakat.MutabakatDurum = 3;
            }

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SendReminderAsync(int mutabakatId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
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
               .Include(x => x.Firma)
               .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null)
                return false;

            if (kullanici.Firma == null)
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
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 2;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<Mutabakat?> GetByTokenAsync(string token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Mutabakatlar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);
        }

        public async Task<bool> ApproveAsync(string token, string mail, string adSoyad, string gsm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mutabakat = await context.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null)
                return false;

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
                return false;

            mutabakat.MutabakatDurum = 1;
            mutabakat.MutabakatCevapTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatCevapMail = mail;
            mutabakat.MutabakatCevapAdSoyad = adSoyad;
            mutabakat.MutabakatCevapGsm = gsm;

            await context.SaveChangesAsync();
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

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
                return false;

            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            mutabakat.MutabakatDurum = 2;
            mutabakat.MutabakatCevapTarihSaat = DateTime.UtcNow;
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
            return true;
        }

        // Helper parsers used by Excel import
        private static int ParseIntCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return 0;
            if (cell.CellType == CellType.Numeric) return Convert.ToInt32(cell.NumericCellValue);
            var s = cell.ToString();
            return int.TryParse(s, out var v) ? v : 0;
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

        public async Task<(int created, int mailsSent, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName)
        {
            var errors = new List<string>();
            var createdCount = 0;
            var mailSentCount = 0;
            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                IWorkbook workbook;
                var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
                if (ext == ".xlsx") workbook = new XSSFWorkbook(stream);
                else workbook = new HSSFWorkbook(stream);

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
                    if (!string.IsNullOrWhiteSpace(text)) headerMap[text] = i;
                }

                string[] required = new[] { "CariId", "MutabakatTarihi", "DovizKodu", "MutabakatBakiye", "MutabakatBakiyeTipi" };
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

                var prepared = new List<(int RowNumber, Mutabakat Entity)>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var cariId = (GetStringCell(row, headerMap["CariId"]) ?? string.Empty).Trim();
                        var tarihColumn = headerMap.ContainsKey("MutabakatTarihi") ? "MutabakatTarihi" : "MutabakatDonemi";
                        DateTime donem = ParseDateCell(row, headerMap[tarihColumn]);
                        var dovizColumn = headerMap.ContainsKey("DovizKodu") ? "DovizKodu" : "TCMP";
                        var doviz = (GetStringCell(row, headerMap[dovizColumn]) ?? string.Empty).Trim().ToUpperInvariant();
                        decimal bakiye = ParseDecimalCell(row, headerMap["MutabakatBakiye"]);
                        var bakiyeTipi = GetStringCell(row, headerMap["MutabakatBakiyeTipi"]) ?? "B";
                        var aciklama = headerMap.ContainsKey("MutabakatAciklama") ? GetStringCell(row, headerMap["MutabakatAciklama"]) : string.Empty;

                        var matchedCariler = await context.Cariler
                            .Where(c => c.CariId == cariId)
                            .Select(c => new { c.CariId, c.FirmaId })
                            .ToListAsync();

                        if (matchedCariler.Count == 0)
                        {
                            errors.Add($"Satır {r + 1}: CariId {cariId} bulunamadı.");
                            continue;
                        }

                        if (matchedCariler.Count > 1)
                        {
                            errors.Add($"Satır {r + 1}: CariId {cariId} birden fazla firmada bulundu. Cari ID benzersiz olmalıdır.");
                            continue;
                        }

                        var firmaId = matchedCariler[0].FirmaId;

                        var dovizExists = await context.DovizKodlari.AnyAsync(d => d.TCMB == doviz);
                        if (!dovizExists)
                        {
                            errors.Add($"Satır {r + 1}: DovizKodu {doviz} geçerli değil.");
                            continue;
                        }

                        var mutabakat = new Mutabakat
                        {
                            FirmaId = firmaId,
                            CariId = cariId,
                            MutabakatDonemi = donem,
                            MutabakatDovizKodu = doviz,
                            MutabakatBakiye = bakiye,
                            MutabakatBakiyeTipi = bakiyeTipi,
                            MutabakatAciklama = aciklama ?? string.Empty
                        };

                        prepared.Add((r + 1, mutabakat));
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
                    return (0, 0, errors);
                }

                await using var tx = await context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var (_, mutabakat) in prepared)
                    {
                        mutabakat.MutabakatDonemi = DateTime.SpecifyKind(mutabakat.MutabakatDonemi, DateTimeKind.Utc);
                        if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken)) mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
                        if (mutabakat.MutabakatDurum == 0) mutabakat.MutabakatDurum = 3;
                        if (mutabakat.MutabakatGonderimDurumu == 0) mutabakat.MutabakatGonderimDurumu = 1;

                        context.Mutabakatlar.Add(mutabakat);
                    }

                    await context.SaveChangesAsync();
                    createdCount = prepared.Count;

                    foreach (var (_, mutabakat) in prepared)
                    {
                        var full = await context.Mutabakatlar
                            .Include(m => m.Cari)
                            .Include(m => m.Firma)
                            .FirstOrDefaultAsync(m => m.MutabakatId == mutabakat.MutabakatId);

                        if (full == null)
                        {
                            throw new InvalidOperationException($"MutabakatId {mutabakat.MutabakatId}: kayıt kaydedildikten sonra bulunamadı.");
                        }

                        var sender = await context.Kullanicilar
                            .Include(k => k.Firma)
                            .FirstOrDefaultAsync(k => k.FirmaId == full.FirmaId);

                        if (sender == null)
                        {
                            sender = new Kullanici { KullaniciMail = full.Firma?.FirmaMail ?? string.Empty, Firma = full.Firma };
                        }

                        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7017";
                        var approveUrl = $"{baseUrl}/reconciliations/response/{full.MutabakatToken}?durum=approve";
                        var rejectUrl = $"{baseUrl}/reconciliations/response/{full.MutabakatToken}?durum=reject";

                        var ok = await _emailService.SendMutabakatMailAsync(full, sender, approveUrl, rejectUrl, false);
                        if (!ok)
                        {
                            throw new InvalidOperationException($"MutabakatId {full.MutabakatId}: Mail gönderilemedi, bilgileri kontrol edin.");
                        }

                        full.MutabakatGonderimTarihSaat = DateTime.UtcNow;
                        full.MutabakatGonderimDurumu = 1;
                        if (full.MutabakatDurum == 0) full.MutabakatDurum = 3;

                        mailSentCount++;
                    }

                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return (createdCount, mailSentCount, errors);
                }
                catch (Exception exMailAny)
                {
                    await tx.RollbackAsync();

                    errors.Add("Mail gönderimi başarısız, lütfen bilgileri kontrol ediniz");
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
                return (0, 0, errors);
            }

        }
    }
}