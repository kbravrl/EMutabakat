using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMutabakat.Services
{
    public class FirmaService : IFirmaService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FirmaService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Firma>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Firmalar
                .AsNoTracking()
                .OrderBy(x => x.FirmaAdi)
                .ToListAsync();
        }

        public async Task<Firma?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Firmalar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FirmaId == id);
        }

        public async Task<Firma> AddAsync(Firma firma)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                firma.FirmaMail = firma.FirmaMail?.Trim().ToLower();
                firma.FirmaSmtpUser = firma.FirmaSmtpUser?.Trim().ToLower();

                context.Firmalar.Add(firma);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Bilgi",
                    "Firma",
                    $"Yeni firma eklendi. Firma Id: {firma.FirmaId}, Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return firma;
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma ekleme hatası. Firma Adı: {firma.FirmaAdi}, Hata: {ex.Message}",
                    GetUserEmail()
                );

                throw;
            }
        }

        public async Task<Firma?> UpdateAsync(Firma firma)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            try
            {
                var existingFirma = await context.Firmalar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FirmaId == firma.FirmaId);

                if (existingFirma == null)
                    return null;

                firma.FirmaMail = firma.FirmaMail?.Trim().ToLower();
                firma.FirmaSmtpUser = firma.FirmaSmtpUser?.Trim().ToLower();

                var updated = await context.Firmalar
                    .Where(x => x.FirmaId == firma.FirmaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.FirmaAdi, firma.FirmaAdi)
                        .SetProperty(x => x.FirmaUnvan, firma.FirmaUnvan)
                        .SetProperty(x => x.FirmaAdres, firma.FirmaAdres)
                        .SetProperty(x => x.FirmaIlce, firma.FirmaIlce)
                        .SetProperty(x => x.FirmaIl, firma.FirmaIl)
                        .SetProperty(x => x.FirmaVergiDairesi, firma.FirmaVergiDairesi)
                        .SetProperty(x => x.FirmaVergiNumarasi, firma.FirmaVergiNumarasi)
                        .SetProperty(x => x.FirmaMersisNumarasi, firma.FirmaMersisNumarasi)
                        .SetProperty(x => x.FirmaWebAdresi, firma.FirmaWebAdresi)
                        .SetProperty(x => x.FirmaYetkiliAdiSoyadi, firma.FirmaYetkiliAdiSoyadi)
                        .SetProperty(x => x.FirmaMail, firma.FirmaMail) // 🔥 normalized
                        .SetProperty(x => x.FirmaTelefon, firma.FirmaTelefon)
                        .SetProperty(x => x.FirmaGsm, firma.FirmaGsm)
                        .SetProperty(x => x.FirmaSmtpHost, firma.FirmaSmtpHost)
                        .SetProperty(x => x.FirmaSmtpPort, firma.FirmaSmtpPort)
                        .SetProperty(x => x.FirmaSmtpUser, firma.FirmaSmtpUser) // 🔥 normalized
                        .SetProperty(x => x.FirmaSmtpPassword, firma.FirmaSmtpPassword)
                        .SetProperty(x => x.FirmaSmtpSecure, firma.FirmaSmtpSecure)
                        .SetProperty(x => x.FirmaAktifPasif, firma.FirmaAktifPasif));

                if (updated == 0)
                    return null;

                await _logService.AddAsync(
                    "Uyarı",
                    "Firma",
                    $"Firma güncellendi. Firma Id: {firma.FirmaId}, Firma Adı: {existingFirma.FirmaAdi} -> {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return await GetByIdAsync(firma.FirmaId);
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma güncelleme hatası. Firma Id: {firma.FirmaId}, Hata: {ex.Message}",
                    GetUserEmail()
                );

                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var firma = await context.Firmalar
                .FirstOrDefaultAsync(x => x.FirmaId == id);

            if (firma == null)
                return false;

            try
            {
                context.Firmalar.Remove(firma);
                await context.SaveChangesAsync();

                await _logService.AddAsync(
                    "Uyarı",
                    "Firma",
                    $"Firma silindi. Firma Id: {firma.FirmaId}, Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );

                return true;
            }
            catch (Exception ex)
            {
                await _logService.AddAsync(
                    "Hata",
                    "Firma",
                    $"Firma silinemedi: Firma Id: {firma.FirmaId} Firma Adı: {firma.FirmaAdi}",
                    GetUserEmail()
                );
                throw new Exception("Bu firma kaydı başka kayıtlarda kullanıldığı için silinemez.");
            }
        }

        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .Identity?
                .Name;
        }
    }
}