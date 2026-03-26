using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class MutabakatService : IMutabakatService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MutabakatService(
            AppDbContext db,
            IEmailService emailService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _emailService = emailService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Mutabakat>> GetAllAsync()
        {
            return await _db.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .OrderByDescending(x => x.MutabakatDonemi)
                .ThenByDescending(x => x.MutabakatId)
                .ToListAsync();
        }

        public async Task<Mutabakat?> GetByIdAsync(int id)
        {
            return await _db.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == id);
        }

        public async Task<Mutabakat> AddAsync(Mutabakat mutabakat)
        {
            mutabakat.MutabakatDonemi = DateTime.SpecifyKind(
                mutabakat.MutabakatDonemi,
                DateTimeKind.Utc);

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            if (mutabakat.MutabakatDurum == 0)
            {
                mutabakat.MutabakatDurum = 3;
            }

            if (mutabakat.MutabakatGonderimDurumu == 0)
            {
                mutabakat.MutabakatGonderimDurumu = 1;
            }

            _db.Mutabakatlar.Add(mutabakat);
            await _db.SaveChangesAsync();

            return mutabakat;
        }

        public async Task<Mutabakat?> UpdateAsync(Mutabakat mutabakat)
        {
            var existingMutabakat = await _db.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakat.MutabakatId);

            if (existingMutabakat == null)
                return null;
            var mailGonderildiMi = existingMutabakat.MutabakatGonderimTarihSaat.HasValue;

            if (!mailGonderildiMi)
            {
                existingMutabakat.FirmaId = mutabakat.FirmaId;
                existingMutabakat.CariId = mutabakat.CariId;
                existingMutabakat.MutabakatDonemi = DateTime.SpecifyKind(
                     mutabakat.MutabakatDonemi,
                     DateTimeKind.Utc);
                existingMutabakat.MutabakatTipi = mutabakat.MutabakatTipi;
                existingMutabakat.MutabakatDovizKodu = mutabakat.MutabakatDovizKodu;
                existingMutabakat.MutabakatBakiye = mutabakat.MutabakatBakiye;
                existingMutabakat.MutabakatBakiyeTipi = mutabakat.MutabakatBakiyeTipi;
            }

            existingMutabakat.MutabakatAciklama = mutabakat.MutabakatAciklama;

            await _db.SaveChangesAsync();
            return existingMutabakat;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var mutabakat = await _db.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatId == id);

            if (mutabakat == null)
                return false;

            _db.Mutabakatlar.Remove(mutabakat);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SendMailAsync(int mutabakatId)
        {
            var mutabakat = await _db.Mutabakatlar
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var email = user.Identity.Name;

            var kullanici = await _db.Kullanicilar
               .Include(x => x.Firma)
               .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null || kullanici.Firma == null)
                return false;

 
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7017";

            var approveUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=reject";

            var result = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                kullanici,
                approveUrl,
                rejectUrl,
                false);

            if (!result)
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 1;

            if (mutabakat.MutabakatDurum == 0)
            {
                mutabakat.MutabakatDurum = 3;
            }

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SendReminderAsync(int mutabakatId)
        {
            var mutabakat = await _db.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Cari == null)
                return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var email = user.Identity.Name;

            var kullanici = await _db.Kullanicilar
               .Include(x => x.Firma)
               .FirstOrDefaultAsync(x => x.KullaniciMail == email);

            if (kullanici == null || kullanici.Firma == null)
                return false;

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";

            var approveUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/reconciliations/response/{mutabakat.MutabakatToken}?durum=reject";

            var result = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                kullanici,
                approveUrl,
                rejectUrl,
                true);

            if (!result)
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 2;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<Mutabakat?> GetByTokenAsync(string token)
        {
            return await _db.Mutabakatlar
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);
        }

        public async Task<bool> ApproveAsync(string token, string mail, string adSoyad, string gsm)
        {
            var mutabakat = await _db.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null)
                return false;

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
                return false;

            mutabakat.MutabakatDurum = 1;
            mutabakat.MutabakatCevapTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatCevapMail = mail;
            mutabakat.MutabakatCevapAdSoyad = adSoyad;
            mutabakat.MutabakatCevapGsm = gsm;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(string token, string mail, string adSoyad, string gsm, string? filePath)
        {
            var mutabakat = await _db.Mutabakatlar
                .FirstOrDefaultAsync(x => x.MutabakatToken == token);

            if (mutabakat == null)
                return false;

            if (mutabakat.MutabakatDurum == 1 || mutabakat.MutabakatDurum == 2)
                return false;

            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            mutabakat.MutabakatDurum = 2;
            mutabakat.MutabakatCevapTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatCevapMail = mail;
            mutabakat.MutabakatCevapAdSoyad = adSoyad;
            mutabakat.MutabakatCevapGsm = gsm;
            mutabakat.MutabakatReceiveStoragePath = filePath;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}