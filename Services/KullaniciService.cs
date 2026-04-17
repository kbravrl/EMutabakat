using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.RegularExpressions;

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
                .Include(x => x.Firmalar)
                .OrderBy(x => x.KullaniciAdi)
                .ThenBy(x => x.KullaniciSoyadi)
                .ToListAsync();
        }

        public async Task<Kullanici?> GetByIdAsync(string id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Firmalar)
                .FirstOrDefaultAsync(x => x.KullaniciId == id);
        }

        public async Task<string> GenerateNextKullaniciIdAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = await context.Kullanicilar
                .AsNoTracking()
                .Select(x => x.KullaniciId)
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

        public async Task<Kullanici?> GetByMailAsync(string mail)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedMail = mail?.Trim().ToLower();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firma)
                .Include(x => x.Firmalar)
                .FirstOrDefaultAsync(x => x.KullaniciMail == normalizedMail);
        }

        public async Task<Kullanici?> RegisterAsync(Kullanici kullanici)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            kullanici.KullaniciMail = kullanici.KullaniciMail?.Trim().ToLower();

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

            var normalizedMail = mail?.Trim().ToLower();

            var kullanici = await context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciMail == normalizedMail && x.KullaniciAktifPasif == "1");

            if (kullanici == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(kullanici, kullanici.Sifre, sifre);

            if (result == PasswordVerificationResult.Success)
            {
                await _logService.AddAsync(
                    "Bilgi",
                    "Kullanıcı",
                    $"Başarılı login: {normalizedMail}",
                    normalizedMail
                );

                return kullanici;
            }
            return null;
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

            kullanici.KullaniciMail = kullanici.KullaniciMail.Trim().ToLower();

            var mailExists = await context.Kullanicilar.AnyAsync(x => x.KullaniciMail == kullanici.KullaniciMail);
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
            kullanici.KullaniciId = await GenerateNextKullaniciIdAsync();
            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre);

            context.Kullanicilar.Add(kullanici);
            await context.SaveChangesAsync();

            var firmalar = await context.Firmalar.Where(f => firmaIds.Contains(f.FirmaId)).ToListAsync();
            foreach (var firma in firmalar)
            {
                kullanici.Firmalar.Add(firma);
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

            var validFirmaCount = await context.Firmalar.CountAsync(f => firmaIds.Contains(f.FirmaId));
            if (validFirmaCount != firmaIds.Count)
                throw new Exception("Seçilen firmalardan biri veya birkaçı bulunamadı.");

            var normalizedMail = kullanici.KullaniciMail?.Trim().ToLower();

            var mailExists = await context.Kullanicilar.AnyAsync(x =>
                x.KullaniciId != kullanici.KullaniciId &&
                x.KullaniciMail == normalizedMail);

            if (mailExists)
                throw new Exception("Bu mail adresi ile kayıtlı başka bir kullanıcı zaten var.");

            existingKullanici.FirmaId = firmaIds[0];
            existingKullanici.KullaniciAdi = kullanici.KullaniciAdi;
            existingKullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi;
            existingKullanici.KullaniciMail = normalizedMail;
            existingKullanici.KullaniciGsm = kullanici.KullaniciGsm;
            existingKullanici.Rol = kullanici.Rol;
            existingKullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif;

            if (!string.IsNullOrWhiteSpace(kullanici.Sifre))
            {
                existingKullanici.Sifre = _passwordHasher.HashPassword(existingKullanici, kullanici.Sifre);
            }

            await context.Entry(existingKullanici).Collection(k => k.Firmalar).LoadAsync();

            var desired = await context.Firmalar.Where(f => firmaIds.Contains(f.FirmaId)).ToListAsync();

            existingKullanici.Firmalar.Clear();
            foreach (var f in desired)
            {
                existingKullanici.Firmalar.Add(f);
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

        public async Task<bool> DeleteAsync(string id)
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

        private static string? GetStringCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            return cell?.ToString();
        }

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, List<int> firmaIds)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Excel import başladı. Dosya: {fileName}",
                GetUserEmail()
            );

            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                if (firmaIds == null || !firmaIds.Any())
                {
                    errors.Add("En az bir firma seçilmelidir.");
                    return (0, 0, errors);
                }

                firmaIds = firmaIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (!firmaIds.Any())
                {
                    errors.Add("Geçerli firma seçimi yapılmadı.");
                    return (0, 0, errors);
                }

                var validFirmaCount = await context.Firmalar.CountAsync(f => firmaIds.Contains(f.FirmaId));
                if (validFirmaCount != firmaIds.Count)
                {
                    errors.Add("Seçilen firmalardan biri veya birkaçı bulunamadı.");
                    return (0, 0, errors);
                }

                IWorkbook workbook;
                var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

                if (ext == ".xlsx")
                    workbook = new XSSFWorkbook(stream);
                else if (ext == ".xls")
                    workbook = new HSSFWorkbook(stream);
                else
                {
                    errors.Add("Desteklenmeyen dosya uzantısı. Sadece .xlsx veya .xls kabul edilir.");
                    return (0, 0, errors);
                }

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
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        headerMap[text] = i;
                    }
                }

                string[] required = new[]
                {
            "KullaniciId",
            "KullaniciAdi",
            "KullaniciSoyadi",
            "KullaniciMail"
        };

                foreach (var h in required)
                {
                    if (!headerMap.ContainsKey(h))
                    {
                        var message = $"Gerekli sütun '{h}' bulunamadı.";
                        errors.Add(message);

                        await _logService.AddAsync(
                            "Hata",
                            "Kullanıcı",
                            message,
                            GetUserEmail()
                        );

                        return (0, 0, errors);
                    }
                }

                var selectedFirmalar = await context.Firmalar
                    .Where(f => firmaIds.Contains(f.FirmaId))
                    .ToListAsync();

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    try
                    {
                        var kullaniciId = GetStringCell(row, headerMap["KullaniciId"])?.Trim() ?? string.Empty;
                        var kullaniciMail = GetStringCell(row, headerMap["KullaniciMail"])?.Trim() ?? string.Empty;

                        var rol = headerMap.ContainsKey("Rol")
                            ? (GetStringCell(row, headerMap["Rol"])?.Trim() ?? KullaniciRolleri.Standart)
                            : KullaniciRolleri.Standart;

                        var aktifPasif = headerMap.ContainsKey("KullaniciAktifPasif")
                            ? (GetStringCell(row, headerMap["KullaniciAktifPasif"])?.Trim() ?? "1")
                            : "1";

                        var kullaniciAdi = GetStringCell(row, headerMap["KullaniciAdi"])?.Trim() ?? string.Empty;
                        var kullaniciSoyadi = GetStringCell(row, headerMap["KullaniciSoyadi"])?.Trim() ?? string.Empty;
                        var kullaniciGsm = headerMap.ContainsKey("KullaniciGsm")
                            ? GetStringCell(row, headerMap["KullaniciGsm"])?.Trim() ?? string.Empty
                            : string.Empty;
                        var sifre = headerMap.ContainsKey("Sifre")
                            ? GetStringCell(row, headerMap["Sifre"])?.Trim() ?? string.Empty
                            : string.Empty;

                        if (string.IsNullOrWhiteSpace(kullaniciId))
                        {
                            var message = $"Satır {r + 1}: KullaniciId boş olamaz.";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(kullaniciAdi) ||
                            string.IsNullOrWhiteSpace(kullaniciSoyadi) ||
                            string.IsNullOrWhiteSpace(kullaniciMail))
                        {
                            var message = $"Satır {r + 1}: Zorunlu alanlar boş olamaz (KullaniciAdi, KullaniciSoyadi, KullaniciMail).";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        if (!KullaniciRolleri.IsValid(rol))
                        {
                            var message = $"Satır {r + 1}: Rol yalnızca '{KullaniciRolleri.Standart}' veya '{KullaniciRolleri.Admin}' olabilir.";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        if (aktifPasif != "0" && aktifPasif != "1")
                        {
                            var message = $"Satır {r + 1}: KullaniciAktifPasif değeri yalnızca 0 veya 1 olabilir.";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        var duplicateIdInExcel = false;
                        for (int i = 1; i < r; i++)
                        {
                            var previousRow = sheet.GetRow(i);
                            if (previousRow == null) continue;

                            var previousId = GetStringCell(previousRow, headerMap["KullaniciId"])?.Trim() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(previousId) &&
                                previousId.Equals(kullaniciId, StringComparison.OrdinalIgnoreCase))
                            {
                                duplicateIdInExcel = true;
                                break;
                            }
                        }

                        if (duplicateIdInExcel)
                        {
                            var message = $"Satır {r + 1}: '{kullaniciId}' değeri Excel içinde tekrar ediyor.";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        var normalizedMail = kullaniciMail.Trim().ToLower();

                        var mailOwner = await context.Kullanicilar
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.KullaniciMail == normalizedMail);

                        if (mailOwner != null && !mailOwner.KullaniciId.Equals(kullaniciId, StringComparison.OrdinalIgnoreCase))
                        {
                            var message = $"Satır {r + 1}: '{kullaniciMail}' mail adresi başka bir kullanıcıda kayıtlı.";
                            errors.Add(message);
                            await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                            continue;
                        }

                        var existingUser = await context.Kullanicilar
                            .Include(x => x.Firmalar)
                            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

                        if (existingUser == null)
                        {
                            if (string.IsNullOrWhiteSpace(sifre))
                            {
                                var message = $"Satır {r + 1}: Yeni kullanıcı için şifre zorunludur.";
                                errors.Add(message);
                                await _logService.AddAsync("Hata", "Kullanıcı", message, GetUserEmail());
                                continue;
                            }

                            var yeniKullanici = new Kullanici
                            {
                                KullaniciId = kullaniciId,
                                FirmaId = firmaIds[0],
                                FirmaIds = firmaIds.ToList(),
                                KullaniciAdi = kullaniciAdi.Trim(),
                                KullaniciSoyadi = kullaniciSoyadi.Trim(),
                                KullaniciMail = normalizedMail,
                                KullaniciGsm = kullaniciGsm.Trim(),
                                Sifre = string.Empty,
                                Rol = rol.Trim(),
                                KullaniciAktifPasif = aktifPasif.Trim()
                            };

                            yeniKullanici.Sifre = _passwordHasher.HashPassword(yeniKullanici, sifre);

                            foreach (var firma in selectedFirmalar)
                            {
                                yeniKullanici.Firmalar.Add(firma);
                            }

                            context.Kullanicilar.Add(yeniKullanici);
                            created++;
                        }
                        else
                        {
                            var normalizedAdi = kullaniciAdi.Trim();
                            var normalizedSoyadi = kullaniciSoyadi.Trim();
                            var normalizedGsm = (kullaniciGsm ?? string.Empty).Trim();
                            var normalizedRol = rol.Trim();
                            var normalizedAktifPasif = aktifPasif.Trim();

                            var currentAdi = (existingUser.KullaniciAdi ?? string.Empty).Trim();
                            var currentSoyadi = (existingUser.KullaniciSoyadi ?? string.Empty).Trim();
                            var currentMail = (existingUser.KullaniciMail ?? string.Empty).Trim();
                            var currentGsm = (existingUser.KullaniciGsm ?? string.Empty).Trim();
                            var currentRol = (existingUser.Rol ?? string.Empty).Trim();
                            var currentAktifPasif = (existingUser.KullaniciAktifPasif ?? string.Empty).Trim();

                            var currentFirmaIds = existingUser.Firmalar
                                .Select(f => f.FirmaId)
                                .OrderBy(x => x)
                                .ToList();

                            var newFirmaIds = selectedFirmalar
                                .Select(f => f.FirmaId)
                                .OrderBy(x => x)
                                .ToList();

                            var basicFieldsChanged =
                                currentAdi != normalizedAdi ||
                                currentSoyadi != normalizedSoyadi ||
                                currentMail != normalizedMail ||
                                currentGsm != normalizedGsm ||
                                currentRol != normalizedRol ||
                                currentAktifPasif != normalizedAktifPasif;

                            var firmaChanged =
                                existingUser.FirmaId != firmaIds[0] ||
                                !currentFirmaIds.SequenceEqual(newFirmaIds);

                            var passwordChanged = !string.IsNullOrWhiteSpace(sifre);

                            var hasChanges = basicFieldsChanged || firmaChanged || passwordChanged;

                            if (hasChanges)
                            {
                                existingUser.FirmaId = firmaIds[0];
                                existingUser.KullaniciAdi = normalizedAdi;
                                existingUser.KullaniciSoyadi = normalizedSoyadi;
                                existingUser.KullaniciMail = normalizedMail;
                                existingUser.KullaniciGsm = normalizedGsm;
                                existingUser.Rol = normalizedRol;
                                existingUser.KullaniciAktifPasif = normalizedAktifPasif;

                                if (passwordChanged)
                                {
                                    existingUser.Sifre = _passwordHasher.HashPassword(existingUser, sifre);
                                }

                                if (firmaChanged)
                                {
                                    existingUser.Firmalar.Clear();

                                    foreach (var firma in selectedFirmalar)
                                    {
                                        existingUser.Firmalar.Add(firma);
                                    }
                                }

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
                    await _logService.AddAsync(
                        "Uyarı",
                        "Kullanıcı",
                        $"Excel import tamamlandı ancak hatalar var. Dosya: {fileName}, Hata sayısı: {errors.Count}",
                        GetUserEmail()
                    );
                }

                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Bilgi",
                    "Kullanıcı",
                    $"Excel import tamamlandı. Dosya: {fileName}, Eklenen kullanıcı sayısı: {created}, Güncellenen kullanıcı sayısı: {updated}",
                    GetUserEmail()
                );

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

                var message = $"İşlem sırasında hata oluştu: {detail}";
                errors.Add(message);

                await _logService.AddAsync(
                    "Hata",
                    "Kullanıcı",
                    message,
                    GetUserEmail()
                );

                return (0, 0, errors);
            }
        }
    }
}