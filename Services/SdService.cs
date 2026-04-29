using EMutabakat.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EMutabakat.Services
{
    public class SdService : ISdService
    {
        private readonly string _storageRoot;
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        public SdService(IConfiguration configuration)
        {
            _storageRoot = configuration["Storage:RootPath"] ?? @"D:\EMutabakatRedDosyaları";
        }

        public async Task<string?> SaveMutabakatResponseFileAsync(
    string token,
    DateTime mutabakatDonemi,
    string cariId,
    string? cariVergiNo,
    Stream fileStream,
    string originalFileName,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                fileStream == null ||
                string.IsNullOrWhiteSpace(originalFileName) ||
                string.IsNullOrWhiteSpace(cariId))
                return null;

            if (fileStream.CanSeek && fileStream.Length > MaxFileSize)
                return null;

            var donemFolder = mutabakatDonemi.ToString("yyyy-MM");
            var cariVergiNoFolder = SanitizePathSegment(cariVergiNo);
            var cariIdFolder = SanitizePathSegment(cariId);

            var uploadsRoot = Path.Combine(_storageRoot, donemFolder, cariVergiNoFolder, cariIdFolder);
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = Path.GetFileName(originalFileName);
            var filePath = Path.Combine(uploadsRoot, safeFileName);

            await using var fs = File.Create(filePath);
            await fileStream.CopyToAsync(fs, cancellationToken);

            return $"/uploads/{donemFolder}/{cariVergiNoFolder}/{cariIdFolder}/{safeFileName}";
        }

        public Task<bool> DeleteMutabakatResponseFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.FromResult(false);

            var normalizedRelativePath = relativePath.Trim().TrimStart('~').TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            var relativeUnderUploads = normalizedRelativePath;
            if (relativeUnderUploads.StartsWith($"uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                relativeUnderUploads = relativeUnderUploads.Substring($"uploads{Path.DirectorySeparatorChar}".Length);
            }

            if (relativeUnderUploads.StartsWith($"mutabakat{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                relativeUnderUploads = relativeUnderUploads.Substring($"mutabakat{Path.DirectorySeparatorChar}".Length);
            }

            if (relativeUnderUploads.StartsWith($"EMutabkatRedDosyaları{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                relativeUnderUploads = relativeUnderUploads.Substring($"EMutabkatRedDosyaları{Path.DirectorySeparatorChar}".Length);
            }

            if (relativeUnderUploads.StartsWith($"EMutabakatRedDosyaları{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                relativeUnderUploads = relativeUnderUploads.Substring($"EMutabakatRedDosyaları{Path.DirectorySeparatorChar}".Length);
            }

            var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, relativeUnderUploads));
            var allowedRoot = Path.GetFullPath(_storageRoot);

            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(false);

            if (!File.Exists(fullPath))
                return Task.FromResult(false);

            File.Delete(fullPath);

            return Task.FromResult(true);
        }

        private static string SanitizePathSegment(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "Firma" : value.Trim();

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(text) ? "Firma" : text;
        }
    }
}