using System;

namespace LocalTTS.Kokoro
{
    /// <summary>
    /// A Kokoro voice: 510 style-embedding rows of 256 floats, one row per possible
    /// phoneme-sequence length. Loaded from the raw little-endian float32 .bin files
    /// distributed with the model.
    /// </summary>
    public sealed class KokoroVoice
    {
        public const int Rows = 510;
        public const int StyleDim = 256;

        private readonly float[] styles; // [Rows * StyleDim]

        public string Name { get; }

        public KokoroVoice(string name, byte[] rawBin)
        {
            if (rawBin.Length != Rows * StyleDim * sizeof(float))
            {
                throw new ArgumentException(
                    $"Voice '{name}': expected {Rows * StyleDim * sizeof(float)} bytes, got {rawBin.Length}.");
            }

            Name = name;
            styles = new float[Rows * StyleDim];
            Buffer.BlockCopy(rawBin, 0, styles, 0, rawBin.Length);
        }

        /// <summary>Style vector for a phoneme sequence of the given length (without pads).</summary>
        public float[] GetStyle(int phonemeCount)
        {
            int row = Math.Clamp(phonemeCount - 1, 0, Rows - 1);
            var result = new float[StyleDim];
            Array.Copy(styles, row * StyleDim, result, 0, StyleDim);
            return result;
        }
    }
}
