using EMutabakat.Models;

namespace EMutabakat.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendMutabakatMailAsync(
            Mutabakat mutabakat,
            Kullanici kullanici,
            string approveUrl,
            string rejectUrl,
            bool isReminder = false);
    }
}