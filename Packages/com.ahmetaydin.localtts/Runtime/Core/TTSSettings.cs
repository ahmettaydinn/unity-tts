using System;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>Which compute backend runs model inference.</summary>
    public enum TTSBackend
    {
        /// <summary>Burst-compiled CPU inference. Works everywhere; no GPU contention.</summary>
        CpuBurst,

        /// <summary>Compute-shader GPU inference. Faster on desktop; competes with rendering.</summary>
        GpuCompute,
    }

    /// <summary>Model weight precision. Smaller is faster to load and lighter in memory.</summary>
    public enum TTSQuantization
    {
        Float32,
        Float16,
        Uint8,
    }

    /// <summary>
    /// Immutable configuration for a <c>TTSEngine</c> instance.
    /// </summary>
    [Serializable]
    public sealed class TTSSettings
    {
        [SerializeField] private TTSBackend backend = TTSBackend.CpuBurst;
        [SerializeField] private TTSQuantization quantization = TTSQuantization.Float16;
        [SerializeField, Range(0.5f, 2f)] private float defaultSpeed = 1f;

        [Tooltip("Max milliseconds of model scheduling per frame. Inference is spread " +
                 "across frames to avoid hitches; 0 schedules everything in one frame.")]
        [SerializeField, Range(0f, 16f)] private float frameBudgetMs = 4f;

        /// <summary>Output sample rate of the Kokoro vocoder, in Hz.</summary>
        public const int OutputSampleRate = 24000;

        public TTSBackend Backend => backend;
        public TTSQuantization Quantization => quantization;
        public float DefaultSpeed => defaultSpeed;

        /// <summary>Max model-scheduling milliseconds per frame; 0 = single-frame scheduling.</summary>
        public float FrameBudgetMs => frameBudgetMs;

        public TTSSettings() { }

        public TTSSettings(TTSBackend backend,
            TTSQuantization quantization = TTSQuantization.Float16, float defaultSpeed = 1f,
            float frameBudgetMs = 4f)
        {
            this.backend = backend;
            this.quantization = quantization;
            this.defaultSpeed = defaultSpeed;
            this.frameBudgetMs = frameBudgetMs;
        }

        public static TTSSettings Default => new TTSSettings();
    }
}
