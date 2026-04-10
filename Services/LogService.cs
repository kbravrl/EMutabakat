using EMutabakat.Data;
using EMutabakat.Models;
using EMutabakat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMutabakat.Services
{
    public class LogService : ILogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public LogService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<AppLog>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.AppLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AppLog>> GetRecentAsync(int count = 200)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.AppLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
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