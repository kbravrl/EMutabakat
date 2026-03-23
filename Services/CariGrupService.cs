using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class CariGrupService : ICariGrupService
    {
        private readonly AppDbContext _db;

        public CariGrupService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CariGrup>> GetAllAsync()
        {
            return await _db.CariGruplar
                .Include(x => x.Firma)
                .OrderBy(x => x.CariGrupAdi)
                .ToListAsync();
        }

        public async Task<CariGrup?> GetByIdAsync(int id)
        {
            return await _db.CariGruplar
                .Include(x => x.Firma)
                .FirstOrDefaultAsync(x => x.CariGrupId == id);
        }

        public async Task<CariGrup> AddAsync(CariGrup cariGrup)
        {
            _db.CariGruplar.Add(cariGrup);
            await _db.SaveChangesAsync();
            return cariGrup;
        }

        public async Task<CariGrup?> UpdateAsync(CariGrup cariGrup)
        {
            var existingCariGrup = await _db.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == cariGrup.CariGrupId);

            if (existingCariGrup == null)
                return null;

            existingCariGrup.FirmaId = cariGrup.FirmaId;
            existingCariGrup.CariGrupAdi = cariGrup.CariGrupAdi;

            await _db.SaveChangesAsync();
            return existingCariGrup;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cariGrup = await _db.CariGruplar
                .FirstOrDefaultAsync(x => x.CariGrupId == id);

            if (cariGrup == null)
                return false;

            _db.CariGruplar.Remove(cariGrup);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}