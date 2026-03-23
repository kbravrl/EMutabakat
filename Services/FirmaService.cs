using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class FirmaService : IFirmaService
    {
        private readonly AppDbContext _db;

        public FirmaService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Firma>> GetAllAsync()
        {
            return await _db.Firmalar
                .OrderBy(x => x.FirmaAdi)
                .ToListAsync();
        }

        public async Task<Firma?> GetByIdAsync(int id)
        {
            return await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);
        }

        public async Task<Firma> AddAsync(Firma firma)
        {
            _db.Firmalar.Add(firma);
            await _db.SaveChangesAsync();
            return firma;
        }

        public async Task<Firma?> UpdateAsync(Firma firma)
        {
            var existingFirma = await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == firma.FirmaId);

            if (existingFirma == null)
                return null;

            existingFirma.FirmaAdi = firma.FirmaAdi;
            existingFirma.FirmaUnvan = firma.FirmaUnvan;
            existingFirma.FirmaAdres = firma.FirmaAdres;
            existingFirma.FirmaIlce = firma.FirmaIlce;
            existingFirma.FirmaIl = firma.FirmaIl;
            existingFirma.FirmaVergiDairesi = firma.FirmaVergiDairesi;
            existingFirma.FirmaVergiNumarasi = firma.FirmaVergiNumarasi;
            existingFirma.FirmaMersisNumarasi = firma.FirmaMersisNumarasi;
            existingFirma.FirmaWebAdresi = firma.FirmaWebAdresi;
            existingFirma.FirmaYetkiliAdiSoyadi = firma.FirmaYetkiliAdiSoyadi;
            existingFirma.FirmaMail = firma.FirmaMail;
            existingFirma.FirmaTelefon = firma.FirmaTelefon;
            existingFirma.FirmaGsm = firma.FirmaGsm;
            existingFirma.FirmaSmtpHost = firma.FirmaSmtpHost;
            existingFirma.FirmaSmtpPort = firma.FirmaSmtpPort;
            existingFirma.FirmaSmtpUser = firma.FirmaSmtpUser;
            existingFirma.FirmaSmtpPassword = firma.FirmaSmtpPassword;
            existingFirma.FirmaSmtpSecure = firma.FirmaSmtpSecure;
            existingFirma.FirmaAktifPasif = firma.FirmaAktifPasif;

            await _db.SaveChangesAsync();
            return existingFirma;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var firma = await _db.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);

            if (firma == null)
                return false;

            _db.Firmalar.Remove(firma);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}