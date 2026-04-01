using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System;
using Npgsql;
using System.Collections.Generic;

namespace EMutabakat.Services
{
    public class KullaniciService : IKullaniciService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<Kullanici> _passwordHasher;

        public KullaniciService(AppDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<Kullanici>();
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
                   "FirmaId",
                   "KullaniciAdi",
                   "KullaniciSoyadi",
                   "KullaniciMail",
                   "Sifre",
                   "KullaniciAktifPasif"
                };

                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        errors.Add($"Gerekli sütun '{h}' bulunamadı.");
                        return (0, errors);
                    }
                }

                var prepared = new List<(int RowNumber, Kullanici Entity)>();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var firmaId = ParseIntCell(row, headerMap.GetValueOrDefault("FirmaId"));
                        var kullanici = new Kullanici
                        {
                            FirmaId = firmaId,
                            KullaniciAdi = GetStringCell(row, headerMap.GetValueOrDefault("KullaniciAdi")) ?? string.Empty,
                            KullaniciSoyadi = GetStringCell(row, headerMap.GetValueOrDefault("KullaniciSoyadi")) ?? string.Empty,
                            KullaniciMail = GetStringCell(row, headerMap.GetValueOrDefault("KullaniciMail")) ?? string.Empty,
                            KullaniciGsm = headerMap.ContainsKey("KullaniciGsm") ? GetStringCell(row, headerMap["KullaniciGsm"]) ?? string.Empty : string.Empty,
                            Sifre = GetStringCell(row, headerMap.GetValueOrDefault("Sifre")) ?? string.Empty,
                            KullaniciAktifPasif = headerMap.ContainsKey("KullaniciAktifPasif") ? GetStringCell(row, headerMap["KullaniciAktifPasif"]) ?? "1" : "1"
                        };

                        if (kullanici.FirmaId <= 0)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId geçerli olmalıdır.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi) || string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi) || string.IsNullOrWhiteSpace(kullanici.KullaniciMail) || string.IsNullOrWhiteSpace(kullanici.Sifre))
                        {
                            errors.Add($"Satır {r + 1}: Zorunlu alanlar boş olamaz (Ad, Soyad, Mail, Şifre).");
                            continue;
                        }

                        var firmaExists = await _db.Firmalar.AnyAsync(f => f.FirmaId == kullanici.FirmaId);
                        if (!firmaExists)
                        {
                            errors.Add($"Satır {r + 1}: FirmaId {kullanici.FirmaId} bulunamadı.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                        {
                            errors.Add($"Satır {r + 1}: Aktif/Pasif bilgisi boş olamaz.");
                            continue;
                        }

                        // Hash password now
                        kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);

                        prepared.Add((r + 1, kullanici));
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

                foreach (var (_, kullanici) in prepared)
                {
                    _db.Kullanicilar.Add(kullanici);
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

        public async Task<List<Kullanici>> GetAllAsync()
        {
            return await _db.Kullanicilar
                .Include(x => x.Firma)
                .OrderBy(x => x.KullaniciAdi)
                .ThenBy(x => x.KullaniciSoyadi)
                .ToListAsync();
        }

        public async Task<Kullanici?> GetByIdAsync(int id)
        {
            return await _db.Kullanicilar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciId == id);
        }

        public async Task<Kullanici?> GetByMailAsync(string mail)
        {
            return await _db.Kullanicilar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciMail == mail);
        }

        public async Task<Kullanici?> RegisterAsync(Kullanici kullanici)
        {
            var mevcutKullanici = await _db.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciMail == kullanici.KullaniciMail);

            if (mevcutKullanici != null)
                return null;

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);
            kullanici.KullaniciAktifPasif ??= "1";

            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync();
            return kullanici;
        }

        public async Task<Kullanici?> LoginAsync(string mail, string sifre)
        {
            var kullanici = await _db.Kullanicilar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciMail == mail && x.KullaniciAktifPasif == "1");

            if (kullanici == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(kullanici, kullanici.Sifre, sifre);

            return result == PasswordVerificationResult.Success ? kullanici : null;
        }

        public async Task<Kullanici> AddAsync(Kullanici kullanici)
        {
            if (kullanici.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi))
                throw new Exception("Ad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi))
                throw new Exception("Soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciMail))
                throw new Exception("Mail zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.Sifre))
                throw new Exception("Şifre zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                throw new Exception("Aktif/Pasif bilgisi zorunludur.");

            var firmaExists = await _db.Firmalar.AnyAsync(f => f.FirmaId == kullanici.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);

            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync();
            return kullanici;
        }

        public async Task<Kullanici?> UpdateAsync(Kullanici kullanici)
        {
            var existingKullanici = await _db.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == kullanici.KullaniciId);

            if (existingKullanici == null)
                return null;

            if (kullanici.FirmaId <= 0)
                throw new Exception("Firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi))
                throw new Exception("Ad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi))
                throw new Exception("Soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciMail))
                throw new Exception("Mail zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                throw new Exception("Aktif/Pasif bilgisi zorunludur.");

            var firmaExists = await _db.Firmalar.AnyAsync(f => f.FirmaId == kullanici.FirmaId);
            if (!firmaExists)
                throw new Exception("Seçilen firma bulunamadı.");

            existingKullanici.FirmaId = kullanici.FirmaId;
            existingKullanici.KullaniciAdi = kullanici.KullaniciAdi;
            existingKullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi;
            existingKullanici.KullaniciMail = kullanici.KullaniciMail;
            existingKullanici.KullaniciGsm = kullanici.KullaniciGsm;
            existingKullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif;

            if (!string.IsNullOrWhiteSpace(kullanici.Sifre))
            {
                existingKullanici.Sifre = _passwordHasher.HashPassword(existingKullanici, kullanici.Sifre);
            }

            await _db.SaveChangesAsync();
            return existingKullanici;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var kullanici = await _db.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == id);

            if (kullanici == null)
                return false;

            try
            {
                _db.Kullanicilar.Remove(kullanici);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
                {
                    throw new Exception("Bu kullanıcı başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Kullanıcı silinirken bir veritabanı hatası oluştu.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("association between entity types") ||
                    ex.Message.Contains("relationship") ||
                    ex.Message.Contains("severed"))
                {
                    throw new Exception("Bu kullanıcı başka kayıtlarda kullanıldığı için silinemez.");
                }

                throw new Exception("Kullanıcı silinirken bir işlem hatası oluştu.");
            }
            catch
            {
                throw new Exception("Kullanıcı silinirken bir hata oluştu.");
            }
        }
    }
}