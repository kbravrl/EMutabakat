using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ICariService
    {
        Task<List<Cari>> GetAllAsync();
        Task<Cari?> GetByIdAsync(int id);
        Task<Cari> AddAsync(Cari cari);
        Task<Cari?> UpdateAsync(Cari cari);
        Task<bool> DeleteAsync(int id);
        Task<(int created, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName);
    }
}