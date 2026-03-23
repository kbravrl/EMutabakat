using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IMutabakatService
    {
        Task<List<Mutabakat>> GetAllAsync();
        Task<Mutabakat?> GetByIdAsync(int id);
        Task<Mutabakat> AddAsync(Mutabakat mutabakat);
        Task<Mutabakat?> UpdateAsync(Mutabakat mutabakat);
        Task<bool> DeleteAsync(int id);
        Task<bool> SendMailAsync(int mutabakatId);
        Task<bool> SendReminderAsync(int mutabakatId);
        Task<Mutabakat?> GetByTokenAsync(string token);
        Task<bool> ApproveAsync(string token, string mail, string adSoyad, string gsm);
        Task<bool> RejectAsync(string token, string mail, string adSoyad, string gsm, string? filePath);
    }
}