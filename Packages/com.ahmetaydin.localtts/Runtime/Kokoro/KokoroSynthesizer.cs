using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

namespace LocalTTS.Kokoro
{
    /// <summary>
    /// Minimal synchronous Kokoro synthesis: phoneme string in, 24 kHz mono samples out.
    /// Phase 1 spike implementation — the async, frame-budgeted engine wraps this later.
    /// </summary>
    public sealed class KokoroSynthesizer : IDisposable
    {
        private readonly Worker worker;

        public KokoroSynthesizer(ModelAsset modelAsset, TTSBackend backend)
        {
            var model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, backend == TTSBackend.GpuCompute
                ? BackendType.GPUCompute
                : BackendType.CPU);
        }

        /// <summary>Synthesizes speech; blocks until the waveform is ready.</summary>
        public float[] Synthesize(string phonemes, KokoroVoice voice, float speed = 1f)
        {
            int[] ids = KokoroTokenizer.Encode(phonemes, out List<char> unknown);
            if (unknown.Count > 0)
            {
                Debug.LogWarning($"KokoroSynthesizer: skipped symbols not in vocab: '{string.Concat(unknown)}'");
            }

            // Inputs in model order: input_ids [1,N], style [1,256], speed [1].
            using var inputIds = new Tensor<int>(new TensorShape(1, ids.Length), ids);
            using var style = new Tensor<float>(
                new TensorShape(1, KokoroVoice.StyleDim), voice.GetStyle(ids.Length - 2));
            using var speedTensor = new Tensor<float>(new TensorShape(1), new[] { speed });

            worker.Schedule(inputIds, style, speedTensor);

            using var output = ((Tensor<float>)worker.PeekOutput()).ReadbackAndClone();
            return output.DownloadToArray();
        }

        /// <summary>Wraps synthesized samples in an AudioClip ready for an AudioSource.</summary>
        public static AudioClip ToAudioClip(float[] samples, string name = "LocalTTS")
        {
            var clip = AudioClip.Create(name, samples.Length, 1, TTSSettings.OutputSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Dispose() => worker?.Dispose();
    }
}
