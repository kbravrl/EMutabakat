using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System;
using System.Collections.Generic;

namespace EMutabakat.Services
{
    public class CariGrupService : ICariGrupService
    {
        private readonly AppDbContext _db;

        public CariGrupService(AppDbContext db)
        {
            _db = db;
        }
      
        public async Task<List<CariGrup>> GetAllAsync()
        {
            return await _db.CariGruplar
                .Include(x => x.Firma)
                .OrderBy(x => x.CariGrupAdi)
                .ToListAsync();
        }

        public async Task<CariGrup?> GetByIdAsync(int id)
        {
            return await _db.CariGruplar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.CariGrupId == id);
        }

        public async Task<CariGrup> AddAsync(CariGrup cariGrup)
        {
            if (cariGrup.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(cariGrup.CariGrupAdi))
                throw new Exception("Cari grup adı zorunludur.");

            var firmaExists = await _db.Firmalar.AnyAsync(f => f.FirmaId == cariGrup.FirmaId);

            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            _db.CariGruplar.Add(cariGrup);
            await _db.SaveChangesAsync();

            return cariGrup;
        }

        public async Task<CariGrup?> UpdateAsync(CariGrup cariGrup)
        {
            var existingCariGrup = await _db.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.CariGrupId);

            if (existingCariGrup == null)
                return null;

            existingCariGrup.FirmaId = cariGrup.FirmaId;
            existingCariGrup.CariGrupAdi = cariGrup.CariGrupAdi;

            await _db.SaveChangesAsync();
            return existingCariGrup;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cariGrup = await _db.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == id);

            if (cariGrup == null)
                return false;

            _db.CariGruplar.Remove(cariGrup);
            await _db.SaveChangesAsync();
            return true;
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

        public async Task<(int created, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName)
        {
            var errors = new List<string>();
            var created = 0;

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
                    return (0, errors);
                }

                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    errors.Add("Excel başlık satırı bulunamadı.");
                    return (0, errors);
                }

                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell == null) continue;
                    var text = cell.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text)) headerMap[text] = i;
                }

                string[] required = new[] { "FirmaId", "CariGrupAdi" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        return (0, errors);
                    }
                }

                var prepared = new List<(int RowNumber, CariGrup Entity)>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var firmaId = ParseIntCell(row, headerMap.GetValueOrDefault("FirmaId"));
                        var grup = new CariGrup
                        {
                            FirmaId = firmaId,
                            CariGrupAdi = GetStringCell(row, headerMap.GetValueOrDefault("CariGrupAdi")) ?? string.Empty
                        };

                        if (grup.FirmaId <= 0)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId geçerli olmalıdır.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(grup.CariGrupAdi))
                        {
                            errors.Add($"Satır {r + 1}: CariGrupAdi boş olamaz.");
                            continue;
                        }

                        var firmaExists = await _db.Firmalar.AnyAsync(f => f.FirmaId == grup.FirmaId);
                        if (!firmaExists)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId {grup.FirmaId} bulunamadı.");
                            continue;
                        }

                        prepared.Add((r + 1, grup));
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
                    return (0, errors);
                }

                foreach (var (_, grup) in prepared)
                {
                    _db.CariGruplar.Add(grup);
                }

                await _db.SaveChangesAsync();
                created = prepared.Count;

                return (created, errors);
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
                return (0, errors);
            }
        }
    }
}