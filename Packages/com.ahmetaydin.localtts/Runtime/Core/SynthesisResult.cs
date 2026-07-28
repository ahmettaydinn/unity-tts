using LocalTTS.Kokoro;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>Output of one synthesis request: mono 24 kHz samples plus diagnostics.</summary>
    public sealed class SynthesisResult
    {
        public float[] Samples { get; }

        /// <summary>The phoneme strings actually fed to the model, one per sentence.</summary>
        public string[] SentencePhonemes { get; }

        public float DurationSeconds => Samples.Length / (float)TTSSettings.OutputSampleRate;

        public SynthesisResult(float[] samples, string[] sentencePhonemes)
        {
            Samples = samples;
            SentencePhonemes = sentencePhonemes;
        }

        public AudioClip ToAudioClip(string name = "LocalTTS")
            => KokoroSynthesizer.ToAudioClip(Samples, name);
    }
}
