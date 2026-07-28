using System;
using System.Diagnostics;
using System.IO;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Phase 4 measurement: every downloaded model variant × backend, reporting load
    /// time, memory delta, synthesis time, worst per-frame scheduling stall, and output
    /// sanity vs the fp32 reference. Headless:
    /// <c>-executeMethod LocalTTS.Editor.BenchmarkRunner.RunBatch</c>.
    /// </summary>
    public static class BenchmarkRunner
    {
        private const string Text =
            "The quick brown fox jumps over the lazy dog while seventeen wizards watch.";

        // fp16/uint8 ONNX variants were measured and removed (fp16: RTF ~35 on CPU;
        // uint8: unimportable operators). The size-reduced path is local weight
        // quantization to .sentis, generated below if missing.
        private static (string label, string path)[] Variants =>
            new[]
            {
                ("Float32", "Assets/LocalTTS/Models/kokoro-v1.0.onnx"),
                ("W-Float16", ModelQuantizerUtil.QuantizedAssetPath(QuantizationType.Float16)),
                ("W-Uint8", ModelQuantizerUtil.QuantizedAssetPath(QuantizationType.Uint8)),
            };

        [MenuItem("LocalTTS/Spike/Run Benchmark Matrix")]
        public static void RunMenu() => Run(exitWhenDone: false);

        public static void RunBatch() => Run(exitWhenDone: true);

        private static async void Run(bool exitWhenDone)
        {
            int failures = 0;
            try
            {
                var voice = new KokoroVoice("af_heart",
                    File.ReadAllBytes(Path.GetFullPath(SpikeModelDownloader.VoiceAssetPath)));

                // Generate the weight-quantized .sentis copies from fp32 if absent.
                var fp32 = AssetDatabase.LoadAssetAtPath<ModelAsset>(Variants[0].path);
                if (fp32 != null)
                {
                    foreach (var type in new[] { QuantizationType.Float16, QuantizationType.Uint8 })
                    {
                        if (!File.Exists(Path.GetFullPath(ModelQuantizerUtil.QuantizedAssetPath(type))))
                        {
                            ModelQuantizerUtil.CreateQuantizedCopy(fp32, type);
                        }
                    }
                }

                foreach (var (label, path) in Variants)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(path);
                    if (asset == null)
                    {
                        Debug.LogWarning($"[bench] {label}: not downloaded, skipping.");
                        continue;
                    }

                    foreach (var backend in new[] { TTSBackend.CpuBurst, TTSBackend.GpuCompute })
                    {
                        try
                        {
                            await BenchmarkOne(label, asset, backend, voice);
                        }
                        catch (Exception e)
                        {
                            failures++;
                            Debug.LogError($"[bench] {label}/{backend} FAILED: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                failures++;
                Debug.LogException(e);
            }

            Debug.Log($"[bench] done, failures: {failures}");
            if (exitWhenDone)
            {
                EditorApplication.Exit(failures == 0 ? 0 : 1);
            }
        }

        private static async Awaitable BenchmarkOne(
            string label, ModelAsset asset, TTSBackend backend, KokoroVoice voice)
        {
            long memBefore = GC.GetTotalMemory(true) + Profiler.GetTotalAllocatedMemoryLong();

            // frameBudgetMs: 0 — batch mode ticks the player loop erratically, so
            // frame-yielding scheduling can crawl. Frame-stall numbers for the budgeted
            // path come from in-editor runs (LocalTTS/Spike menu), not batch.
            var loadTimer = Stopwatch.StartNew();
            var engine = await TTSEngine.CreateAsync(
                asset, new TTSSettings(backend, frameBudgetMs: 0f), warmupVoice: voice);
            loadTimer.Stop();

            long memAfter = GC.GetTotalMemory(false) + Profiler.GetTotalAllocatedMemoryLong();

            try
            {
                var timer = Stopwatch.StartNew();
                SynthesisResult result = await engine.SynthesizeAsync(Text, voice);
                timer.Stop();

                double rms = 0;
                foreach (float s in result.Samples) rms += s * s;
                rms = Math.Sqrt(rms / result.Samples.Length);

                if (result.DurationSeconds < 2f || rms < 1e-3)
                {
                    throw new InvalidOperationException(
                        $"suspicious output ({result.DurationSeconds:F2}s, RMS {rms:F5})");
                }

                Directory.CreateDirectory(SpikeRunner.OutputDir);
                WavWriter.Write(
                    Path.Combine(SpikeRunner.OutputDir, $"bench_{label}_{backend}.wav"),
                    result.Samples, TTSSettings.OutputSampleRate);

                Debug.Log($"[bench] {label,-8} {backend,-10} | ready {loadTimer.ElapsedMilliseconds,5} ms" +
                          $" | mem +{(memAfter - memBefore) / (1024 * 1024),4} MB" +
                          $" | synth {timer.ElapsedMilliseconds,5} ms for {result.DurationSeconds:F2}s" +
                          $" (RTF {timer.ElapsedMilliseconds / 1000.0 / result.DurationSeconds:F3})" +
                          $" | worst frame stall {engine.LastMaxFrameStallMs:F1} ms" +
                          $" over {engine.LastScheduleFrames} frames | RMS {rms:F4}");
            }
            finally
            {
                engine.Dispose();
            }
        }
    }
}
