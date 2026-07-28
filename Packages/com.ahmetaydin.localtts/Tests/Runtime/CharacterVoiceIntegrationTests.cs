using System.Collections;
using System.IO;
using LocalTTS;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LocalTTS.Tests
{
    /// <summary>
    /// End-to-end component test: provider + CharacterVoice speak a real line. Skips
    /// automatically when the model/voice files are absent (e.g. cloud CI), so it runs
    /// wherever a developer has done the Model Manager setup.
    /// </summary>
    public class CharacterVoiceIntegrationTests
    {
#if UNITY_EDITOR
        private const string SpikeModelPath = "Assets/LocalTTS/Models/kokoro-v1.0.onnx";
        private const string SpikeVoicePath = "Assets/LocalTTS/Models/af_heart.bin";

        [UnityTest]
        public IEnumerator CharacterVoice_SpeaksALine()
        {
            var modelAsset = UnityEditor.AssetDatabase
                .LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>(SpikeModelPath);
            if (modelAsset == null || !File.Exists(Path.GetFullPath(SpikeVoicePath)))
            {
                Assert.Ignore("Kokoro model/voice not downloaded — skipping integration test.");
            }

            // Inactive while wiring: Awake must not run before the model is assigned.
            var providerGo = new GameObject("Provider");
            providerGo.SetActive(false);
            var provider = providerGo.AddComponent<TTSEngineProvider>();
            SetPrivateField(provider, "model", modelAsset);
            providerGo.SetActive(true);

            var voice = ScriptableObject.CreateInstance<TTSVoice>();
            var styleData = new TextAsset(); // placeholder; runtime voice injected below

            var characterGo = new GameObject("Character");
            characterGo.AddComponent<AudioSource>();
            var character = characterGo.AddComponent<CharacterVoice>();
            character.Voice = voice;

            // Bypass the TextAsset (binary TextAssets can't be constructed at runtime):
            // put the KokoroVoice straight into TTSVoice's cache via reflection.
            SetPrivateField(voice, "runtime", new LocalTTS.Kokoro.KokoroVoice(
                "af_heart", File.ReadAllBytes(Path.GetFullPath(SpikeVoicePath))));
            SetPrivateField(voice, "styleData", styleData);

            string started = null;
            bool finished = false;
            character.LineStarted += line => started = line;
            character.FinishedSpeaking += () => finished = true;

            character.Speak("Testing one two three.");

            float deadline = Time.realtimeSinceStartup + 120f;
            while (!finished && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(started, Is.EqualTo("Testing one two three."), "line never started");
            Assert.That(finished, Is.True, "speech never finished");

            Object.Destroy(characterGo);
            Object.Destroy(providerGo);
        }

        private static void SetPrivateField(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(target, value);
        }
#endif
    }
}
