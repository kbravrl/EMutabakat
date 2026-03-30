using EMutabakat.Models;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMutabakat.Services.Interfaces
{
    public interface IFirmaService
    {
        Task<List<Firma>> GetAllAsync();
        Task<Firma?> GetByIdAsync(int id);
        Task<Firma> AddAsync(Firma firma);
        Task<Firma?> UpdateAsync(Firma firma);
        Task<bool> DeleteAsync(int id);
        Task<(int created, List<string> errors)> ImportFromExcelAsync(Stream stream, string fileName);
    }
}