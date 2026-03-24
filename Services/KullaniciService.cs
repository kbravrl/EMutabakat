using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class KullaniciService : IKullaniciService
    {
        private readonly AppDbContext _context;

        public KullaniciService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Kullanici>> GetAllAsync()
        {
            return await _context.Kullanicilar
                .Include(x => x.Firma)
                .OrderBy(x => x.KullaniciAdi)
                .ThenBy(x => x.KullaniciSoyadi)
                .ToListAsync();
        }

        public async Task<Kullanici?> GetByIdAsync(int id)
        {
            return await _context.Kullanicilar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.KullaniciId == id);
        }

        public async Task<Kullanici> AddAsync(Kullanici kullanici)
        {
            _context.Kullanicilar.Add(kullanici);
            await _context.SaveChangesAsync();
            return kullanici;
        }

        public async Task<Kullanici?> UpdateAsync(Kullanici kullanici)
        {
            var existingKullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == kullanici.KullaniciId);

            if (existingKullanici == null)
                return null;

            existingKullanici.FirmaId = kullanici.FirmaId;
            existingKullanici.KullaniciAdi = kullanici.KullaniciAdi;
            existingKullanici.KullaniciSoyadi = kullanici.KullaniciSoyadi;
            existingKullanici.KullaniciMail = kullanici.KullaniciMail;
            existingKullanici.KullaniciGsm = kullanici.KullaniciGsm;
            existingKullanici.KullaniciAktifPasif = kullanici.KullaniciAktifPasif;

            await _context.SaveChangesAsync();
            return existingKullanici;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == id);

            if (kullanici == null)
                return false;

            _context.Kullanicilar.Remove(kullanici);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}