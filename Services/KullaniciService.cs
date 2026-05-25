using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
            return (kullanici.FirmaIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        public async Task<List<Kullanici>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var query = context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firmalar)
                .Include(x => x.Yetkileri)
                .OrderBy(x => x.KullaniciAdi)
                .ThenBy(x => x.KullaniciSoyadi)
                .AsQueryable();

            if (allowedFirmaIds != null)
            {
                if (allowedFirmaIds.Count == 0)
                    return new List<Kullanici>();

                query = query.Where(k => k.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)));
            }

            return await query.ToListAsync();
        }

        private async Task<List<int>?> GetAllowedFirmaIdsAsync(AppDbContext context)
        {
            var mail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(mail))
                return new List<int>();

            var kullanici = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            if (kullanici == null)
                return new List<int>();

            if (kullanici.IsSeedUser)
                return null;

            return kullanici.Firmalar
                .Select(f => f.FirmaId)
                .Distinct()
                .Where(id => id > 0)
                .ToList();
        }

        public async Task<Kullanici?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            var query = context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firmalar)
                .Include(x => x.Yetkileri)
                .Where(x => x.KullaniciId == id)
                .AsQueryable();

            if (allowedFirmaIds != null)
            {
                if (allowedFirmaIds.Count == 0)
                    return null;

                query = query.Where(x => x.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)));
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Kullanici?> GetByMailAsync(string mail)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedMail = mail?.Trim().ToLower();

            return await context.Kullanicilar
                .AsNoTracking()
                .Include(x => x.Firmalar)
                .Include(x => x.Yetkileri)
                .FirstOrDefaultAsync(x => x.KullaniciMail == normalizedMail);
        }

        public async Task<bool> IsCurrentUserSeedAsync()
        {
            var mail = GetUserEmail();
            if (string.IsNullOrWhiteSpace(mail))
                return false;

            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Kullanicilar
                .AsNoTracking()
                .AnyAsync(k => k.KullaniciMail == mail && k.IsSeedUser);
        }

        public async Task<KullaniciYetki?> GetCurrentUserYetkiAsync()
        {
            var mail = GetUserEmail();
            if (string.IsNullOrWhiteSpace(mail))
                return null;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var kullanici = await context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Yetkileri)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            return kullanici?.Yetkileri;
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
                $"Yeni kullanıcı kaydı oluşturuldu | Mail: {kullanici.KullaniciMail}",
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
                    $"Kullanıcı girişi başarılı | Mail: {normalizedMail}",
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

            if (string.IsNullOrWhiteSpace(kullanici.Sifre))
                throw new Exception("Şifre zorunludur.");

            if (kullanici.Sifre.Trim().Length < 6)
                throw new Exception("Şifre en az 6 karakter olmalıdır.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                throw new Exception("Aktif/Pasif bilgisi zorunludur.");

            kullanici.KullaniciAdi = kullanici.KullaniciAdi.Trim();
            kullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi.Trim();
            kullanici.KullaniciMail = kullanici.KullaniciMail.Trim().ToLower();
            kullanici.KullaniciGsm = NormalizeGsm(kullanici.KullaniciGsm);
            kullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif.Trim();

            var mailExists = await context.Kullanicilar.AnyAsync(x => x.KullaniciMail == kullanici.KullaniciMail);
            if (mailExists)
                throw new Exception("Bu mail adresi ile kayıtlı kullanıcı zaten var.");

            var firmalar = await context.Firmalar
                .Where(f => firmaIds.Contains(f.FirmaId))
                .ToListAsync();

            if (firmalar.Count != firmaIds.Count)
                throw new Exception("Seçilen firmalardan biri veya birkaçı bulunamadı.");

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, kullanici.Sifre.Trim());

            kullanici.Yetkileri ??= new KullaniciYetki();

            foreach (var firma in firmalar)
            {
                kullanici.Firmalar.Add(firma);
            }

            context.Kullanicilar.Add(kullanici);
            await context.SaveChangesAsync();

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Yeni kullanıcı eklendi | Id: {kullanici.KullaniciId}, Ad: {kullanici.KullaniciAdi} {kullanici.KullaniciSoyadi}, Mail: {kullanici.KullaniciMail}",
                GetUserEmail()
            );

            return kullanici;
        }

        public async Task<Kullanici?> UpdateAsync(Kullanici kullanici)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existingKullanici = await context.Kullanicilar
                .Include(x => x.Firmalar)
                .Include(x => x.Yetkileri)
                .FirstOrDefaultAsync(x => x.KullaniciId == kullanici.KullaniciId);

            if (existingKullanici == null)
                return null;

            if (existingKullanici.IsSeedUser)
            {
                var currentUserMail = GetUserEmail();
                var isSelf = !string.IsNullOrWhiteSpace(currentUserMail) &&
                            !string.IsNullOrWhiteSpace(existingKullanici.KullaniciMail) &&
                            currentUserMail.Equals(existingKullanici.KullaniciMail, StringComparison.OrdinalIgnoreCase);

                if (!isSelf)
                {
                    throw new Exception("Sistem kullanıcısı sadece kendisi tarafından düzenlenebilir.");
                }
            }

            var firmaIds = NormalizeFirmaIds(kullanici);
            if (firmaIds.Count == 0)
                throw new Exception("En az bir firma seçimi zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAdi))
                throw new Exception("Ad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciSoyadi))
                throw new Exception("Soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciMail))
                throw new Exception("Mail zorunludur.");

            if (string.IsNullOrWhiteSpace(kullanici.KullaniciAktifPasif))
                throw new Exception("Aktif/Pasif bilgisi zorunludur.");

            var desiredFirmalar = await context.Firmalar
                .Where(f => firmaIds.Contains(f.FirmaId))
                .ToListAsync();

            if (desiredFirmalar.Count != firmaIds.Count)
                throw new Exception("Seçilen firmalardan biri veya birkaçı bulunamadı.");

            var normalizedMail = kullanici.KullaniciMail?.Trim().ToLower();

            var mailExists = await context.Kullanicilar.AnyAsync(x =>
                x.KullaniciId != kullanici.KullaniciId &&
                x.KullaniciMail == normalizedMail);

            if (mailExists)
                throw new Exception("Bu mail adresi ile kayıtlı başka bir kullanıcı zaten var.");

            var snapshot = CreateSnapshot(existingKullanici, false);

            existingKullanici.KullaniciAdi = kullanici.KullaniciAdi.Trim();
            existingKullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi.Trim();
            existingKullanici.KullaniciMail = normalizedMail;
            existingKullanici.KullaniciGsm = NormalizeGsm(kullanici.KullaniciGsm);
            existingKullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif.Trim();

            var sifreDegisti = false;
            if (!string.IsNullOrWhiteSpace(kullanici.Sifre))
            {
                if (kullanici.Sifre.Trim().Length < 6)
                    throw new Exception("Şifre en az 6 karakter olmalıdır.");

                existingKullanici.Sifre = _passwordHasher.HashPassword(existingKullanici, kullanici.Sifre.Trim());
                sifreDegisti = true;
            }

            if (kullanici.Yetkileri != null)
            {
                if (existingKullanici.Yetkileri == null)
                {
                    existingKullanici.Yetkileri = new KullaniciYetki();
                }

                existingKullanici.Yetkileri.Cariler = kullanici.Yetkileri.Cariler;
                existingKullanici.Yetkileri.CariGruplar = kullanici.Yetkileri.CariGruplar;
                existingKullanici.Yetkileri.DovizKodlari = kullanici.Yetkileri.DovizKodlari;
                existingKullanici.Yetkileri.Mutabakatlar = kullanici.Yetkileri.Mutabakatlar;
                existingKullanici.Yetkileri.Firmalar = kullanici.Yetkileri.Firmalar;
                existingKullanici.Yetkileri.Kullanicilar = kullanici.Yetkileri.Kullanicilar;
                existingKullanici.Yetkileri.MutabakatMailYetki = kullanici.Yetkileri.MutabakatMailYetki;
                existingKullanici.Yetkileri.MutabakatSilYetki = kullanici.Yetkileri.MutabakatSilYetki;
                existingKullanici.Yetkileri.AylikBilgilerYetki = kullanici.Yetkileri.AylikBilgilerYetki;
                existingKullanici.Yetkileri.ImportYetki = kullanici.Yetkileri.ImportYetki;
                existingKullanici.Yetkileri.ExportYetki = kullanici.Yetkileri.ExportYetki;
                existingKullanici.Yetkileri.LogYetki = kullanici.Yetkileri.LogYetki;
            }

            existingKullanici.Firmalar.Clear();

            foreach (var firma in desiredFirmalar)
            {
                existingKullanici.Firmalar.Add(firma);
            }

            var newSnapshot = CreateSnapshot(existingKullanici, sifreDegisti);

            await context.SaveChangesAsync();

            await _logService.AddChangeAsync(
                "Kullanıcı",
                $"Id: {kullanici.KullaniciId}, Mail: {existingKullanici.KullaniciMail}",
                snapshot,
                newSnapshot,
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

            // Seed kullanıcı koruması
            if (kullanici.IsSeedUser)
            {
                throw new Exception("Sistem kullanıcısı silinemez.");
            }

            try
            {
                context.Kullanicilar.Remove(kullanici);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Kullanıcı",
                    $"Kullanıcı silindi | Id: {id}, Ad: {kullanici.KullaniciAdi} {kullanici.KullaniciSoyadi}, Mail: {kullanici.KullaniciMail}",
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
                    "Kullanıcı",
                    $"Kullanıcı silinemedi | Id: {id}",
                    GetUserEmail(),
                    detail
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

        public string? GetCurrentUserEmail()
        {
            return GetUserEmail();
        }

        private static string? GetStringCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            return cell?.ToString();
        }

        private static string? NormalizeGsm(string? gsm)
        {
            if (string.IsNullOrWhiteSpace(gsm))
                return null;

            gsm = gsm.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "");

            if (gsm.StartsWith("+90"))
                gsm = "0" + gsm.Substring(3);

            else if (gsm.StartsWith("90") && gsm.Length == 12)
                gsm = "0" + gsm.Substring(2);

            else if (gsm.StartsWith("5") && gsm.Length == 10)
                gsm = "0" + gsm;

            return gsm;
        }

        private static object CreateSnapshot(Kullanici kullanici, bool sifreDegisti)
        {
            return new
            {
                kullanici.KullaniciId,
                kullanici.KullaniciAdi,
                kullanici.KullaniciSoyadi,
                KullaniciMail = kullanici.KullaniciMail ?? string.Empty,
                KullaniciGsm = kullanici.KullaniciGsm ?? string.Empty,
                kullanici.KullaniciAktifPasif,
                SifreDegisti = sifreDegisti,
                Firmalar = kullanici.Firmalar
                    .Select(f => f.FirmaId)
                    .OrderBy(id => id)
                    .ToList(),
                Yetkiler = new
                {
                    Cariler = kullanici.Yetkileri?.Cariler ?? YetkiSeviyesi.Giris,
                    CariGruplar = kullanici.Yetkileri?.CariGruplar ?? YetkiSeviyesi.Giris,
                    DovizKodlari = kullanici.Yetkileri?.DovizKodlari ?? YetkiSeviyesi.Giris,
                    Mutabakatlar = kullanici.Yetkileri?.Mutabakatlar ?? YetkiSeviyesi.Giris,
                    Firmalar = kullanici.Yetkileri?.Firmalar ?? YetkiSeviyesi.Giris,
                    Kullanicilar = kullanici.Yetkileri?.Kullanicilar ?? YetkiSeviyesi.Giris,
                    MutabakatMailYetki = kullanici.Yetkileri?.MutabakatMailYetki ?? false,
                    ImportYetki = kullanici.Yetkileri?.ImportYetki ?? false,
                    ExportYetki = kullanici.Yetkileri?.ExportYetki ?? false,
                    LogYetki = kullanici.Yetkileri?.LogYetki ?? false
                }
            };
        }

        public async Task<byte[]> ExportToExcelAsync(List<Kullanici> kullanicilar)
        {
            var orderedKullanicilar = kullanicilar
                .OrderBy(x => x.KullaniciId)
                .ToList();

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Kullanıcı Excel export başladı. Kayıt sayısı: {orderedKullanicilar.Count}",
                GetUserEmail()
            );

            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Kullanicilar");

            var headers = new[]
            {
               "KullaniciId",
               "KullaniciAdi",
               "KullaniciSoyadi",
               "KullaniciMail",
               "KullaniciGsm",
               "KullaniciAktifPasif",
               "Sifre"
            };

            var headerRow = sheet.CreateRow(0);

            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }

            for (int i = 0; i < orderedKullanicilar.Count; i++)
            {
                var kullanici = orderedKullanicilar[i];
                var row = sheet.CreateRow(i + 1);

                row.CreateCell(0).SetCellValue(kullanici.KullaniciId);
                row.CreateCell(1).SetCellValue(kullanici.KullaniciAdi ?? "");
                row.CreateCell(2).SetCellValue(kullanici.KullaniciSoyadi ?? "");
                row.CreateCell(3).SetCellValue(kullanici.KullaniciMail ?? "");
                row.CreateCell(4).SetCellValue(kullanici.KullaniciGsm ?? "");
                row.CreateCell(5).SetCellValue(kullanici.KullaniciAktifPasif);
                row.CreateCell(6).SetCellValue("");
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            await using var ms = new MemoryStream();
            workbook.Write(ms, true);

            await _logService.AddAsync(
                "Bilgi",
                "Kullanıcı",
                $"Kullanıcı Excel export tamamlandı. Kayıt sayısı: {orderedKullanicilar.Count}",
                GetUserEmail()
            );

            return ms.ToArray();
        }

        public async Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, List<int> firmaIds)
        {
            var errors = new List<string>();
            var created = 0;
            var updated = 0;

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

                        await _logService.AddImportResultAsync(
                            "Kullanıcı",
                            $"Excel import başarısız. Dosya: {fileName}",
                            errors,
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
                        var kullaniciMail = GetStringCell(row, headerMap["KullaniciMail"])?.Trim() ?? string.Empty;

                        var aktifPasif = headerMap.ContainsKey("KullaniciAktifPasif")
                            ? (GetStringCell(row, headerMap["KullaniciAktifPasif"])?.Trim() ?? "1")
                            : "1";

                        var kullaniciAdi = GetStringCell(row, headerMap["KullaniciAdi"])?.Trim() ?? string.Empty;
                        var kullaniciSoyadi = GetStringCell(row, headerMap["KullaniciSoyadi"])?.Trim() ?? string.Empty;
                        var kullaniciGsm = headerMap.ContainsKey("KullaniciGsm")
                            ? GetStringCell(row, headerMap["KullaniciGsm"])?.Trim() ?? string.Empty
                            : string.Empty;
                        kullaniciGsm = NormalizeGsm(kullaniciGsm) ?? string.Empty;
                        var sifre = headerMap.ContainsKey("Sifre")
                            ? GetStringCell(row, headerMap["Sifre"])?.Trim() ?? string.Empty
                            : string.Empty;

                        if (string.IsNullOrWhiteSpace(kullaniciAdi) ||
                            string.IsNullOrWhiteSpace(kullaniciSoyadi) ||
                            string.IsNullOrWhiteSpace(kullaniciMail))
                        {
                            var message = $"Satır {r + 1}: Zorunlu alanlar boş olamaz (KullaniciAdi, KullaniciSoyadi, KullaniciMail).";
                            errors.Add(message);
                            continue;
                        }

                        if (aktifPasif != "0" && aktifPasif != "1")
                        {
                            var message = $"Satır {r + 1}: KullaniciAktifPasif değeri yalnızca 0 veya 1 olabilir.";
                            errors.Add(message);
                            continue;
                        }

                        var normalizedMail = kullaniciMail.Trim().ToLower();

                        var existingUser = await context.Kullanicilar
                            .Include(x => x.Firmalar)
                            .FirstOrDefaultAsync(x => x.KullaniciMail == normalizedMail);

                        if (existingUser == null)
                        {
                            if (string.IsNullOrWhiteSpace(sifre))
                            {
                                var message = $"Satır {r + 1}: Yeni kullanıcı için şifre zorunludur.";
                                errors.Add(message);
                                continue;
                            }

                            var yeniKullanici = new Kullanici
                            {
                                FirmaIds = firmaIds.ToList(),
                                KullaniciAdi = kullaniciAdi.Trim(),
                                KullaniciSoyadi = kullaniciSoyadi.Trim(),
                                KullaniciMail = normalizedMail,
                                KullaniciGsm = kullaniciGsm.Trim(),
                                Sifre = string.Empty,
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
                            var normalizedAktifPasif = aktifPasif.Trim();

                            var currentAdi = (existingUser.KullaniciAdi ?? string.Empty).Trim();
                            var currentSoyadi = (existingUser.KullaniciSoyadi ?? string.Empty).Trim();
                            var currentMail = (existingUser.KullaniciMail ?? string.Empty).Trim();
                            var currentGsm = (existingUser.KullaniciGsm ?? string.Empty).Trim();
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
                                currentAktifPasif != normalizedAktifPasif;

                            var firmaChanged = !currentFirmaIds.SequenceEqual(newFirmaIds);

                            var passwordChanged = !string.IsNullOrWhiteSpace(sifre);

                            var hasChanges = basicFieldsChanged || firmaChanged || passwordChanged;

                            if (hasChanges)
                            {
                                existingUser.KullaniciAdi = normalizedAdi;
                                existingUser.KullaniciSoyadi = normalizedSoyadi;
                                existingUser.KullaniciMail = normalizedMail;
                                existingUser.KullaniciGsm = normalizedGsm;
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
                    }
                }

                if (errors.Count > 0)
                {
                    await _logService.AddImportResultAsync(
                        "Kullanıcı",
                        $"Excel import tamamlandı ancak hatalar var. Dosya: {fileName}, Hata sayısı: {errors.Count}",
                        errors,
                        GetUserEmail()
                    );
                }

                await context.SaveChangesAsync();

                await _logService.AddImportResultAsync(
                    "Kullanıcı",
                    $"Excel import tamamlandı. Dosya: {fileName}, Eklenen: {created}, Güncellenen: {updated}",
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

                var message = $"İşlem sırasında hata oluştu: {detail}";
                errors.Add(message);

                await _logService.AddImportResultAsync(
                    "Kullanıcı",
                    $"Excel import genel hata. Dosya: {fileName}",
                    errors,
                    GetUserEmail()
                );

                return (0, 0, errors);
            }
        }
    }
}