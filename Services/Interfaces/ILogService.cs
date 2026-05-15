using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ILogService
    {
        Task<List<AppLog>> GetAllAsync();
        Task<List<AppLog>> GetRecentAsync(int count = 200);
        Task AddAsync(string level, string source, string message, string? userEmail = null, string? details = null);
        Task AddChangeAsync(string source, string entityId, object oldEntity, object newEntity, string? userEmail = null);
        Task AddImportResultAsync(string source, string message, List<string> errors, string? userEmail = null);
        Task DeleteAllAsync();
        Task DeleteLastDaysAsync(int dayCount);
        Task<byte[]> ExportToExcelAsync(List<AppLog> logs);
    }
}
