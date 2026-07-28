using System;
using System.Diagnostics;
using System.IO;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Phase 2 verification: plain English text → TTSEngine → WAV, headless.
    /// Run via menu or <c>-executeMethod LocalTTS.Editor.PipelineRunner.RunBatch</c>.
    /// </summary>
    public static class PipelineRunner
    {
        private const string Text =
            "Dr. Smith paid $1,499.99 for the 3rd GPU! Was it worth it? " +
            "The dragon's lair lies 40 miles north. Good luck, traveler.";

        [MenuItem("LocalTTS/Spike/Run Text Pipeline (CPU)")]
        public static void RunCpuMenu() => RunAsync(TTSBackend.CpuBurst, exitWhenDone: false);

        public static void RunBatch() => RunAsync(TTSBackend.CpuBurst, exitWhenDone: true);

        private static async void RunAsync(TTSBackend backend, bool exitWhenDone)
        {
            try
            {
                var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
                if (modelAsset == null)
                {
                    throw new FileNotFoundException("Spike model missing — run the downloader first.");
                }

                var voice = new KokoroVoice("af_heart",
                    File.ReadAllBytes(Path.GetFullPath(SpikeModelDownloader.VoiceAssetPath)));

                var loadTimer = Stopwatch.StartNew();
                using var engine = await TTSEngine.CreateAsync(
                    modelAsset, new TTSSettings(), warmupVoice: voice);
                loadTimer.Stop();

                var timer = Stopwatch.StartNew();
                SynthesisResult result = await engine.SynthesizeAsync(Text, voice);
                timer.Stop();

                double rms = 0;
                foreach (float s in result.Samples) rms += s * s;
                rms = Math.Sqrt(rms / result.Samples.Length);
                if (result.DurationSeconds < 2f || rms < 1e-4)
                {
                    throw new InvalidOperationException(
                        $"Suspicious output: {result.DurationSeconds:F2}s, RMS {rms:F6}");
                }

                Directory.CreateDirectory(SpikeRunner.OutputDir);
                string wavPath = Path.Combine(SpikeRunner.OutputDir, $"pipeline_{backend}.wav");
                WavWriter.Write(wavPath, result.Samples, TTSSettings.OutputSampleRate);

                Debug.Log($"[LocalTTS pipeline | {backend}] {result.SentencePhonemes.Length} sentences, " +
                          $"{result.DurationSeconds:F2}s audio | engine ready in {loadTimer.ElapsedMilliseconds} ms " +
                          $"(incl. lexicon + warmup), full synthesis {timer.ElapsedMilliseconds} ms | " +
                          $"unknown words: [{string.Join(", ", engine.G2P.UnknownWords)}] | wav: {wavPath}");
                foreach (string p in result.SentencePhonemes)
                {
                    Debug.Log($"  phonemes: {p}");
                }

                if (exitWhenDone)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (exitWhenDone)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
