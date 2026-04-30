using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IMutabakatService
    {
        Task<List<Mutabakat>> GetAllAsync();
        Task<Mutabakat?> GetByIdAsync(string id);
        Task<Mutabakat> AddAsync(Mutabakat mutabakat, string? cariMail = null);
        Task<Mutabakat?> UpdateAsync(Mutabakat mutabakat, string? cariMail = null);
        Task<bool> DeleteAsync(string id);
        Task<bool> SendMailAsync(string mutabakatId);
        Task<bool> SendReminderAsync(string mutabakatId);
        Task<(int successCount, int failCount, List<string> errors)> SendPendingMailsAsync();
        Task<(int successCount, int failCount, List<string> errors)> SendSelectedMailsAsync(List<string> mutabakatIds);
        Task<Mutabakat?> GetByTokenAsync(string token);
        Task<bool> ApproveAsync(string token, string mail, string adSoyad, string gsm);
        Task<bool> RejectAsync(string token, string mail, string adSoyad, string gsm, string? aciklama, string? filePath);
        Task<(int created, int mailsSent, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, bool sendMail);
        Task<byte[]> ExportToExcelAsync();
        Task<string> GenerateNextMutabakatIdAsync();
        Task<List<SilinenMutabakat>> GetAllDeletedAsync();
        Task<SilinenMutabakat?> GetDeletedByIdAsync(int id);
        Task<bool> DeleteDeletedAsync(int id);
    }
}