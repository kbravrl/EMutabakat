using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class CariService : ICariService
    {
        private readonly AppDbContext _context;

        public CariService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cari>> GetAllAsync()
        {
            return await _context.Cariler
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .OrderBy(x => x.CariAdi)
                .ToListAsync();
        }

        public async Task<Cari?> GetByIdAsync(int id)
        {
            return await _context.Cariler
                .Include(x => x.Firma)
                .Include(x => x.CariGrup)
                .FirstOrDefaultAsync(x => x.CariId == id);
        }

        public async Task<Cari> AddAsync(Cari cari)
        {
            _context.Cariler.Add(cari);
            await _context.SaveChangesAsync();
            return cari;
        }

        public async Task<Cari?> UpdateAsync(Cari cari)
        {
            var existingCari = await _context.Cariler
                .FirstOrDefaultAsync(x => x.CariId == cari.CariId);

            if (existingCari == null)
                return null;

            existingCari.FirmaId = cari.FirmaId;
            existingCari.CariAdi = cari.CariAdi;
            existingCari.CariUnvan = cari.CariUnvan;
            existingCari.CariAdres = cari.CariAdres;
            existingCari.CariIlce = cari.CariIlce;
            existingCari.CariIl = cari.CariIl;
            existingCari.CariVergiDairesi = cari.CariVergiDairesi;
            existingCari.CariVergiNumarasi = cari.CariVergiNumarasi;
            existingCari.CariWebAdresi = cari.CariWebAdresi;
            existingCari.CariYetkiliAdiSoyadi = cari.CariYetkiliAdiSoyadi;
            existingCari.CariYetkiliTelefon = cari.CariYetkiliTelefon;
            existingCari.CariYetkiliGsm = cari.CariYetkiliGsm;
            existingCari.CariYetkiliMail = cari.CariYetkiliMail;
            existingCari.CariGrupId = cari.CariGrupId;
            existingCari.CariDovizKodu = cari.CariDovizKodu;
            existingCari.CariAktifPasif = cari.CariAktifPasif;

            await _context.SaveChangesAsync();
            return existingCari;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cari = await _context.Cariler.FirstOrDefaultAsync(x => x.CariId == id);

            if (cari == null)
                return false;

            _context.Cariler.Remove(cari);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}