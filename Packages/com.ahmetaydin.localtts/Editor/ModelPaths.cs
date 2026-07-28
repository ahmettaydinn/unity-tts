using System.IO;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Canonical locations for downloaded model assets. Model weights are never committed
    /// to git (see repo .gitignore) — the ModelDownloader (Phase 3) fetches them here.
    /// </summary>
    public static class ModelPaths
    {
        /// <summary>Hugging Face repo the ONNX model and voices are fetched from.</summary>
        public const string ModelRepo = "onnx-community/Kokoro-82M-v1.0-ONNX";

        /// <summary>Project-relative folder that holds imported model assets.</summary>
        public static string ModelAssetFolder => "Assets/LocalTTS/Models";

        /// <summary>Absolute path of the model asset folder.</summary>
        public static string ModelAssetFolderAbsolute =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "LocalTTS/Models"));
    }
}
