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
    public class CariService : ICariService
    {
        private readonly AppDbContext _context;

        public CariService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cari>> GetAllAsync()
        {
            return await _context.Cariler
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .OrderBy(x => x.CariAdi)
                .ToListAsync();
        }

        public async Task<Cari?> GetByIdAsync(int id)
        {
            return await _context.Cariler
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .FirstOrDefaultAsync(x => x.CariId == id);
        }

        public async Task<Cari> AddAsync(Cari cari)
        {
            _context.Cariler.Add(cari);
            await _context.SaveChangesAsync();
            return cari;
        }

        public async Task<Cari?> UpdateAsync(Cari cari)
        {
            var existingCari = await _context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == cari.CariId);

            if (existingCari == null)
                return null;

            existingCari.FirmaId = cari.FirmaId;
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

            await _context.SaveChangesAsync();
            return existingCari;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cari = await _context.Cariler.FirstOrDefaultAsync(x => x.CariId == id);

            if (cari == null)
                return false;

            _context.Cariler.Remove(cari);
            await _context.SaveChangesAsync();
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

                string[] required = new[] { "CariAdi", "FirmaId" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        return (0, errors);
                    }
                }

                var prepared = new List<(int RowNumber, Cari Entity)>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var firmaId = ParseIntCell(row, headerMap.GetValueOrDefault("FirmaId"));
                        var cari = new Cari
                        {
                            FirmaId = firmaId,
                            CariAdi = GetStringCell(row, headerMap.GetValueOrDefault("CariAdi")) ?? string.Empty,
                            CariUnvan = headerMap.ContainsKey("CariUnvan") ? GetStringCell(row, headerMap["CariUnvan"]) ?? string.Empty : string.Empty,
                            CariAdres = headerMap.ContainsKey("CariAdres") ? GetStringCell(row, headerMap["CariAdres"]) ?? string.Empty : string.Empty,
                            CariIlce = headerMap.ContainsKey("CariIlce") ? GetStringCell(row, headerMap["CariIlce"]) ?? string.Empty : string.Empty,
                            CariIl = headerMap.ContainsKey("CariIl") ? GetStringCell(row, headerMap["CariIl"]) ?? string.Empty : string.Empty,
                            CariVergiDairesi = headerMap.ContainsKey("CariVergiDairesi") ? GetStringCell(row, headerMap["CariVergiDairesi"]) ?? string.Empty : string.Empty,
                            CariVergiNumarasi = headerMap.ContainsKey("CariVergiNumarasi") ? GetStringCell(row, headerMap["CariVergiNumarasi"]) ?? string.Empty : string.Empty,
                            CariWebAdresi = headerMap.ContainsKey("CariWebAdresi") ? GetStringCell(row, headerMap["CariWebAdresi"]) ?? string.Empty : string.Empty,
                            CariYetkiliAdiSoyadi = headerMap.ContainsKey("CariYetkiliAdiSoyadi") ? GetStringCell(row, headerMap["CariYetkiliAdiSoyadi"]) ?? string.Empty : string.Empty,
                            CariYetkiliTelefon = headerMap.ContainsKey("CariYetkiliTelefon") ? GetStringCell(row, headerMap["CariYetkiliTelefon"]) ?? string.Empty : string.Empty,
                            CariYetkiliGsm = headerMap.ContainsKey("CariYetkiliGsm") ? GetStringCell(row, headerMap["CariYetkiliGsm"]) ?? string.Empty : string.Empty,
                            CariYetkiliMail = headerMap.ContainsKey("CariYetkiliMail") ? GetStringCell(row, headerMap["CariYetkiliMail"]) ?? string.Empty : string.Empty,
                            CariGrupId = headerMap.ContainsKey("CariGrupId") ? ParseIntCell(row, headerMap["CariGrupId"]) : 0,
                            CariDovizKodu = headerMap.ContainsKey("CariDovizKodu") ? ParseIntCell(row, headerMap["CariDovizKodu"]) : 0,
                            CariAktifPasif = headerMap.ContainsKey("CariAktifPasif") ? ParseIntCell(row, headerMap["CariAktifPasif"]) : 1
                        };

                        if (cari.FirmaId <= 0)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId geçerli olmalıdır.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(cari.CariAdi))
                        {
                            errors.Add($"Satır {r + 1}: CariAdi boş olamaz.");
                            continue;
                        }

                        var firmaExists = await _context.Firmalar.AnyAsync(f => f.FirmaId == cari.FirmaId);
                        if (!firmaExists)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId {cari.FirmaId} bulunamadı.");
                            continue;
                        }

                        prepared.Add((r + 1, cari));
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

                foreach (var (_, cari) in prepared)
                {
                    _context.Cariler.Add(cari);
                }

                await _context.SaveChangesAsync();
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