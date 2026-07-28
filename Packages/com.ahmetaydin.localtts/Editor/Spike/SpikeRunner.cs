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
    /// Phase 1 spike: run Kokoro end-to-end (phonemes → waveform → WAV on disk) and print
    /// timings. Runnable from the menu or headless via
    /// <c>-executeMethod LocalTTS.Editor.SpikeRunner.RunBatch</c>.
    /// </summary>
    public static class SpikeRunner
    {
        // "Hello! This is a test of local speech, running inside Unity."
        private const string Phonemes =
            "həlˈoʊ! ðɪs ɪz ɐ tˈɛst ʌv lˈoʊkəl spˈiːʧ, ɹˈʌnɪŋ ɪnsˈaɪd jˈuːnɪɾi.";

        // Library/ persists across editor sessions (unlike Temp/, which Unity deletes on exit).
        public static string OutputDir =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Library", "LocalTTSSpike");

        [MenuItem("LocalTTS/Spike/Run Synthesis (CPU)")]
        public static void RunCpu() => Run(TTSBackend.CpuBurst);

        [MenuItem("LocalTTS/Spike/Run Synthesis (GPU)")]
        public static void RunGpu() => Run(TTSBackend.GpuCompute);

        /// <summary>Headless entry point: runs CPU then GPU, exits non-zero on failure.</summary>
        public static void RunBatch()
        {
            try
            {
                Run(TTSBackend.CpuBurst);
                Run(TTSBackend.GpuCompute);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Run(TTSBackend backend)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
            if (modelAsset == null)
            {
                throw new FileNotFoundException(
                    $"Model asset missing at {SpikeModelDownloader.ModelAssetPath}. " +
                    "Run 'LocalTTS/Spike/Download Model + Voice' first.");
            }

            var voice = new KokoroVoice("af_heart",
                File.ReadAllBytes(Path.GetFullPath(SpikeModelDownloader.VoiceAssetPath)));

            var loadTimer = Stopwatch.StartNew();
            using var synth = new KokoroSynthesizer(modelAsset, backend);
            loadTimer.Stop();

            // First run includes backend warmup/compilation; second run is the honest number.
            var warmupTimer = Stopwatch.StartNew();
            float[] samples = synth.Synthesize(Phonemes, voice);
            warmupTimer.Stop();

            var timer = Stopwatch.StartNew();
            samples = synth.Synthesize(Phonemes, voice);
            timer.Stop();

            float durationSec = samples.Length / (float)TTSSettings.OutputSampleRate;
            double rms = 0;
            foreach (float s in samples) rms += s * s;
            rms = Math.Sqrt(rms / samples.Length);

            if (samples.Length < TTSSettings.OutputSampleRate / 2 || rms < 1e-4)
            {
                throw new InvalidOperationException(
                    $"[{backend}] Suspicious output: {samples.Length} samples, RMS {rms:F6} — synthesis likely broken.");
            }

            Directory.CreateDirectory(OutputDir);
            string wavPath = Path.Combine(OutputDir, $"spike_{backend}.wav");
            WavWriter.Write(wavPath, samples, TTSSettings.OutputSampleRate);

            Debug.Log(
                $"[LocalTTS spike | {backend}] audio {durationSec:F2}s, RMS {rms:F4} | " +
                $"model+worker load {loadTimer.ElapsedMilliseconds} ms, " +
                $"first synth (warmup) {warmupTimer.ElapsedMilliseconds} ms, " +
                $"second synth {timer.ElapsedMilliseconds} ms " +
                $"(RTF {timer.ElapsedMilliseconds / 1000.0 / durationSec:F3}) | wav: {wavPath}");
        }
    }
}
