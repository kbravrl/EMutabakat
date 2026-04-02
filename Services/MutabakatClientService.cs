using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace EMutabakat.Services
{
    public class MutabakatClientService : IMutabakatClientService
    {
        private readonly IMutabakatService _mutabakatService;
        private readonly ISdService _sdService;

        public MutabakatClientService(IMutabakatService _mutabakatService, ISdService sdService)
        {
            this._mutabakatService = _mutabakatService;
            _sdService = sdService;
        }

        public Task<Mutabakat?> GetByTokenAsync(string token)
            => _mutabakatService.GetByTokenAsync(token);

        public async Task<bool> ApproveAsync(string token, string? mail = null, string? adSoyad = null, string? gsm = null)
        {
            if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(gsm))
            {
                var mutabakat = await _mutabakatService.GetByTokenAsync(token);

                if (mutabakat?.Cari != null)
                {
                    mail = string.IsNullOrWhiteSpace(mail) ? mutabakat.Cari.CariYetkiliMail : mail;
                    adSoyad = string.IsNullOrWhiteSpace(adSoyad) ? mutabakat.Cari.CariYetkiliAdiSoyadi : adSoyad;
                    gsm = string.IsNullOrWhiteSpace(gsm) ? mutabakat.Cari.CariYetkiliGsm : gsm;
                }
            }

            return await _mutabakatService.ApproveAsync(
                token,
                mail ?? string.Empty,
                adSoyad ?? string.Empty,
                gsm ?? string.Empty);
        }

        public async Task<bool> RejectAsync(
            string token,
            string? mail = null,
            string? adSoyad = null,
            string? gsm = null,
            Stream? fileStream = null,
            string? originalFileName = null)
        {
            string? savedRelativePath = null;
            var mutabakat = await _mutabakatService.GetByTokenAsync(token);

            if (mutabakat == null)
                return false;

            if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(gsm))
            {
                if (mutabakat?.Cari != null)
                {
                    mail = string.IsNullOrWhiteSpace(mail) ? mutabakat.Cari.CariYetkiliMail : mail;
                    adSoyad = string.IsNullOrWhiteSpace(adSoyad) ? mutabakat.Cari.CariYetkiliAdiSoyadi : adSoyad;
                    gsm = string.IsNullOrWhiteSpace(gsm) ? mutabakat.Cari.CariYetkiliGsm : gsm;
                }
            }

            if (fileStream == null || string.IsNullOrWhiteSpace(originalFileName))
                return false;

            savedRelativePath = await _sdService.SaveMutabakatResponseFileAsync(
                token,
                mutabakat.MutabakatDonemi,
                mutabakat.CariId,
                mutabakat.Firma?.FirmaAdi,
                fileStream,
                originalFileName);

            if (string.IsNullOrWhiteSpace(savedRelativePath))
                return false;

            var ok = await _mutabakatService.RejectAsync(
                token,
                mail ?? string.Empty,
                adSoyad ?? string.Empty,
                gsm ?? string.Empty,
                savedRelativePath);

            if (!ok)
            {
                await _sdService.DeleteMutabakatResponseFileAsync(savedRelativePath);
            }

            return ok;
        }
    }
}