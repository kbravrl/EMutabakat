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

        public MutabakatService(
            AppDbContext db,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _db = db;
            _emailService = emailService;
            _configuration = configuration;
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

            // Mail gönderildiyse kritik alanları değiştirmeyelim
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
                .Include(x => x.Firma)
                .Include(x => x.Cari)
                .FirstOrDefaultAsync(x => x.MutabakatId == mutabakatId);

            if (mutabakat == null || mutabakat.Firma == null || mutabakat.Cari == null)
                return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var approveUrl = $"{baseUrl}/mutabakat/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/mutabakat/response/{mutabakat.MutabakatToken}?durum=reject";

            var result = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                mutabakat.Firma,
                mutabakat.Cari,
                approveUrl,
                rejectUrl,
                false);

            if (!result)
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 1; // Normal

            if (mutabakat.MutabakatDurum == 0)
            {
                mutabakat.MutabakatDurum = 3; // Cevaplanmayan
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

            if (mutabakat == null || mutabakat.Firma == null || mutabakat.Cari == null)
                return false;

            if (string.IsNullOrWhiteSpace(mutabakat.MutabakatToken))
            {
                mutabakat.MutabakatToken = Guid.NewGuid().ToString("N");
            }

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var approveUrl = $"{baseUrl}/mutabakat/response/{mutabakat.MutabakatToken}?durum=approve";
            var rejectUrl = $"{baseUrl}/mutabakat/response/{mutabakat.MutabakatToken}?durum=reject";

            var result = await _emailService.SendMutabakatMailAsync(
                mutabakat,
                mutabakat.Firma,
                mutabakat.Cari,
                approveUrl,
                rejectUrl,
                true);

            if (!result)
                return false;

            mutabakat.MutabakatGonderimTarihSaat = DateTime.UtcNow;
            mutabakat.MutabakatGonderimDurumu = 2; // Hatırlatma

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

            mutabakat.MutabakatDurum = 1; // Mutabık
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

            mutabakat.MutabakatDurum = 2; // Değil
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