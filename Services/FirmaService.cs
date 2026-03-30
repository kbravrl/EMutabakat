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
    public class FirmaService : IFirmaService
    {
        private readonly AppDbContext _db;

        public FirmaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Firma>> GetAllAsync()
        {
            return await _db.Firmalar
                .OrderBy(x => x.FirmaAdi)
                .ToListAsync();
        }

        public async Task<Firma?> GetByIdAsync(int id)
        {
            return await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);
        }

        public async Task<Firma> AddAsync(Firma firma)
        {
            _db.Firmalar.Add(firma);
            await _db.SaveChangesAsync();
            return firma;
        }

        public async Task<Firma?> UpdateAsync(Firma firma)
        {
            var existingFirma = await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == firma.FirmaId);

            if (existingFirma == null)
                return null;

            existingFirma.FirmaAdi = firma.FirmaAdi;
            existingFirma.FirmaUnvan = firma.FirmaUnvan;
            existingFirma.FirmaAdres = firma.FirmaAdres;
            existingFirma.FirmaIlce = firma.FirmaIlce;
            existingFirma.FirmaIl = firma.FirmaIl;
            existingFirma.FirmaVergiDairesi = firma.FirmaVergiDairesi;
            existingFirma.FirmaVergiNumarasi = firma.FirmaVergiNumarasi;
            existingFirma.FirmaMersisNumarasi = firma.FirmaMersisNumarasi;
            existingFirma.FirmaWebAdresi = firma.FirmaWebAdresi;
            existingFirma.FirmaYetkiliAdiSoyadi = firma.FirmaYetkiliAdiSoyadi;
            existingFirma.FirmaMail = firma.FirmaMail;
            existingFirma.FirmaTelefon = firma.FirmaTelefon;
            existingFirma.FirmaGsm = firma.FirmaGsm;
            existingFirma.FirmaSmtpHost = firma.FirmaSmtpHost;
            existingFirma.FirmaSmtpPort = firma.FirmaSmtpPort;
            existingFirma.FirmaSmtpUser = firma.FirmaSmtpUser;
            existingFirma.FirmaSmtpPassword = firma.FirmaSmtpPassword;
            existingFirma.FirmaSmtpSecure = firma.FirmaSmtpSecure;
            existingFirma.FirmaAktifPasif = firma.FirmaAktifPasif;

            await _db.SaveChangesAsync();
            return existingFirma;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var firma = await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);

            if (firma == null)
                return false;

            _db.Firmalar.Remove(firma);
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

                string[] required = new[] { "FirmaAdi", "FirmaMail" };
                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        return (0, errors);
                    }
                }

                var prepared = new List<(int RowNumber, Firma Entity)>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var firma = new Firma
                        {
                            FirmaAdi = GetStringCell(row, headerMap.GetValueOrDefault("FirmaAdi")) ?? string.Empty,
                            FirmaUnvan = headerMap.ContainsKey("FirmaUnvan") ? GetStringCell(row, headerMap["FirmaUnvan"]) ?? string.Empty : string.Empty,
                            FirmaAdres = headerMap.ContainsKey("FirmaAdres") ? GetStringCell(row, headerMap["FirmaAdres"]) ?? string.Empty : string.Empty,
                            FirmaIlce = headerMap.ContainsKey("FirmaIlce") ? GetStringCell(row, headerMap["FirmaIlce"]) ?? string.Empty : string.Empty,
                            FirmaIl = headerMap.ContainsKey("FirmaIl") ? GetStringCell(row, headerMap["FirmaIl"]) ?? string.Empty : string.Empty,
                            FirmaVergiDairesi = headerMap.ContainsKey("FirmaVergiDairesi") ? GetStringCell(row, headerMap["FirmaVergiDairesi"]) ?? string.Empty : string.Empty,
                            FirmaVergiNumarasi = headerMap.ContainsKey("FirmaVergiNumarasi") ? GetStringCell(row, headerMap["FirmaVergiNumarasi"]) ?? string.Empty : string.Empty,
                            FirmaMersisNumarasi = headerMap.ContainsKey("FirmaMersisNumarasi") ? GetStringCell(row, headerMap["FirmaMersisNumarasi"]) ?? string.Empty : string.Empty,
                            FirmaWebAdresi = headerMap.ContainsKey("FirmaWebAdresi") ? GetStringCell(row, headerMap["FirmaWebAdresi"]) ?? string.Empty : string.Empty,
                            FirmaYetkiliAdiSoyadi = headerMap.ContainsKey("FirmaYetkiliAdiSoyadi") ? GetStringCell(row, headerMap["FirmaYetkiliAdiSoyadi"]) ?? string.Empty : string.Empty,
                            FirmaMail = GetStringCell(row, headerMap.GetValueOrDefault("FirmaMail")) ?? string.Empty,
                            FirmaTelefon = headerMap.ContainsKey("FirmaTelefon") ? GetStringCell(row, headerMap["FirmaTelefon"]) ?? string.Empty : string.Empty,
                            FirmaGsm = headerMap.ContainsKey("FirmaGsm") ? GetStringCell(row, headerMap["FirmaGsm"]) ?? string.Empty : string.Empty,
                            FirmaSmtpHost = headerMap.ContainsKey("FirmaSmtpHost") ? GetStringCell(row, headerMap["FirmaSmtpHost"]) ?? string.Empty : string.Empty,
                            FirmaSmtpPort = headerMap.ContainsKey("FirmaSmtpPort") ? ParseIntCell(row, headerMap["FirmaSmtpPort"]) : 0,
                            FirmaSmtpUser = headerMap.ContainsKey("FirmaSmtpUser") ? GetStringCell(row, headerMap["FirmaSmtpUser"]) ?? string.Empty : string.Empty,
                            FirmaSmtpPassword = headerMap.ContainsKey("FirmaSmtpPassword") ? GetStringCell(row, headerMap["FirmaSmtpPassword"]) ?? string.Empty : string.Empty,
                            FirmaSmtpSecure = headerMap.ContainsKey("FirmaSmtpSecure") ? GetStringCell(row, headerMap["FirmaSmtpSecure"]) ?? string.Empty : string.Empty,
                            FirmaAktifPasif = headerMap.ContainsKey("FirmaAktifPasif") ? ParseIntCell(row, headerMap["FirmaAktifPasif"]) : 1
                        };

                        if (string.IsNullOrWhiteSpace(firma.FirmaAdi))
                        {
                            errors.Add($"Satır {r + 1}: FirmaAdi boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaMail))
                        {
                            errors.Add($"Satır {r + 1}: FirmaMail boş olamaz.");
                            continue;
                        }

                        prepared.Add((r + 1, firma));
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

                foreach (var (_, firma) in prepared)
                {
                    _db.Firmalar.Add(firma);
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