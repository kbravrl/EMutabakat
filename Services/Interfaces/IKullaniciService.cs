using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IKullaniciService
    {
        Task<List<Kullanici>> GetAllAsync();
        Task<Kullanici?> GetByIdAsync(int id);
        Task<Kullanici> AddAsync(Kullanici kullanici);
        Task<Kullanici?> UpdateAsync(Kullanici kullanici);
        Task<bool> DeleteAsync(int id);
    }
}