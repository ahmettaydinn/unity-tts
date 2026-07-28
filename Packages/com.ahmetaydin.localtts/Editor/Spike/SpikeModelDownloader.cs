using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Spike-grade model fetcher: menu items that download the Kokoro ONNX model and one
    /// voice into <see cref="ModelPaths.ModelAssetFolder"/>. The real ModelDownloader
    /// (Phase 3) replaces this with checksums, progress UI, and quantization choices.
    /// </summary>
    public static class SpikeModelDownloader
    {
        private const string BaseUrl =
            "https://huggingface.co/onnx-community/Kokoro-82M-v1.0-ONNX/resolve/main";

        public const string ModelAssetPath = "Assets/LocalTTS/Models/kokoro-v1.0.onnx";
        public const string VoiceAssetPath = "Assets/LocalTTS/Models/af_heart.bin";

        [MenuItem("LocalTTS/Spike/Download Model + Voice (~330 MB)")]
        public static void DownloadAll()
        {
            Directory.CreateDirectory(ModelPaths.ModelAssetFolderAbsolute);
            Download($"{BaseUrl}/onnx/model.onnx", ModelAssetPath);
            Download($"{BaseUrl}/voices/af_heart.bin", VoiceAssetPath);
            AssetDatabase.Refresh();
            Debug.Log("LocalTTS spike model + voice downloaded and imported.");
        }

        private static void Download(string url, string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
            {
                Debug.Log($"LocalTTS: {assetPath} already present, skipping download.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("LocalTTS", $"Downloading {Path.GetFileName(url)}…", 0.5f);
                using var client = new HttpClient();
                byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(fullPath, data);
                Debug.Log($"LocalTTS: downloaded {assetPath} ({data.Length / (1024 * 1024)} MB).");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
