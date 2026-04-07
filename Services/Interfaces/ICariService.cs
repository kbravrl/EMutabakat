using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ICariService
    {
        Task<List<Cari>> GetAllAsync();
        Task<string> GenerateNextCariIdAsync();
        Task<Cari?> GetByIdAsync(string cariId, int firmaId);
        Task<Cari> AddAsync(Cari cari);
        Task<Cari?> UpdateAsync(Cari cari);
        Task<bool> DeleteAsync(string cariId, int firmaId);
        Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId);
    }
}