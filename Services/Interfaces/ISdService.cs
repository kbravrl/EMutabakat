using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EMutabakat.Services.Interfaces
{
    public interface ISdService
    {
        Task<string?> SaveMutabakatResponseFileAsync(
            string token,
            DateTime mutabakatDonemi,
            int cariId,
            string? firmaAdi,
            Stream fileStream,
            string originalFileName,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteMutabakatResponseFileAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
