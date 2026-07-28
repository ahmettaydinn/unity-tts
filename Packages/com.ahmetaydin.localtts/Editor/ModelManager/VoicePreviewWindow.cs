using System;
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
                PlayPreview(result.ToAudioClip("VoicePreview"));
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

        /// <summary>Plays a clip in the editor via the internal AudioUtil (reflection).</summary>
        private static void PlayPreview(AudioClip clip)
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var play = audioUtil?.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            var stop = audioUtil?.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

            if (play == null)
            {
                Debug.LogWarning("LocalTTS: editor audio preview unavailable in this Unity version.");
                return;
            }

            stop?.Invoke(null, null);
            play.Invoke(null, new object[] { clip, 0, false });
        }
    }
}
