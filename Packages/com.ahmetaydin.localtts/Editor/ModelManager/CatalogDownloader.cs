using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Streams a catalog entry to disk with progress reporting and verifies its SHA-256
    /// against the pinned hash before the file is accepted.
    /// </summary>
    public static class CatalogDownloader
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        /// <summary>Downloads to <paramref name="destinationPath"/> (absolute). Throws on any failure.</summary>
        public static async Task DownloadAsync(
            KokoroCatalog.Entry entry, string destinationPath, Action<float> onProgress = null)
        {
            string tempPath = destinationPath + ".download";
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            try
            {
                using var response = await Client.GetAsync(
                    entry.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using (var body = await response.Content.ReadAsStreamAsync())
                await using (var file = File.Create(tempPath))
                {
                    var buffer = new byte[1 << 16];
                    long written = 0;
                    int read;
                    while ((read = await body.ReadAsync(buffer)) > 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, read));
                        written += read;
                        onProgress?.Invoke((float)((double)written / entry.SizeBytes));
                    }
                }

                VerifyOrThrow(entry, tempPath);
                File.Delete(destinationPath);
                File.Move(tempPath, destinationPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static void VerifyOrThrow(KokoroCatalog.Entry entry, string path)
        {
            var info = new FileInfo(path);
            if (info.Length != entry.SizeBytes)
            {
                throw new InvalidDataException(
                    $"{entry.Name}: size mismatch ({info.Length} vs expected {entry.SizeBytes}).");
            }

            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            string actual = BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", "").ToLowerInvariant();
            if (actual != entry.Sha256)
            {
                throw new InvalidDataException(
                    $"{entry.Name}: SHA-256 mismatch — download corrupt or upstream changed.\n" +
                    $"expected {entry.Sha256}\nactual   {actual}");
            }
        }

        /// <summary>True if the file exists with the exact catalog size (cheap presence check).</summary>
        public static bool IsPresent(KokoroCatalog.Entry entry, string path)
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length == entry.SizeBytes;
        }
    }
}
