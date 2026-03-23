using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendMutabakatMailAsync(
            Mutabakat mutabakat,
            Firma firma,
            Cari cari,
            string approveUrl,
            string rejectUrl,
            bool isReminder = false);
    }
}