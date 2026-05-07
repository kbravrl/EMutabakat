using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IDovizKoduService
    {
        Task<List<DovizKodu>> GetAllAsync();
        Task<List<DovizKodu>> GetActiveAsync();
        Task<DovizKodu?> GetByTcmbAsync(string tcmb);
        Task<DovizKodu> AddAsync(DovizKodu dovizKodu);
        Task<DovizKodu?> UpdateAsync(DovizKodu dovizKodu, string originalTcmb);
        Task<bool> DeleteAsync(string tcmb);
    }
}
