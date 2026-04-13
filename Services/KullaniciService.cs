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
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly PasswordHasher<Kullanici> _passwordHasher;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KullaniciService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
            _passwordHasher = new PasswordHasher<Kullanici>();
        }

        private static List<int> NormalizeFirmaIds(Kullanici kullanici)
        {
            var firmaIds = (kullanici.FirmaIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (firmaIds.Count == 0 && kullanici.FirmaId > 0)
            {
                firmaIds.Add(kullanici.FirmaId);
            }

            return firmaIds;
        }

        public async Task<List<Kullanici>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.KullaniciFirmalari)
                    .ThenInclude(x => x.Firma)
                .OrderBy(x => x.KullaniciAdi)
                .ThenBy(x => x.KullaniciSoyadi)
                .ToListAsync();
        }

        public async Task<Kullanici?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.KullaniciFirmalari)
                    .ThenInclude(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciId == id);
        }

        public async Task<Kullanici?> GetByMailAsync(string mail)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.KullaniciFirmalari)
                    .ThenInclude(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciMail == mail);
        }

        public async Task<Kullanici?> RegisterAsync(Kullanici kullanici)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mevcutKullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciMail == kullanici.KullaniciMail);

            if (mevcutKullanici != null)
                return null;

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);

            context.Kullanicilar.Add(kullanici);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Yeni kayıt (register): {kullanici.KullaniciMail}",
                GetUserEmail()
            );

            return kullanici;
        }

        public async Task<Kullanici?> LoginAsync(string mail, string sifre)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciMail == mail && x.KullaniciAktifPasif == "1");

            if (kullanici == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(kullanici, kullanici.Sifre, sifre);

            if (result == PasswordVerificationResult.Success)
            {
                await _logService.AddAsync(
                    "Bilgi",
                    "Kullanıcı",
                    $"Başarılı login: {mail}",
                    mail
                );

                return kullanici;
            }
            else
            {
                return null;
            }
        }

        public async Task<Kullanici> AddAsync(Kullanici kullanici)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var firmaIds = NormalizeFirmaIds(kullanici);
            if (firmaIds.Count == 0)
                throw new Exception("En az bir firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi))
                throw new Exception("Ad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi))
                throw new Exception("Soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciMail))
                throw new Exception("Mail zorunludur.");

            if (!KullaniciRolleri.IsValid(kullanici.Rol))
                throw new Exception("Geçerli bir rol seçiniz.");

            var normalizedMail = kullanici.KullaniciMail.Trim().ToLower();
            var mailExists = await context.Kullanicilar.AnyAsync(x => x.KullaniciMail.ToLower() == normalizedMail);
            if (mailExists)
                throw new Exception("Bu mail adresi ile kayıtlı kullanıcı zaten var.");

            if (string.IsNullOrWhiteSpace(kullanici.Sifre))
                throw new Exception("Şifre zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                throw new Exception("Aktif/Pasif bilgisi zorunludur.");

            var validFirmaCount = await context.Firmalar.CountAsync(f => firmaIds.Contains(f.FirmaId));
            if (validFirmaCount != firmaIds.Count)
                throw new Exception("Seçilen firmalardan biri veya birkaçı bulunamadı.");

            kullanici.FirmaId = firmaIds[0];
            kullanici.KullaniciMail = kullanici.KullaniciMail.Trim();
            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);

            context.Kullanicilar.Add(kullanici);
            await context.SaveChangesAsync();

            foreach (var firmaId in firmaIds)
            {
                context.KullaniciFirmalari.Add(new KullaniciFirma
                {
                    KullaniciId = kullanici.KullaniciId,
                    FirmaId = firmaId
                });
            }

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Yeni kullanıcı eklendi: {kullanici.KullaniciAdi} {kullanici.KullaniciSoyadi}",
                GetUserEmail()
            );

            return kullanici;
        }

        public async Task<Kullanici?> UpdateAsync(Kullanici kullanici)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existingKullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == kullanici.KullaniciId);

            if (existingKullanici == null)
                return null;

            var firmaIds = NormalizeFirmaIds(kullanici);
            if (firmaIds.Count == 0)
                throw new Exception("En az bir firma seçimi zorunludur.");

            existingKullanici.FirmaId = firmaIds[0];
            existingKullanici.KullaniciAdi = kullanici.KullaniciAdi;
            existingKullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi;
            existingKullanici.KullaniciMail = kullanici.KullaniciMail;
            existingKullanici.KullaniciGsm = kullanici.KullaniciGsm;
            existingKullanici.Rol = kullanici.Rol;
            existingKullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif;

            if (!string.IsNullOrWhiteSpace(kullanici.Sifre))
            {
                existingKullanici.Sifre = _passwordHasher.HashPassword(existingKullanici, kullanici.Sifre);
            }

            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Uyarı",
                "Kullanıcı",
                $"Kullanıcı güncellendi: Id: {kullanici.KullaniciId}",
                GetUserEmail()
            );

            return existingKullanici;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == id);

            if (kullanici == null)
                return false;

            try
            {
                context.Kullanicilar.Remove(kullanici);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Kullancı",
                    $"Kullanıcı silindi: Id: {id}",
                    GetUserEmail()
                );

                return true;
            }
            catch (Exception)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Kullanıcı",
                    $"Kullanıcı silinemedi: Id: {id}",
                    GetUserEmail()
                );

                throw;
            }
        }

        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .Name;
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
            "Sifre"
        };

                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        var message = $"Gerekli sütun '{h}' bulunamadı.";
                        errors.Add(message);

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
                            KullaniciGsm = headerMap.ContainsKey("KullaniciGsm")
                                ? GetStringCell(row, headerMap["KullaniciGsm"]) ?? string.Empty
                                : string.Empty,
                            Sifre = GetStringCell(row, headerMap.GetValueOrDefault("Sifre")) ?? string.Empty,
                            Rol = headerMap.ContainsKey("Rol")
                                ? GetStringCell(row, headerMap["Rol"]) ?? KullaniciRolleri.Standart
                                : KullaniciRolleri.Standart,
                            KullaniciAktifPasif = headerMap.ContainsKey("KullaniciAktifPasif")
                                ? GetStringCell(row, headerMap["KullaniciAktifPasif"]) ?? "1"
                                : "1"
                        };

                        if (kullanici.FirmaId <= 0)
                        {
                            var message = $"Satır {r + 1}: FirmaId geçerli olmalıdır.";
                            errors.Add(message);

                            await _logService.AddAsync(
                                "Hata",
                                "Kullanıcı",
                                message,
                                GetUserEmail()
                            );

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi) ||
                            string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi) ||
                            string.IsNullOrWhiteSpace(kullanici.KullaniciMail) ||
                            string.IsNullOrWhiteSpace(kullanici.Sifre))
                        {
                            var message = $"Satır {r + 1}: Zorunlu alanlar boş olamaz (Ad, Soyad, Mail, Şifre).";
                            errors.Add(message);

                            await _logService.AddAsync(
                                "Hata",
                                "Kullanıcı",
                                message,
                                GetUserEmail()
                            );

                            continue;
                        }

                        var firmaExists = await context.Firmalar.AnyAsync(f => f.FirmaId == kullanici.FirmaId);
                        if (!firmaExists)
                        {
                            var message = $"Satır {r + 1}: FirmaId {kullanici.FirmaId} bulunamadı.";
                            errors.Add(message);

                            await _logService.AddAsync(
                                "Hata",
                                "Kullanıcı",
                                message,
                                GetUserEmail()
                            );

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                        {
                            kullanici.KullaniciAktifPasif = "1";
                        }

                        kullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif.Trim();
                        if (kullanici.KullaniciAktifPasif != "0" && kullanici.KullaniciAktifPasif != "1")
                        {
                            var message = $"Satır {r + 1}: Aktif/Pasif değeri 0 veya 1 olmalıdır.";
                            errors.Add(message);

                            await _logService.AddAsync(
                                "Hata",
                                "Kullanıcı",
                                message,
                                GetUserEmail()
                            );

                            continue;
                        }

                        if (!KullaniciRolleri.IsValid(kullanici.Rol))
                        {
                            var message = $"Satır {r + 1}: Rol yalnızca '{KullaniciRolleri.Standart}' veya '{KullaniciRolleri.Admin}' olabilir.";
                            errors.Add(message);

                            await _logService.AddAsync(
                                "Hata",
                                "Kullanıcı",
                                message,
                                GetUserEmail()
                            );

                            continue;
                        }

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

                        var message = $"Satır {r + 1}: {detail}";
                        errors.Add(message);

                        await _logService.AddAsync(
                            "Hata",
                            "Kullanıcı",
                            message,
                            GetUserEmail()
                        );
                    }
                }

                if (errors.Count > 0)
                {

                    return (0, errors);
                }

                foreach (var (_, kullanici) in prepared)
                {
                    context.Kullanicilar.Add(kullanici);
                }

                await context.SaveChangesAsync();

                foreach (var (_, kullanici) in prepared)
                {
                    context.KullaniciFirmalari.Add(new KullaniciFirma
                    {
                        KullaniciId = kullanici.KullaniciId,
                        FirmaId = kullanici.FirmaId
                    });
                }

                await context.SaveChangesAsync();
                created = prepared.Count;

                await _logService.AddAsync(
                    "Bilgi",
                    "Kullanıcı",
                    $"Excel import tamamlandı. Dosya: {fileName}, Eklenen kullanıcı sayısı: {created}",
                    GetUserEmail()
                );

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