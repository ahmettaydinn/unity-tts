using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Audition TTSVoice assets without entering play mode: pick a voice, type a line,
    /// press Speak. Uses a CPU engine so it never touches the game's GPU budget.
    /// </summary>
    public sealed class VoicePreviewWindow : EditorWindow
    {
        private TTSVoice[] voices = Array.Empty<TTSVoice>();
        private int voiceIndex;
        private string text = "Hello, traveler! Welcome to the village.";
        private TTSEngine engine;
        private bool busy;
        private string status = "";

        [MenuItem("LocalTTS/Voice Preview")]
        public static void Open() => GetWindow<VoicePreviewWindow>("Voice Preview");

        private void OnEnable() => RefreshVoices();

        private void OnDisable()
        {
            engine?.Dispose();
            engine = null;
        }

        private void RefreshVoices()
        {
            voices = AssetDatabase.FindAssets("t:TTSVoice")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TTSVoice>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(v => v != null && v.IsValid)
                .ToArray();
        }

        private void OnGUI()
        {
            if (voices.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No TTSVoice assets found. Download voices via LocalTTS → Model Manager.",
                    MessageType.Info);
                if (GUILayout.Button("Refresh"))
                {
                    RefreshVoices();
                }

                return;
            }

            using (new EditorGUI.DisabledScope(busy))
            {
                voiceIndex = EditorGUILayout.Popup("Voice",
                    Mathf.Clamp(voiceIndex, 0, voices.Length - 1),
                    voices.Select(v => v.DisplayName).ToArray());
                text = EditorGUILayout.TextArea(text, GUILayout.MinHeight(48));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Speak"))
                    {
                        _ = SpeakAsync(voices[voiceIndex]);
                    }

                    if (GUILayout.Button("Refresh Voices", GUILayout.Width(110)))
                    {
                        RefreshVoices();
                    }
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.HelpBox(status, MessageType.None);
            }
        }

        private async Awaitable SpeakAsync(TTSVoice voice)
        {
            busy = true;
            status = "Synthesizing…";
            Repaint();
            try
            {
                if (engine == null)
                {
                    var modelAsset = FindModelAsset();
                    engine = await TTSEngine.CreateAsync(
                        modelAsset, new TTSSettings(TTSBackend.CpuBurst));
                }

                SynthesisResult result = await engine.SynthesizeAsync(text, voice.Voice);
                PlayPreview(result);
                status = $"{result.DurationSeconds:F1}s — {voice.DisplayName}";
            }
            catch (Exception e)
            {
                status = $"FAILED: {e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private static ModelAsset FindModelAsset()
        {
            foreach (var entry in KokoroCatalog.Models)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(
                    ModelManagerWindow.ModelAssetPathFor(entry));
                if (asset != null)
                {
                    return asset;
                }
            }

            // Spike-era default path, then any ModelAsset in the model folder.
            var spike = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
            if (spike != null)
            {
                return spike;
            }

            throw new InvalidOperationException(
                "No Kokoro model found — download one via LocalTTS → Model Manager.");
        }

        private static System.Diagnostics.Process previewProcess;

        /// <summary>
        /// Plays through the OS audio player (afplay on macOS, SoundPlayer on
        /// Windows). Deliberately not the editor's internal preview API: that is
        /// affected by the editor mute toggle and moved assemblies across Unity
        /// versions — an audition tool should just always be audible.
        /// </summary>
        private static void PlayPreview(SynthesisResult result)
        {
            string path = Path.Combine(Path.GetTempPath(), "localtts-preview.wav");
            WavWriter.Write(path, result.Samples, TTSSettings.OutputSampleRate);

            try { previewProcess?.Kill(); } catch { /* already exited */ }

#if UNITY_EDITOR_OSX
            previewProcess = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("afplay", $"\"{path}\"")
                { UseShellExecute = false, CreateNoWindow = true });
#elif UNITY_EDITOR_WIN
            previewProcess = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("powershell",
                    $"-NoProfile -WindowStyle Hidden -Command \"(New-Object Media.SoundPlayer '{path}').PlaySync()\"")
                { UseShellExecute = false, CreateNoWindow = true });
#else
            EditorUtility.OpenWithDefaultApp(path);
#endif
        }
    }
}
