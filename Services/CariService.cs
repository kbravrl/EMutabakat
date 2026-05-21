using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Radzen.Blazor;
using System.Text.RegularExpressions;

namespace EMutabakat.Services
{
    public class CariService : ICariService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CariService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Cari>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var query = context.Cariler
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .Include(x => x.DovizKodu)
                .OrderBy(x => x.CariAdi)
                .AsQueryable();

            if (allowedFirmaIds != null)
            {
                if (allowedFirmaIds.Count == 0)
                    return new List<Cari>();

                query = query.Where(x => allowedFirmaIds.Contains(x.FirmaId));
            }

            return await query.ToListAsync();
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

            if (kullanici.IsSeedUser)
                return null;

            return kullanici.Firmalar
                .Select(uf => uf.FirmaId)
                .Distinct()
                .Where(i => i > 0)
                .ToList();
        }

        public async Task<string> GenerateNextCariIdAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = await context.Cariler
                .AsNoTracking()
                .Select(x => x.CariId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToListAsync();

            var maxNumeric = 0;
            foreach (var id in ids)
            {
                var match = Regex.Match(id!, @"\d+");
                if (match.Success && int.TryParse(match.Value, out var number) && number > maxNumeric)
                {
                    maxNumeric = number;
                }
            }

            return $"P{maxNumeric + 1}";
        }

        public async Task<Cari?> GetByIdAsync(string cariId, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedCariId = cariId?.Trim() ?? string.Empty;

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds != null && !allowedFirmaIds.Contains(firmaId))
                return null;

            return await context.Cariler
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .Include(x => x.DovizKodu)
                .FirstOrDefaultAsync(x => x.CariId == normalizedCariId && x.FirmaId == firmaId);
        }

        public async Task<Cari> AddAsync(Cari cari)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await ValidateCariAsync(context, cari);

            cari.CariAdi = (cari.CariAdi ?? string.Empty).Trim();

            var sameNameExists = await context.Cariler.AnyAsync(x =>
                x.FirmaId == cari.FirmaId &&
                x.CariAdi.ToLower() == cari.CariAdi.ToLower());

            if (sameNameExists)
                throw new Exception("Bu firmaya ait cari zaten mevcut.");

            var exists = await context.Cariler.AnyAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
            if (exists)
                throw new Exception("Aynı Cari ID ve Firma ile kayıt zaten mevcut.");

            try
            {
                context.Cariler.Add(cari);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new Exception("Bu firmaya ait aynı cari adı zaten mevcut.");
            }

            await _logService.AddAsync(
                "Bilgi",
                "Cari",
                $"Yeni cari eklendi | Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}, Cari Adı: {cari.CariAdi}",
                GetUserEmail()
            );

            return cari;
        }

        public async Task<Cari?> UpdateAsync(Cari cari)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            cari.CariId = cari.CariId?.Trim() ?? string.Empty;
            cari.CariDovizKodu = string.IsNullOrWhiteSpace(cari.CariDovizKodu) ? null : cari.CariDovizKodu.Trim().ToUpperInvariant();
            cari.CariGrupId = cari.CariGrupId?.Trim() ?? string.Empty;
            cari.OriginalCariId = string.IsNullOrWhiteSpace(cari.OriginalCariId) ? cari.CariId : cari.OriginalCariId.Trim();
            cari.OriginalFirmaId = cari.OriginalFirmaId <= 0 ? cari.FirmaId : cari.OriginalFirmaId;

            var existingCari = await context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == cari.OriginalCariId && x.FirmaId == cari.OriginalFirmaId);

            if (existingCari == null)
                return null;

            await ValidateCariAsync(context, cari);

            var keyChanged = !string.Equals(cari.OriginalCariId, cari.CariId, StringComparison.Ordinal)
                || cari.OriginalFirmaId != cari.FirmaId;

            if (keyChanged)
            {
                var keyExists = await context.Cariler.AnyAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
                if (keyExists)
                    throw new Exception("Aynı Cari ID ve Firma ile kayıt zaten mevcut.");

                var newCari = new Cari
                {
                    CariId = cari.CariId,
                    FirmaId = cari.FirmaId,
                    CariAdi = cari.CariAdi,
                    CariUnvan = cari.CariUnvan,
                    CariAdres = cari.CariAdres,
                    CariIlce = cari.CariIlce,
                    CariIl = cari.CariIl,
                    CariVergiDairesi = cari.CariVergiDairesi,
                    CariVergiNumarasi = cari.CariVergiNumarasi,
                    CariWebAdresi = cari.CariWebAdresi,
                    CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi,
                    CariYetkiliTelefon = cari.CariYetkiliTelefon,
                    CariYetkiliGsm = cari.CariYetkiliGsm,
                    CariYetkiliMail = cari.CariYetkiliMail,
                    CariGrupId = cari.CariGrupId,
                    CariDovizKodu = cari.CariDovizKodu,
                    CariAktifPasif = cari.CariAktifPasif
                };

                context.Cariler.Add(newCari);
                await context.SaveChangesAsync();

                await context.Mutabakatlar
                    .Where(m => m.CariId == cari.OriginalCariId && m.FirmaId == cari.OriginalFirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.CariId, cari.CariId)
                        .SetProperty(m => m.FirmaId, cari.FirmaId));

                await context.SilinenMutabakatlar
                    .Where(sm => sm.CariId == cari.OriginalCariId && sm.FirmaId == cari.OriginalFirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(sm => sm.CariId, cari.CariId)
                        .SetProperty(sm => sm.FirmaId, cari.FirmaId));

                context.Cariler.Remove(existingCari);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Cari",
                    $"Cari güncellendi | Eski: Cari Id: {cari.OriginalCariId}, Firma Id: {cari.OriginalFirmaId} → Yeni: Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}",
                    GetUserEmail()
                );

                return await context.Cariler
                    .AsNoTracking()
                    .Include(x => x.Firma)
                    .Include(x => x.CariGrup)
                    .Include(x => x.DovizKodu)
                    .FirstOrDefaultAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);
            }

            var oldSnapshot = new
            {
                existingCari.CariAdi,
                existingCari.CariUnvan,
                existingCari.CariAdres,
                existingCari.CariIlce,
                existingCari.CariIl,
                existingCari.CariVergiDairesi,
                existingCari.CariVergiNumarasi,
                existingCari.CariWebAdresi,
                existingCari.CariYetkiliAdiSoyadi,
                existingCari.CariYetkiliTelefon,
                existingCari.CariYetkiliGsm,
                existingCari.CariYetkiliMail,
                existingCari.CariGrupId,
                existingCari.CariDovizKodu,
                existingCari.CariAktifPasif
            };

            existingCari.CariAdi = cari.CariAdi;
            existingCari.CariUnvan = cari.CariUnvan;
            existingCari.CariAdres = cari.CariAdres;
            existingCari.CariIlce = cari.CariIlce;
            existingCari.CariIl = cari.CariIl;
            existingCari.CariVergiDairesi = cari.CariVergiDairesi;
            existingCari.CariVergiNumarasi = cari.CariVergiNumarasi;
            existingCari.CariWebAdresi = cari.CariWebAdresi;
            existingCari.CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi;
            existingCari.CariYetkiliTelefon = cari.CariYetkiliTelefon;
            existingCari.CariYetkiliGsm = cari.CariYetkiliGsm;
            existingCari.CariYetkiliMail = cari.CariYetkiliMail;
            existingCari.CariGrupId = cari.CariGrupId;
            existingCari.CariDovizKodu = cari.CariDovizKodu;
            existingCari.CariAktifPasif = cari.CariAktifPasif;

            await context.SaveChangesAsync();

            await _logService.AddChangeAsync(
                "Cari",
                $"Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}",
                oldSnapshot,
                new
                {
                    cari.CariAdi,
                    cari.CariUnvan,
                    cari.CariAdres,
                    cari.CariIlce,
                    cari.CariIl,
                    cari.CariVergiDairesi,
                    cari.CariVergiNumarasi,
                    cari.CariWebAdresi,
                    cari.CariYetkiliAdiSoyadi,
                    cari.CariYetkiliTelefon,
                    cari.CariYetkiliGsm,
                    cari.CariYetkiliMail,
                    cari.CariGrupId,
                    cari.CariDovizKodu,
                    cari.CariAktifPasif
                },
                GetUserEmail()
            );

            return existingCari;
        }

        public async Task<bool> DeleteAsync(string cariId, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedCariId = cariId?.Trim() ?? string.Empty;

            var cari = await context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == normalizedCariId && x.FirmaId == firmaId);

            if (cari == null)
                return false;

            try
            {
                context.Cariler.Remove(cari);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Cari",
                    $"Cari silindi | Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}, Cari Adı: {cari.CariAdi}",
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
                    "Cari",
                    $"Cari silinemedi | Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}, Cari Adı: {cari.CariAdi}",
                    GetUserEmail(),
                    detail
                );

                throw new Exception("Bu cari kaydı başka kayıtlarda kullanıldığı için silinemez.");
            }
        }

        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .Name;
        }

        private async Task ValidateCariAsync(AppDbContext context, Cari cari)
        {
            cari.CariId = cari.CariId?.Trim() ?? string.Empty;
            cari.CariGrupId = cari.CariGrupId?.Trim() ?? string.Empty;
            cari.CariYetkiliMail = cari.CariYetkiliMail?.Trim().ToLower();
            cari.CariDovizKodu = string.IsNullOrWhiteSpace(cari.CariDovizKodu)
                ? null
                : cari.CariDovizKodu.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(cari.CariId))
                throw new Exception("Cari ID zorunludur.");

            if (cari.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariAdi))
                throw new Exception("Cari adı zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariVergiDairesi))
                throw new Exception("Vergi dairesi zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariVergiNumarasi))
                throw new Exception("Vergi numarası zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariYetkiliMail))
                throw new Exception("Yetkili mail zorunludur.");

            if (string.IsNullOrWhiteSpace(cari.CariGrupId))
                throw new Exception("Cari grup seçimi zorunludur.");

            if (cari.CariAktifPasif != 0 && cari.CariAktifPasif != 1)
                throw new Exception("Aktif/Pasif değeri geçersiz.");

            var firmaExists = await context.Firmalar.AnyAsync(x => x.FirmaId == cari.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            var cariGrup = await context.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cari.CariGrupId && x.FirmaId == cari.FirmaId);

            if (cariGrup == null)
                throw new Exception("Seçilen cari grup bulunamadı veya bu firmaya ait değil.");

            if (!string.IsNullOrWhiteSpace(cari.CariDovizKodu))
            {
                var dovizExists = await context.DovizKodlari
                    .AnyAsync(x => x.TCMB == cari.CariDovizKodu && x.DovizKoduAktifPasif == 1);

                if (!dovizExists)
                    throw new Exception($"Seçilen Döviz kodu geçerli ve aktif değil.");
            }
        }

        private static string? GetStringCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            return cell?.ToString()?.Trim();
        }

        private static int ParseIntCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return 0;

            if (cell.CellType == CellType.Numeric)
                return Convert.ToInt32(cell.NumericCellValue);

            return int.TryParse(cell.ToString(), out var value) ? value : 0;
        }

        public async Task<byte[]> ExportToExcelAsync(List<Cari> cariler)
        {
            var orderedCariler = cariler
                .OrderBy(x => x.CariAdi ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.CariId)
                .ToList();

            await _logService.AddAsync(
                "Bilgi",
                "Cari",
                $"Cari Excel export başladı. Kayıt sayısı: {orderedCariler.Count}",
                GetUserEmail()
            );

            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Cariler");

            var headers = new[]
            {
               "CariId",
               "CariGrupId",
               "CariAdi",
               "CariUnvan",
               "CariAdres",
               "CariIl",
               "CariIlce",
               "CariVergiDairesi",
               "CariVergiNumarasi",
               "CariWebAdresi",
               "CariYetkiliAdiSoyadi",
               "CariYetkiliTelefon",
               "CariYetkiliGsm",
               "CariYetkiliMail",
               "DovizKodu",
               "CariAktifPasif"
            };

            var headerRow = sheet.CreateRow(0);

            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }

            for (int i = 0; i < orderedCariler.Count; i++)
            {
                var cari = orderedCariler[i];
                var row = sheet.CreateRow(i + 1);

                row.CreateCell(0).SetCellValue(cari.CariId ?? "");
                row.CreateCell(1).SetCellValue(cari.CariGrupId ?? "");
                row.CreateCell(2).SetCellValue(cari.CariAdi ?? "");
                row.CreateCell(3).SetCellValue(cari.CariUnvan ?? "");
                row.CreateCell(4).SetCellValue(cari.CariAdres ?? "");
                row.CreateCell(5).SetCellValue(cari.CariIl ?? "");
                row.CreateCell(6).SetCellValue(cari.CariIlce ?? "");
                row.CreateCell(7).SetCellValue(cari.CariVergiDairesi ?? "");
                row.CreateCell(8).SetCellValue(cari.CariVergiNumarasi ?? "");
                row.CreateCell(9).SetCellValue(cari.CariWebAdresi ?? "");
                row.CreateCell(10).SetCellValue(cari.CariYetkiliAdiSoyadi ?? "");
                row.CreateCell(11).SetCellValue(cari.CariYetkiliTelefon ?? "");
                row.CreateCell(12).SetCellValue(cari.CariYetkiliGsm ?? "");
                row.CreateCell(13).SetCellValue(cari.CariYetkiliMail ?? "");
                row.CreateCell(14).SetCellValue(cari.CariDovizKodu ?? "");
                row.CreateCell(15).SetCellValue(cari.CariAktifPasif);
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            await using var ms = new MemoryStream();
            workbook.Write(ms, true);

            await _logService.AddAsync(
                "Bilgi",
                "Cari",
                $"Cari Excel export tamamlandı. Kayıt sayısı: {orderedCariler.Count}",
                GetUserEmail()
            );

            return ms.ToArray();
        }

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;

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
                    await _logService.AddImportResultAsync("Cari", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                    return (0, 0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    await _logService.AddImportResultAsync("Cari", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
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

                string[] requiredColumns =
                {
                    "CariId",
                    "CariAdi",
                    "CariVergiDairesi",
                    "CariVergiNumarasi",
                    "CariYetkiliMail",
                    "CariGrupId",
                    "DovizKodu"
                };

                foreach (var column in requiredColumns)
                {
                    if (!headerMap.ContainsKey(column))
                    {
                        errors.Add($"Gerekli sütun '{column}' bulunamadı.");
                    }
                }

                if (errors.Count > 0)
                {
                    await _logService.AddImportResultAsync("Cari", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                    return (0, 0, errors);
                }

                await using var context = await _contextFactory.CreateDbContextAsync();

                if (firmaId <= 0)
                {
                    errors.Add("Firma seçimi zorunludur.");
                    await _logService.AddImportResultAsync("Cari", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                    return (0, 0, errors);
                }

                var firmaExists = await context.Firmalar.AnyAsync(x => x.FirmaId == firmaId);
                if (!firmaExists)
                {
                    errors.Add("Seçilen firma bulunamadı.");
                    await _logService.AddImportResultAsync("Cari", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                    return (0, 0, errors);
                }

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    var cari = new Cari
                    {
                        CariId = GetStringCell(row, headerMap["CariId"]) ?? string.Empty,
                        FirmaId = firmaId,
                        CariAdi = GetStringCell(row, headerMap["CariAdi"]) ?? string.Empty,
                        CariUnvan = headerMap.ContainsKey("CariUnvan") ? GetStringCell(row, headerMap["CariUnvan"]) : null,
                        CariAdres = headerMap.ContainsKey("CariAdres") ? GetStringCell(row, headerMap["CariAdres"]) : null,
                        CariIlce = headerMap.ContainsKey("CariIlce") ? GetStringCell(row, headerMap["CariIlce"]) : null,
                        CariIl = headerMap.ContainsKey("CariIl") ? GetStringCell(row, headerMap["CariIl"]) : null,
                        CariVergiDairesi = GetStringCell(row, headerMap["CariVergiDairesi"]) ?? string.Empty,
                        CariVergiNumarasi = GetStringCell(row, headerMap["CariVergiNumarasi"]) ?? string.Empty,
                        CariWebAdresi = headerMap.ContainsKey("CariWebAdresi") ? GetStringCell(row, headerMap["CariWebAdresi"]) : null,
                        CariYetkiliAdiSoyadi = headerMap.ContainsKey("CariYetkiliAdiSoyadi") ? GetStringCell(row, headerMap["CariYetkiliAdiSoyadi"]) : null,
                        CariYetkiliTelefon = headerMap.ContainsKey("CariYetkiliTelefon") ? GetStringCell(row, headerMap["CariYetkiliTelefon"]) : null,
                        CariYetkiliGsm = headerMap.ContainsKey("CariYetkiliGsm") ? GetStringCell(row, headerMap["CariYetkiliGsm"]) : null,
                        CariYetkiliMail = GetStringCell(row, headerMap["CariYetkiliMail"]) ?? string.Empty,
                        CariGrupId = GetStringCell(row, headerMap["CariGrupId"]) ?? string.Empty,
                        CariDovizKodu = GetStringCell(row, headerMap["DovizKodu"]),
                        CariAktifPasif = headerMap.ContainsKey("CariAktifPasif")
                            ? ParseIntCell(row, headerMap["CariAktifPasif"])
                            : 1
                    };

                    cari.CariId = cari.CariId.Trim();
                    cari.CariGrupId = cari.CariGrupId.Trim();

                    try
                    {
                        await ValidateCariAsync(context, cari);

                        var existing = await context.Cariler
                            .FirstOrDefaultAsync(x => x.CariId == cari.CariId && x.FirmaId == cari.FirmaId);

                        if (existing == null)
                        {
                            context.Cariler.Add(cari);
                            try
                            {
                                await context.SaveChangesAsync();
                                created++;

                            }
                            catch (DbUpdateException)
                            {
                                context.ChangeTracker.Clear();
                                var msg = $"Satır {r + 1}: Bu firmada '{cari.CariAdi}' adında başka bir cari zaten mevcut.";
                                errors.Add(msg);
                            }
                        }
                        else
                        {
                            var hasChange =
                               existing.CariAdi != cari.CariAdi || existing.CariUnvan != cari.CariUnvan ||
                               existing.CariAdres != cari.CariAdres || existing.CariIlce != cari.CariIlce ||
                               existing.CariIl != cari.CariIl || existing.CariVergiDairesi != cari.CariVergiDairesi ||
                               existing.CariVergiNumarasi != cari.CariVergiNumarasi || existing.CariWebAdresi != cari.CariWebAdresi ||
                               existing.CariYetkiliAdiSoyadi != cari.CariYetkiliAdiSoyadi || existing.CariYetkiliTelefon != cari.CariYetkiliTelefon ||
                               existing.CariYetkiliGsm != cari.CariYetkiliGsm || existing.CariYetkiliMail != cari.CariYetkiliMail ||
                               existing.CariGrupId != cari.CariGrupId || existing.CariDovizKodu != cari.CariDovizKodu ||
                               existing.CariAktifPasif != cari.CariAktifPasif;

                            if (hasChange)
                            {
                                var oldSnapshot = new
                                {
                                    existing.CariAdi,
                                    existing.CariUnvan,
                                    existing.CariAdres,
                                    existing.CariIlce,
                                    existing.CariIl,
                                    existing.CariVergiDairesi,
                                    existing.CariVergiNumarasi,
                                    existing.CariWebAdresi,
                                    existing.CariYetkiliAdiSoyadi,
                                    existing.CariYetkiliTelefon,
                                    existing.CariYetkiliGsm,
                                    existing.CariYetkiliMail,
                                    existing.CariGrupId,
                                    existing.CariDovizKodu,
                                    existing.CariAktifPasif
                                };

                                existing.CariAdi = cari.CariAdi;
                                existing.CariUnvan = cari.CariUnvan;
                                existing.CariAdres = cari.CariAdres;
                                existing.CariIlce = cari.CariIlce;
                                existing.CariIl = cari.CariIl;
                                existing.CariVergiDairesi = cari.CariVergiDairesi;
                                existing.CariVergiNumarasi = cari.CariVergiNumarasi;
                                existing.CariWebAdresi = cari.CariWebAdresi;
                                existing.CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi;
                                existing.CariYetkiliTelefon = cari.CariYetkiliTelefon;
                                existing.CariYetkiliGsm = cari.CariYetkiliGsm;
                                existing.CariYetkiliMail = cari.CariYetkiliMail;
                                existing.CariGrupId = cari.CariGrupId;
                                existing.CariDovizKodu = cari.CariDovizKodu;
                                existing.CariAktifPasif = cari.CariAktifPasif;

                                try
                                {
                                    await context.SaveChangesAsync();
                                    updated++;

                                    await _logService.AddChangeAsync(
                                        "Cari",
                                        $"Cari Id: {cari.CariId}, Firma Id: {cari.FirmaId}",
                                        oldSnapshot,
                                        new
                                        {
                                            cari.CariAdi,
                                            cari.CariUnvan,
                                            cari.CariAdres,
                                            cari.CariIlce,
                                            cari.CariIl,
                                            cari.CariVergiDairesi,
                                            cari.CariVergiNumarasi,
                                            cari.CariWebAdresi,
                                            cari.CariYetkiliAdiSoyadi,
                                            cari.CariYetkiliTelefon,
                                            cari.CariYetkiliGsm,
                                            cari.CariYetkiliMail,
                                            cari.CariGrupId,
                                            cari.CariDovizKodu,
                                            cari.CariAktifPasif
                                        },
                                        GetUserEmail()
                                    );
                                }
                                catch (DbUpdateException)
                                {
                                    context.ChangeTracker.Clear();
                                    var msg = $"Satır {r + 1}: Bu firmada '{cari.CariAdi}' adında başka bir cari zaten mevcut.";
                                    errors.Add(msg);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {r + 1}: {ex.Message}");
                    }
                }

                await _logService.AddImportResultAsync(
                    "Cari",
                    $"Excel import tamamlandı. Dosya: {fileName}, Oluşturulan: {created}, Güncellenen: {updated}",
                    errors,
                    GetUserEmail()
                );

                return (created, updated, errors);
            }
            catch (Exception ex)
            {
                var detail = ex.Message;
                var inner = ex.InnerException;
                while (inner != null) { detail += " → " + inner.Message; inner = inner.InnerException; }

                errors.Add($"İşlem sırasında hata oluştu: {detail}");

                await _logService.AddImportResultAsync(
                    "Cari",
                    $"Excel import genel hata. Dosya: {fileName}",
                    errors,
                    GetUserEmail()
                );

                return (0, 0, errors);
            }
        }
    }
}