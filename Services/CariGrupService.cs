using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
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

        public CariGrupService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
      
        public async Task<List<CariGrup>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.CariGruplar
                .AsNoTracking()
                .Include(x => x.Firma)
                .OrderByDescending(x => x.CariGrupAktifPasif)
                .ThenBy(x => x.CariGrupAdi)
                .ToListAsync();
        }

        public async Task<CariGrup?> GetByIdAsync(string id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.CariGruplar
                .AsNoTracking()
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.CariGrupId == id);
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

            var exists = await context.CariGruplar.AnyAsync(x => x.CariGrupId == cariGrup.CariGrupId);
            if (exists)
                throw new Exception("Bu cari grup ID zaten kullanılıyor.");

            var sameNameExists = await context.CariGruplar
                .AnyAsync(x => x.CariGrupAdi.ToLower() == cariGrup.CariGrupAdi.ToLower());
            if (sameNameExists)
                throw new Exception("Bu cari grup adı zaten kullanılıyor.");

            var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == cariGrup.FirmaId);

            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            context.CariGruplar.Add(cariGrup);
            await context.SaveChangesAsync();

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
                .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.OriginalCariGrupId);

            if (existingCariGrup == null)
                return null;

            var sameNameExists = await context.CariGruplar
                .AnyAsync(x => x.CariGrupId != cariGrup.OriginalCariGrupId && x.CariGrupAdi.ToLower() == cariGrup.CariGrupAdi.ToLower());
            if (sameNameExists)
                throw new Exception("Bu cari grup adı zaten kullanılıyor.");

            if (!string.Equals(cariGrup.OriginalCariGrupId, cariGrup.CariGrupId, StringComparison.Ordinal))
            {
                var newIdExists = await context.CariGruplar.AnyAsync(x => x.CariGrupId == cariGrup.CariGrupId);
                if (newIdExists)
                    throw new Exception("Bu cari grup ID zaten kullanılıyor.");

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
                    .Where(c => c.CariGrupId == cariGrup.OriginalCariGrupId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.CariGrupId, cariGrup.CariGrupId));

                context.CariGruplar.Remove(existingCariGrup);
                await context.SaveChangesAsync();

                return await context.CariGruplar
                    .Include(x => x.Firma)
                    .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.CariGrupId);
            }

            existingCariGrup.FirmaId = cariGrup.FirmaId;
            existingCariGrup.CariGrupAdi = cariGrup.CariGrupAdi;
            existingCariGrup.CariGrupAktifPasif = cariGrup.CariGrupAktifPasif;

            await context.SaveChangesAsync();
            return existingCariGrup;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedId = id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedId))
                return false;

            var cariGrup = await context.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == normalizedId);

            if (cariGrup == null)
                return false;

            try
            {
                context.CariGruplar.Remove(cariGrup);
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
                {
                    throw new Exception("Bu cari grup başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Cari grup silinirken bir veritabanı hatası oluştu.");
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
            catch
            {
                throw new Exception("Cari grup silinirken bir hata oluştu.");
            }
        }

        // Helper parsers
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

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (firmaId <= 0)
            {
                errors.Add("Firma seçimi zorunludur.");
                return (0, 0, errors);
            }

            var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == firmaId);
            if (!firmaExists)
            {
                errors.Add("Seçilen firma bulunamadı.");
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

                string[] required = new[] { "CariGrupId", "CariGrupAdi" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
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
                            errors.Add($"Satır {r + 1}: CariGrupId boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(grup.CariGrupAdi))
                        {
                            errors.Add($"Satır {r + 1}: CariGrupAdi boş olamaz.");
                            continue;
                        }

                        if (grup.CariGrupAktifPasif != 0 && grup.CariGrupAktifPasif != 1)
                        {
                            errors.Add($"Satır {r + 1}: CariGrupAktifPasif değeri 0 veya 1 olmalıdır.");
                            continue;
                        }

                        var normalizedName = grup.CariGrupAdi.Trim().ToLowerInvariant();

                        var existing = await context.CariGruplar.FirstOrDefaultAsync(x => x.CariGrupId == grup.CariGrupId);
                        var sameNameLocal = context.CariGruplar.Local
                            .FirstOrDefault(x => x.CariGrupAdi != null
                                && x.CariGrupAdi.Trim().ToLower() == normalizedName);

                        CariGrup? sameNameDb = null;
                        if (sameNameLocal == null)
                        {
                            sameNameDb = await context.CariGruplar
                                .FirstOrDefaultAsync(x => x.CariGrupAdi.ToLower() == normalizedName);
                        }

                        var sameNameCariGrupId = sameNameLocal?.CariGrupId ?? sameNameDb?.CariGrupId;

                        if (sameNameCariGrupId != null && !string.Equals(sameNameCariGrupId, grup.CariGrupId, StringComparison.Ordinal))
                        {
                            errors.Add($"Satır {r + 1}: CariGrupAdi '{grup.CariGrupAdi}' zaten başka bir ID ile kayıtlı.");
                            continue;
                        }

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
                                existing.CariGrupAdi = grup.CariGrupAdi;
                                existing.FirmaId = grup.FirmaId;
                                existing.CariGrupAktifPasif = grup.CariGrupAktifPasif;
                                updated++;
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
                {
                    return (0, 0, errors);
                }

                await context.SaveChangesAsync();

                return (created, updated, errors);
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