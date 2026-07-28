using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// One-stop setup UI: pick a model variant, download it (SHA-256 verified), and
    /// download voices — each voice becomes a ready-to-assign TTSVoice asset.
    /// </summary>
    public sealed class ModelManagerWindow : EditorWindow
    {
        public const string VoiceFolder = "Assets/LocalTTS/Voices";

        private int modelIndex;
        private Vector2 scroll;
        private bool working;
        private string status = "";

        [MenuItem("LocalTTS/Model Manager")]
        public static void Open() => GetWindow<ModelManagerWindow>("LocalTTS Models");

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(working))
            {
                DrawModelSection();
                EditorGUILayout.Space(12);
                DrawVoiceSection();
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }
        }

        private void DrawModelSection()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);

            string[] labels = KokoroCatalog.Models
                .Select(m => $"{m.Name} — {m.SizeBytes / (1024 * 1024)} MB — {m.Note}")
                .ToArray();
            modelIndex = EditorGUILayout.Popup("Variant", modelIndex, labels);

            var entry = KokoroCatalog.Models[modelIndex];
            string path = ModelAssetPathFor(entry);
            bool present = CatalogDownloader.IsPresent(entry, Path.GetFullPath(path));

            EditorGUILayout.LabelField("Status", present ? $"Installed at {path}" : "Not downloaded");

            if (GUILayout.Button(present ? "Re-download (verify)" : "Download Model")
                && ConfirmSize(entry))
            {
                _ = DownloadModelAsync(entry, path);
            }

            if (present)
            {
                DrawQuantizeButtons(path);
            }
        }

        private void DrawQuantizeButtons(string float32Path)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "Smaller variants (weight-quantized .sentis, same speed):", EditorStyles.miniLabel);

            using var row = new EditorGUILayout.HorizontalScope();
            foreach (var type in new[]
                     { Unity.InferenceEngine.QuantizationType.Float16,
                       Unity.InferenceEngine.QuantizationType.Uint8 })
            {
                string qPath = ModelQuantizerUtil.QuantizedAssetPath(type);
                bool exists = File.Exists(Path.GetFullPath(qPath));
                if (GUILayout.Button(exists ? $"{type} ✓ (rebuild)" : $"Create {type}"))
                {
                    var src = AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>(float32Path);
                    ModelQuantizerUtil.CreateQuantizedCopy(src, type);
                    status = $"Quantized copy ready: {qPath}";
                }
            }
        }

        private void DrawVoiceSection()
        {
            EditorGUILayout.LabelField("English Voices", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Each download creates a TTSVoice asset in " + VoiceFolder, EditorStyles.miniLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var entry in KokoroCatalog.EnglishVoices)
            {
                using var row = new EditorGUILayout.HorizontalScope();
                string binPath = $"{VoiceFolder}/{entry.Name}.bytes";
                bool present = CatalogDownloader.IsPresent(entry, Path.GetFullPath(binPath));

                EditorGUILayout.LabelField(entry.Note, GUILayout.MinWidth(160));
                EditorGUILayout.LabelField(present ? "✓" : "", GUILayout.Width(20));
                if (GUILayout.Button(present ? "Re-download" : "Download", GUILayout.Width(110)))
                {
                    _ = DownloadVoiceAsync(entry, binPath);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static bool ConfirmSize(KokoroCatalog.Entry entry)
        {
            return EditorUtility.DisplayDialog("LocalTTS",
                $"Download {entry.Name} ({entry.SizeBytes / (1024 * 1024)} MB) from Hugging Face?\n\n{entry.Url}",
                "Download", "Cancel");
        }

        public static string ModelAssetPathFor(KokoroCatalog.Entry entry)
            => $"{ModelPaths.ModelAssetFolder}/kokoro-v1.0-{entry.Name.ToLowerInvariant()}.onnx";

        private async Awaitable DownloadModelAsync(KokoroCatalog.Entry entry, string assetPath)
        {
            await RunJob(entry, Path.GetFullPath(assetPath));
            AssetDatabase.ImportAsset(assetPath);
            status = $"Model ready: {assetPath}. Assign it to a TTSEngineProvider.";
        }

        private async Awaitable DownloadVoiceAsync(KokoroCatalog.Entry entry, string binPath)
        {
            await RunJob(entry, Path.GetFullPath(binPath));
            AssetDatabase.ImportAsset(binPath);

            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(binPath);
            string assetPath = $"{VoiceFolder}/{entry.Name}.asset";
            var voice = AssetDatabase.LoadAssetAtPath<TTSVoice>(assetPath);
            if (voice == null)
            {
                voice = CreateInstance<TTSVoice>();
                voice.Initialize(entry.Note, data);
                AssetDatabase.CreateAsset(voice, assetPath);
            }
            else
            {
                voice.Initialize(entry.Note, data);
                EditorUtility.SetDirty(voice);
            }

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(voice);
            status = $"Voice ready: {assetPath}";
        }

        private async Awaitable RunJob(KokoroCatalog.Entry entry, string destination)
        {
            working = true;
            try
            {
                float progress = 0;
                var task = CatalogDownloader.DownloadAsync(entry, destination, p => progress = p);
                while (!task.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar("LocalTTS", $"Downloading {entry.Name}…", progress);
                    await Awaitable.NextFrameAsync();
                }

                await task; // rethrows on failure
            }
            catch (Exception e)
            {
                status = $"FAILED: {e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                working = false;
                Repaint();
            }
        }
    }
}
