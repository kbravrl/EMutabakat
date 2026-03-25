using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

            _db.Kullanicilar.Remove(kullanici);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}