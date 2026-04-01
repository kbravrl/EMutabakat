using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

            try
            {
                _db.Firmalar.Remove(firma);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
                {
                    throw new Exception("Bu firma başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Firma silinirken bir veritabanı hatası oluştu.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("association between entity types") ||
                    ex.Message.Contains("relationship") ||
                    ex.Message.Contains("severed"))
                {
                    throw new Exception("Bu firma başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Firma silinirken bir işlem hatası oluştu.");
            }
            catch
            {
                throw new Exception("Firma silinirken bir hata oluştu.");
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

                string[] required = new[]
                {
                   "FirmaAdi",
                   "FirmaVergiDairesi",
                   "FirmaVergiNumarasi",
                   "FirmaYetkiliAdiSoyadi",
                   "FirmaMail",
                   "FirmaTelefon",
                   "FirmaSmtpHost",
                   "FirmaSmtpPort",
                   "FirmaSmtpUser",
                   "FirmaSmtpPassword",
                   "FirmaSmtpSecure",
                   "FirmaAktifPasif"
                };

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
                            FirmaAdi = GetStringCell(row, headerMap["FirmaAdi"]) ?? string.Empty,
                            FirmaUnvan = headerMap.ContainsKey("FirmaUnvan") ? GetStringCell(row, headerMap["FirmaUnvan"]) : null,
                            FirmaAdres = headerMap.ContainsKey("FirmaAdres") ? GetStringCell(row, headerMap["FirmaAdres"]) : null,
                            FirmaIlce = headerMap.ContainsKey("FirmaIlce") ? GetStringCell(row, headerMap["FirmaIlce"]) : null,
                            FirmaIl = headerMap.ContainsKey("FirmaIl") ? GetStringCell(row, headerMap["FirmaIl"]) : null,
                            FirmaVergiDairesi = GetStringCell(row, headerMap["FirmaVergiDairesi"]) ?? string.Empty,
                            FirmaVergiNumarasi = GetStringCell(row, headerMap["FirmaVergiNumarasi"]) ?? string.Empty,
                            FirmaMersisNumarasi = headerMap.ContainsKey("FirmaMersisNumarasi") ? GetStringCell(row, headerMap["FirmaMersisNumarasi"]) : null,
                            FirmaWebAdresi = headerMap.ContainsKey("FirmaWebAdresi") ? GetStringCell(row, headerMap["FirmaWebAdresi"]) : null,
                            FirmaYetkiliAdiSoyadi = GetStringCell(row, headerMap["FirmaYetkiliAdiSoyadi"]) ?? string.Empty,
                            FirmaMail = GetStringCell(row, headerMap["FirmaMail"]) ?? string.Empty,
                            FirmaTelefon = GetStringCell(row, headerMap["FirmaTelefon"]) ?? string.Empty,
                            FirmaGsm = headerMap.ContainsKey("FirmaGsm") ? GetStringCell(row, headerMap["FirmaGsm"]) : null,
                            FirmaSmtpHost = GetStringCell(row, headerMap["FirmaSmtpHost"]) ?? string.Empty,
                            FirmaSmtpPort = ParseIntCell(row, headerMap["FirmaSmtpPort"]),
                            FirmaSmtpUser = GetStringCell(row, headerMap["FirmaSmtpUser"]) ?? string.Empty,
                            FirmaSmtpPassword = GetStringCell(row, headerMap["FirmaSmtpPassword"]) ?? string.Empty,
                            FirmaSmtpSecure = GetStringCell(row, headerMap["FirmaSmtpSecure"]) ?? string.Empty,
                            FirmaAktifPasif = ParseIntCell(row, headerMap["FirmaAktifPasif"])
                        };


                        if (string.IsNullOrWhiteSpace(firma.FirmaAdi))
                        {
                            errors.Add($"Satır {r + 1}: Firma adı boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaVergiDairesi))
                        {
                            errors.Add($"Satır {r + 1}: Vergi dairesi boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaVergiNumarasi))
                        {
                            errors.Add($"Satır {r + 1}: Vergi numarası boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaYetkiliAdiSoyadi))
                        {
                            errors.Add($"Satır {r + 1}: Yetkili adı soyadı boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaMail))
                        {
                            errors.Add($"Satır {r + 1}: Mail adresi boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaTelefon))
                        {
                            errors.Add($"Satır {r + 1}: Telefon boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaSmtpHost))
                        {
                            errors.Add($"Satır {r + 1}: SMTP Host boş olamaz.");
                            continue;
                        }

                        if (firma.FirmaSmtpPort <= 0)
                        {
                            errors.Add($"Satır {r + 1}: SMTP Port geçerli bir değer olmalıdır.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaSmtpUser))
                        {
                            errors.Add($"Satır {r + 1}: SMTP kullanıcı adı boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaSmtpPassword))
                        {
                            errors.Add($"Satır {r + 1}: SMTP şifresi boş olamaz.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firma.FirmaSmtpSecure))
                        {
                            errors.Add($"Satır {r + 1}: SMTP Secure bilgisi boş olamaz.");
                            continue;
                        }

                        if (firma.FirmaAktifPasif != 0 && firma.FirmaAktifPasif != 1)
                        {
                            errors.Add($"Satır {r + 1}: Aktif/Pasif değeri 0 veya 1 olmalıdır.");
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