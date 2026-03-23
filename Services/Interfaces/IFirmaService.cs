using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IFirmaService
    {
        Task<List<Firma>> GetAllAsync();
        Task<Firma?> GetByIdAsync(int id);
        Task<Firma> AddAsync(Firma firma);
        Task<Firma?> UpdateAsync(Firma firma);
        Task<bool> DeleteAsync(int id);
    }
}