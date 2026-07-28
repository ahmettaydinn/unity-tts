using System.Collections.Generic;

namespace LocalTTS.G2P
{
    /// <summary>
    /// Converts normalized text into the phoneme token sequence the acoustic model consumes.
    /// One implementation per language; registered with the engine at load time.
    /// </summary>
    public interface IG2P
    {
        /// <summary>BCP-47 language tag this converter handles, e.g. "en-US".</summary>
        string LanguageTag { get; }

        /// <summary>
        /// Converts a single sentence to phonemes. Must be safe to call from a background
        /// thread — no UnityEngine API access inside implementations.
        /// </summary>
        IReadOnlyList<string> Phonemize(string sentence);
    }
}
