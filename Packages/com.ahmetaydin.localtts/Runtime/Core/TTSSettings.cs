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

        /// <summary>Output sample rate of the Kokoro vocoder, in Hz.</summary>
        public const int OutputSampleRate = 24000;

        public TTSBackend Backend => backend;
        public TTSQuantization Quantization => quantization;
        public float DefaultSpeed => defaultSpeed;

        public static TTSSettings Default => new TTSSettings();
    }
}
