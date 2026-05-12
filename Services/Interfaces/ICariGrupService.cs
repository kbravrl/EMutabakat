using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface ICariGrupService
    {
        Task<List<CariGrup>> GetAllAsync();
        Task<CariGrup?> GetByIdAsync(string id, int firmaId);
        Task<string> GenerateNextCariGrupIdAsync();
        Task<CariGrup> AddAsync(CariGrup cariGrup);
        Task<CariGrup?> UpdateAsync(CariGrup cariGrup);
        Task<bool> DeleteAsync(string id, int firmaId);
        Task<(int created, int updated, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName, int firmaId);
        Task<byte[]> ExportToExcelAsync(List<CariGrup> cariGruplar);
    }
}