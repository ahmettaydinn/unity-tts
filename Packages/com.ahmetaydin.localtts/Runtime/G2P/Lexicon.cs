using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace LocalTTS.G2P
{
    /// <summary>
    /// English pronunciation dictionary: word → Kokoro phoneme string. Built from the
    /// misaki gold/silver US lexicons (Apache-2.0), shipped as a gzipped TSV resource.
    /// </summary>
    public sealed class Lexicon
    {
        public const string ResourcePath = "LocalTTS/lexicon-en-us";

        private readonly Dictionary<string, string> entries;
        private readonly Dictionary<string, string> overrides =
            new Dictionary<string, string>();

        public int Count => entries.Count;

        private Lexicon(Dictionary<string, string> entries) => this.entries = entries;

        /// <summary>Loads the packed lexicon TextAsset. Main thread only.</summary>
        public static byte[] ReadPackedBytes()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new FileNotFoundException($"Lexicon resource missing: {ResourcePath}");
            }

            return asset.bytes;
        }

        /// <summary>Parses the gzipped TSV. Safe to run on a background thread.</summary>
        public static Lexicon FromGzipBytes(byte[] gzipBytes)
        {
            var dict = new Dictionary<string, string>(200_000);
            using var reader = new StreamReader(
                new GZipStream(new MemoryStream(gzipBytes), CompressionMode.Decompress));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int tab = line.IndexOf('\t');
                if (tab > 0)
                {
                    dict[line.Substring(0, tab)] = line.Substring(tab + 1);
                }
            }

            return new Lexicon(dict);
        }

        /// <summary>
        /// Case-aware lookup: project overrides first, then exact match, then lowercase.
        /// </summary>
        public bool TryGetPronunciation(string word, out string phonemes)
        {
            return overrides.TryGetValue(word, out phonemes)
                || overrides.TryGetValue(word.ToLowerInvariant(), out phonemes)
                || entries.TryGetValue(word, out phonemes)
                || entries.TryGetValue(word.ToLowerInvariant(), out phonemes);
        }

        /// <summary>Per-project pronunciation override (character names, invented words).</summary>
        public void AddOverride(string word, string phonemes) => overrides[word] = phonemes;
    }
}
