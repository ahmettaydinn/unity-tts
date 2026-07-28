using System;
using System.IO;

namespace LocalTTS.Editor
{
    /// <summary>Writes mono float samples as a 16-bit PCM WAV file (spike/debug tooling).</summary>
    public static class WavWriter
    {
        public static void Write(string path, float[] samples, int sampleRate)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            int dataBytes = samples.Length * 2;

            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + dataBytes);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);              // PCM chunk size
            writer.Write((short)1);        // PCM format
            writer.Write((short)1);        // mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);  // byte rate
            writer.Write((short)2);        // block align
            writer.Write((short)16);       // bits per sample
            writer.Write("data".ToCharArray());
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write((short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
