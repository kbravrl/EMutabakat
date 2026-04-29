using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ILogService
    {
        Task<List<AppLog>> GetAllAsync();
        Task<List<AppLog>> GetRecentAsync(int count = 200);
        Task AddAsync(string level, string source, string message, string? userEmail = null);
        Task DeleteAllAsync();
        Task DeleteLastDaysAsync(int dayCount);
    }
}