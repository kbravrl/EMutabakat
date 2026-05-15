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
        Task<Kullanici?> GetByMailAsync(string mail);
        Task<Kullanici?> RegisterAsync(Kullanici kullanici);
        Task<Kullanici?> LoginAsync(string mail, string sifre);
        Task<bool> IsCurrentUserSeedAsync();
        Task<KullaniciYetki?> GetCurrentUserYetkiAsync();
        string? GetCurrentUserEmail();
        Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, List<int> firmaIds);
        Task<byte[]> ExportToExcelAsync(List<Kullanici> kullanicilar);
    }
}