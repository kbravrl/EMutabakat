using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System;
using Npgsql;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EMutabakat.Services
{
    public class CariGrupService : ICariGrupService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CariGrupService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<CariGrup>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var query = context.CariGruplar
                .AsNoTracking()
                .Include(x => x.Firma)
                .OrderByDescending(x => x.CariGrupAktifPasif)
                .ThenBy(x => x.CariGrupAdi)
                .AsQueryable();

            if (allowedFirmaIds != null)
            {
                if (allowedFirmaIds.Count == 0)
                    return new List<CariGrup>();

                query = query.Where(x => allowedFirmaIds.Contains(x.FirmaId));
            }

            return await query.ToListAsync();
        }

        public async Task<CariGrup?> GetByIdAsync(string id, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds != null && !allowedFirmaIds.Contains(firmaId))
                return null;

            return await context.CariGruplar
                .AsNoTracking()
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.CariGrupId == id && x.FirmaId == firmaId);
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

            var ids = kullanici.Firmalar
               .Select(uf => uf.FirmaId)
               .Distinct()
               .Where(i => i > 0)
               .ToList();

            return ids;
        }

        public async Task<string> GenerateNextCariGrupIdAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = await context.CariGruplar
                .AsNoTracking()
                .Select(x => x.CariGrupId)
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

        public async Task<CariGrup> AddAsync(CariGrup cariGrup)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            cariGrup.CariGrupId = cariGrup.CariGrupId?.Trim() ?? string.Empty;
            cariGrup.CariGrupAdi = cariGrup.CariGrupAdi?.Trim() ?? string.Empty;

            if (cariGrup.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(cariGrup.CariGrupId))
                throw new Exception("Cari grup ID zorunludur.");

            if (string.IsNullOrWhiteSpace(cariGrup.CariGrupAdi))
                throw new Exception("Cari grup adı zorunludur.");

            if (cariGrup.CariGrupAktifPasif != 0 && cariGrup.CariGrupAktifPasif != 1)
                throw new Exception("Aktif/Pasif bilgisi geçersiz.");

            var exists = await context.CariGruplar.AnyAsync(x => x.CariGrupId == cariGrup.CariGrupId && x.FirmaId == cariGrup.FirmaId);
            if (exists)
                throw new Exception("Bu firmada aynı cari grup ID zaten kullanılıyor.");

            var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == cariGrup.FirmaId);

            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            context.CariGruplar.Add(cariGrup);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "CariGrup",
                $"Yeni cari grup eklendi | Cari Grup Id: {cariGrup.CariGrupId}, Firma Id: {cariGrup.FirmaId}, Adı: {cariGrup.CariGrupAdi}",
                GetUserEmail()
            );

            return cariGrup;
        }

        public async Task<CariGrup?> UpdateAsync(CariGrup cariGrup)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            cariGrup.CariGrupId = cariGrup.CariGrupId?.Trim() ?? string.Empty;
            cariGrup.CariGrupAdi = cariGrup.CariGrupAdi?.Trim() ?? string.Empty;
            cariGrup.OriginalCariGrupId = string.IsNullOrWhiteSpace(cariGrup.OriginalCariGrupId)
                ? cariGrup.CariGrupId
                : cariGrup.OriginalCariGrupId.Trim();
            cariGrup.OriginalFirmaId = cariGrup.OriginalFirmaId <= 0
                ? cariGrup.FirmaId
                : cariGrup.OriginalFirmaId;

            if (string.IsNullOrWhiteSpace(cariGrup.CariGrupId))
                throw new Exception("Cari grup ID zorunludur.");

            if (cariGrup.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(cariGrup.CariGrupAdi))
                throw new Exception("Cari grup adı zorunludur.");

            if (cariGrup.CariGrupAktifPasif != 0 && cariGrup.CariGrupAktifPasif != 1)
                throw new Exception("Aktif/Pasif bilgisi geçersiz.");

            var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == cariGrup.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            var existingCariGrup = await context.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.OriginalCariGrupId && x.FirmaId == cariGrup.OriginalFirmaId);

            if (existingCariGrup == null)
                return null;

            var keyChanged = !string.Equals(cariGrup.OriginalCariGrupId, cariGrup.CariGrupId, StringComparison.Ordinal)
                || cariGrup.OriginalFirmaId != cariGrup.FirmaId;

            if (keyChanged)
            {
                var newIdExists = await context.CariGruplar.AnyAsync(x => x.CariGrupId == cariGrup.CariGrupId && x.FirmaId == cariGrup.FirmaId);
                if (newIdExists)
                    throw new Exception("Bu firmada aynı cari grup ID zaten kullanılıyor.");

                var newCariGrup = new CariGrup
                {
                    CariGrupId = cariGrup.CariGrupId,
                    FirmaId = cariGrup.FirmaId,
                    CariGrupAdi = cariGrup.CariGrupAdi,
                    CariGrupAktifPasif = cariGrup.CariGrupAktifPasif
                };

                context.CariGruplar.Add(newCariGrup);
                await context.SaveChangesAsync();

                await context.Cariler
                    .Where(c => c.CariGrupId == cariGrup.OriginalCariGrupId && c.FirmaId == cariGrup.OriginalFirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.CariGrupId, cariGrup.CariGrupId)
                        .SetProperty(c => c.FirmaId, cariGrup.FirmaId));

                context.ChangeTracker.Clear();

                var oldGrupToDelete = await context.CariGruplar
                    .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.OriginalCariGrupId && x.FirmaId == cariGrup.OriginalFirmaId);

                if (oldGrupToDelete != null)
                {
                    context.CariGruplar.Remove(oldGrupToDelete);
                    await context.SaveChangesAsync();
                }

                await _logService.AddChangeAsync(
                    "CariGrup",
                    $"Cari Grup Id: {cariGrup.CariGrupId}, Firma Id: {cariGrup.FirmaId}",
                    new
                    {
                        CariGrupId = cariGrup.OriginalCariGrupId,
                        FirmaId = cariGrup.OriginalFirmaId,
                        cariGrup.CariGrupAdi,
                        cariGrup.CariGrupAktifPasif
                    },
                    new
                    {
                        cariGrup.CariGrupId,
                        cariGrup.FirmaId,
                        cariGrup.CariGrupAdi,
                        cariGrup.CariGrupAktifPasif
                    },
                    GetUserEmail()
                );

                return await context.CariGruplar
                    .Include(x => x.Firma)
                    .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.CariGrupId && x.FirmaId == cariGrup.FirmaId);
            }

            var oldSnapshot = new 
            { 
                existingCariGrup.CariGrupAdi, 
                existingCariGrup.FirmaId, 
                existingCariGrup.CariGrupAktifPasif 
            }; 

            existingCariGrup.FirmaId = cariGrup.FirmaId; 
            existingCariGrup.CariGrupAdi = cariGrup.CariGrupAdi; 
            existingCariGrup.CariGrupAktifPasif = cariGrup.CariGrupAktifPasif; 

            await context.SaveChangesAsync();

            await _logService.AddChangeAsync(
                "CariGrup",
                $"Cari Grup Id: {cariGrup.CariGrupId}, Firma Id: {cariGrup.FirmaId}",
                oldSnapshot,
                new
                {
                    cariGrup.CariGrupAdi,
                    cariGrup.FirmaId,
                    cariGrup.CariGrupAktifPasif
                },
                GetUserEmail()
            );

            return existingCariGrup;
        }

        public async Task<bool> DeleteAsync(string id, int firmaId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedId = id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedId))
                return false;

            var cariGrup = await context.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == normalizedId && x.FirmaId == firmaId);

            if (cariGrup == null)
                return false;

            try
            {
                context.CariGruplar.Remove(cariGrup);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "CariGrup",
                    $"Cari grup silindi | Cari Grup Id: {cariGrup.CariGrupId}, Firma Id: {cariGrup.FirmaId}, Adı: {cariGrup.CariGrupAdi}",
                    GetUserEmail()
                );

                return true;
            }
            catch (DbUpdateException)
            {

                throw new Exception("Bu cari grup başka kayıtlarda kullanıldığı için silinemez.");
            }
            catch (InvalidOperationException ex)
            {

                if (ex.Message.Contains("association between entity types") ||
                    ex.Message.Contains("relationship") ||
                    ex.Message.Contains("severed"))
                {
                    throw new Exception("Bu cari grup başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Cari grup silinirken bir işlem hatası oluştu.");
            }
            catch (Exception)
            {
                throw new Exception("Cari grup silinirken bir hata oluştu.");
            }
        }

        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .Name;
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

        public async Task<byte[]> ExportToExcelAsync(List<CariGrup> cariGruplar)
        {
            await _logService.AddAsync(
                "Bilgi",
                "CariGrup",
                $"Cari Grup Excel export başladı. Kayıt sayısı: {cariGruplar.Count}",
                GetUserEmail()
            );

            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("CariGruplar");

            var headers = new[]
            {
                "CariGrupId",
                "CariGrupAdi",
                "CariGrupAktifPasif"
            };

            var headerRow = sheet.CreateRow(0);

            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }

            for (int i = 0; i < cariGruplar.Count; i++)
            {
                var item = cariGruplar[i];
                var row = sheet.CreateRow(i + 1);

                row.CreateCell(0).SetCellValue(item.CariGrupId);
                row.CreateCell(1).SetCellValue(item.CariGrupAdi);
                row.CreateCell(2).SetCellValue(item.CariGrupAktifPasif);
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            await using var ms = new MemoryStream();
            workbook.Write(ms, true);

            await _logService.AddAsync(
                "Bilgi",
                "CariGrup",
                $"Cari Grup Excel export tamamlandı. Kayıt sayısı: {cariGruplar.Count}",
                GetUserEmail()
            );

            return ms.ToArray();
        }

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (firmaId <= 0)
            {
                errors.Add("Firma seçimi zorunludur.");
                await _logService.AddImportResultAsync("CariGrup", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                return (0, 0, errors);
            }

            var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == firmaId);
            if (!firmaExists)
            {
                errors.Add("Seçilen firma bulunamadı.");
                await _logService.AddImportResultAsync("CariGrup", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                return (0, 0, errors);
            }

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
                    await _logService.AddImportResultAsync("CariGrup", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                    return (0, 0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    await _logService.AddImportResultAsync("CariGrup", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
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

                string[] required = new[] { "CariGrupId", "CariGrupAdi" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        await _logService.AddImportResultAsync("CariGrup", $"Excel import başarısız. Dosya: {fileName}", errors, GetUserEmail());
                        return (0, 0, errors);
                    }
                }

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var grup = new CariGrup
                        {
                            CariGrupId = GetStringCell(row, headerMap.GetValueOrDefault("CariGrupId")) ?? string.Empty,
                            FirmaId = firmaId,
                            CariGrupAdi = GetStringCell(row, headerMap.GetValueOrDefault("CariGrupAdi")) ?? string.Empty,
                            CariGrupAktifPasif = headerMap.ContainsKey("CariGrupAktifPasif")
                                ? ParseIntCell(row, headerMap.GetValueOrDefault("CariGrupAktifPasif"))
                                : 1
                        };

                        grup.CariGrupId = grup.CariGrupId.Trim();
                        grup.CariGrupAdi = grup.CariGrupAdi.Trim();

                        if (string.IsNullOrWhiteSpace(grup.CariGrupId))
                        {
                            errors.Add($"Satır {r + 1}: Cari Grup Id boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(grup.CariGrupAdi))
                        {
                            errors.Add($"Satır {r + 1}: Cari Grup Adı boş olamaz.");
                            continue;
                        }

                        if (grup.CariGrupAktifPasif != 0 && grup.CariGrupAktifPasif != 1)
                        {
                            errors.Add($"Satır {r + 1}: Cari Grup Aktif/Pasif değeri 0 veya 1 olmalıdır.");
                            continue;
                        }

                        var existing = await context.CariGruplar.FirstOrDefaultAsync(x => x.CariGrupId == grup.CariGrupId && x.FirmaId == grup.FirmaId);

                        if (existing == null)
                        {
                            context.CariGruplar.Add(grup);
                            created++;
                        }
                        else
                        {
                            var hasChange = !string.Equals(existing.CariGrupAdi?.Trim(), grup.CariGrupAdi, StringComparison.Ordinal)
                                || existing.FirmaId != grup.FirmaId
                                || existing.CariGrupAktifPasif != grup.CariGrupAktifPasif;

                            if (hasChange)
                            {
                                var oldSnapshot = new
                                {
                                    existing.CariGrupAdi,
                                    existing.FirmaId,
                                    existing.CariGrupAktifPasif
                                };

                                existing.CariGrupAdi = grup.CariGrupAdi;
                                existing.FirmaId = grup.FirmaId;
                                existing.CariGrupAktifPasif = grup.CariGrupAktifPasif;
                                updated++;

                                await _logService.AddChangeAsync(
                                    "CariGrup",
                                    $"Cari Grup Id: {grup.CariGrupId}, Firma Id: {grup.FirmaId} (import)",
                                    oldSnapshot,
                                    new
                                    {
                                        grup.CariGrupAdi,
                                        grup.FirmaId,
                                        grup.CariGrupAktifPasif
                                    },
                                    GetUserEmail()
                                );
                            }
                        }
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
                    return (0, 0, errors);

                await context.SaveChangesAsync();

                await _logService.AddImportResultAsync(
                    "CariGrup",
                    $"Excel import tamamlandı. Oluşturulan: {created}, Güncellenen: {updated}",
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
                    "CariGrup",
                    "Excel import genel hata.",
                    errors,
                    GetUserEmail()
                );

                return (0, 0, errors);
            }
        }
    }
}