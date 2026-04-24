using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class LogService : ILogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(
            IDbContextFactory<AppDbContext> contextFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<AppLog>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds.Count == 0)
                return new List<AppLog>();

            var allowedUserEmails = await context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Firmalar)
                .Where(k => k.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)))
                .Select(k => k.KullaniciMail)
                .ToListAsync();

            return await context.AppLogs
                .AsNoTracking()
                .Where(x => x.UserEmail != null && allowedUserEmails.Contains(x.UserEmail))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AppLog>> GetRecentAsync(int count = 200)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var allowedFirmaIds = await GetAllowedFirmaIdsAsync(context);

            if (allowedFirmaIds.Count == 0)
                return new List<AppLog>();

            var allowedUserEmails = await context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Firmalar)
                .Where(k => k.Firmalar.Any(f => allowedFirmaIds.Contains(f.FirmaId)))
                .Select(k => k.KullaniciMail)
                .ToListAsync();

            return await context.AppLogs
                .AsNoTracking()
                .Where(x => x.UserEmail != null && allowedUserEmails.Contains(x.UserEmail))
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<int>> GetAllowedFirmaIdsAsync(AppDbContext context)
        {
            var mail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(mail))
                return new List<int>();

            var kullanici = await context.Kullanicilar
                .Include(k => k.Firmalar)
                .FirstOrDefaultAsync(k => k.KullaniciMail == mail);

            if (kullanici == null)
                return new List<int>();

            return kullanici.Firmalar
                .Select(f => f.FirmaId)
                .Distinct()
                .Where(id => id > 0)
                .ToList();
        }

        public async Task AddAsync(string level, string source, string message, string? userEmail = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var log = new AppLog
            {
                CreatedAt = DateTime.UtcNow,
                Level = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim(),
                Source = string.IsNullOrWhiteSpace(source) ? "System" : source.Trim(),
                Message = string.IsNullOrWhiteSpace(message) ? "Log message is empty." : message.Trim(),
                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail.Trim()
            };

            context.AppLogs.Add(log);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var logs = await context.AppLogs.ToListAsync();
            if (logs.Count == 0)
                return;

            context.AppLogs.RemoveRange(logs);
            await context.SaveChangesAsync();
        }
    }
}