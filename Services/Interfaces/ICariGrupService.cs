using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ICariGrupService
    {
        Task<List<CariGrup>> GetAllAsync();
        Task<CariGrup?> GetByIdAsync(int id);
        Task<CariGrup> AddAsync(CariGrup cariGrup);
        Task<CariGrup?> UpdateAsync(CariGrup cariGrup);
        Task<bool> DeleteAsync(int id);
    }
}