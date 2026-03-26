using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EMutabakat.Services
{
    public class MutabakatClientService : IMutabakatClientService
    {
        private readonly IMutabakatService _mutabakatService;
        private readonly IWebHostEnvironment _env;

        public MutabakatClientService(IMutabakatService _mutabakatService, IWebHostEnvironment env)
        {
            this._mutabakatService = _mutabakatService;
            _env = env;
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

            if (fileStream == null || string.IsNullOrWhiteSpace(originalFileName))
                return false;

            var ext = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;
            if (ext != ".xls" && ext != ".xlsx" && ext != ".pdf")
                return false;

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsRoot = Path.Combine(webRoot, "uploads", "mutabakat", token);
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(originalFileName)}";
            var filePath = Path.Combine(uploadsRoot, safeFileName);

            await using (var fs = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fs);
            }

            savedRelativePath = $"/uploads/mutabakat/{token}/{safeFileName}";

            return await _mutabakatService.RejectAsync(
                token,
                mail ?? string.Empty,
                adSoyad ?? string.Empty,
                gsm ?? string.Empty,
                savedRelativePath);
        }
    }
}