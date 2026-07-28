using System.Collections.Generic;

namespace LocalTTS.Kokoro
{
    /// <summary>
    /// Converts a phoneme string (IPA symbols from <see cref="KokoroVocab"/>) into the
    /// padded token-id sequence the Kokoro model consumes.
    /// </summary>
    public static class KokoroTokenizer
    {
        /// <summary>
        /// Encodes phonemes to token ids, wrapped in the pad token at both ends.
        /// Symbols missing from the vocabulary are skipped and reported in
        /// <paramref name="unknownSymbols"/>.
        /// </summary>
        public static int[] Encode(string phonemes, out List<char> unknownSymbols)
        {
            unknownSymbols = new List<char>();
            var ids = new List<int>(phonemes.Length + 2) { KokoroVocab.PadId };

            foreach (char c in phonemes)
            {
                if (KokoroVocab.SymbolToId.TryGetValue(c, out int id))
                {
                    // Room for the trailing pad within the model's context window.
                    if (ids.Count >= KokoroVocab.MaxTokens - 1)
                    {
                        break;
                    }

                    ids.Add(id);
                }
                else
                {
                    unknownSymbols.Add(c);
                }
            }

            ids.Add(KokoroVocab.PadId);
            return ids.ToArray();
        }
    }
}
