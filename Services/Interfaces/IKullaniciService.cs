using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IKullaniciService
    {
        Task<List<Kullanici>> GetAllAsync();
        Task<Kullanici?> GetByIdAsync(string id);
        Task<Kullanici> AddAsync(Kullanici kullanici);
        Task<Kullanici?> UpdateAsync(Kullanici kullanici);
        Task<bool> DeleteAsync(string id);
        Task<Kullanici?> GetByMailAsync(string mail);
        Task<Kullanici?> RegisterAsync(Kullanici kullanici);
        Task<Kullanici?> LoginAsync(string mail, string sifre);
        Task<string> GenerateNextKullaniciIdAsync();
        Task<(int created, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, List<int> firmaIds);
    }
}