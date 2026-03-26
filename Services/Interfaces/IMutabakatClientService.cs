using System.IO;
using System.Threading.Tasks;
using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IMutabakatClientService
    {
        Task<Mutabakat?> GetByTokenAsync(string token);

        Task<bool> ApproveAsync(
            string token,
            string? mail = null,
            string? adSoyad = null,
            string? gsm = null);

        Task<bool> RejectAsync(
            string token,
            string? mail = null,
            string? adSoyad = null,
            string? gsm = null,
            Stream? fileStream = null,
            string? originalFileName = null);
    }
}