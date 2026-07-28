using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalTTS.G2P
{
    /// <summary>
    /// Dictionary-based English G2P: normalized sentence → Kokoro phoneme string.
    /// Lookup order: pronunciation overrides → lexicon → suffix rules ('s, s) →
    /// hyphen split → letter-by-letter spelling for acronyms and unknown words.
    /// Pure C#; safe on background threads.
    /// </summary>
    public sealed class EnglishG2P : IG2P
    {
        public string LanguageTag => "en-US";

        private readonly Lexicon lexicon;

        /// <summary>Words the lexicon missed (spelled out instead) — for diagnostics.</summary>
        public readonly HashSet<string> UnknownWords = new HashSet<string>();

        // Letter-name pronunciations (misaki symbol set: A=eɪ, I=aɪ, O=oʊ).
        private static readonly string[] LetterNames =
        {
            "ˈA", "bˈi", "sˈi", "dˈi", "ˈi", "ˈɛf", "ʤˈi", "ˈAʧ", "ˈI", "ʤˈA", "kˈA",
            "ˈɛl", "ˈɛm", "ˈɛn", "ˈO", "pˈi", "kjˈu", "ˈɑɹ", "ˈɛs", "tˈi", "jˈu",
            "vˈi", "dˈʌbəljˌu", "ˈɛks", "wˈI", "zˈi",
        };

        private static readonly Regex TokenRx = new Regex(
            @"[A-Za-z]+(?:['’][A-Za-z]+)*(?:-[A-Za-z]+(?:['’][A-Za-z]+)*)*|[;:,.!?—…“”""()]",
            RegexOptions.Compiled);

        private const string VoicelessFinals = "ptkfθ";
        private const string SibilantFinals = "szʃʒʧʤ";

        public EnglishG2P(Lexicon lexicon) => this.lexicon = lexicon;

        public IReadOnlyList<string> Phonemize(string sentence)
        {
            var parts = new List<string>();
            foreach (Match m in TokenRx.Matches(sentence))
            {
                string token = m.Value;
                if (token.Length == 1 && !char.IsLetter(token[0]))
                {
                    parts.Add(token); // punctuation carries prosody; keep it
                }
                else
                {
                    parts.Add(PhonemizeWord(token));
                }
            }

            return parts;
        }

        /// <summary>Full sentence to a single phoneme string ready for the tokenizer.</summary>
        public string PhonemizeToString(string sentence)
        {
            var sb = new StringBuilder();
            foreach (string part in Phonemize(sentence))
            {
                bool isPunct = part.Length == 1 && !char.IsLetter(part[0])
                    && !KokoroIsPhoneme(part[0]);
                if (isPunct)
                {
                    sb.Append(part);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(part);
                }
            }

            return sb.ToString();
        }

        private static bool KokoroIsPhoneme(char c) => char.IsLetter(c);

        private string PhonemizeWord(string word)
        {
            word = word.Replace('’', '\'');

            if (lexicon.TryGetPronunciation(word, out string phonemes))
            {
                return phonemes;
            }

            // Possessive / clitic suffixes on a known base: cat's, dogs', it'll…
            foreach (var (suffix, tail) in ClicitTails)
            {
                if (word.EndsWith(suffix) && word.Length > suffix.Length
                    && lexicon.TryGetPronunciation(word.Substring(0, word.Length - suffix.Length), out string basePh))
                {
                    return basePh + (tail ?? SibilantTail(basePh));
                }
            }

            // Hyphenated compounds: phonemize each part.
            if (word.Contains('-'))
            {
                return string.Join(" ", word.Split('-').Select(PhonemizeWord));
            }

            // Acronyms (GPU, NPC) and unknown words: spell letter names.
            if (!word.All(char.IsLetter) || word.Length == 0)
            {
                return "";
            }

            UnknownWords.Add(word);
            return string.Join("", word.ToUpperInvariant()
                .Where(c => c is >= 'A' and <= 'Z')
                .Select(c => LetterNames[c - 'A']));
        }

        // suffix → fixed phoneme tail, or null for voicing-dependent s/z/ɪz.
        private static readonly (string suffix, string tail)[] ClicitTails =
        {
            ("'s", null), ("s'", null), ("s", null),
            ("'ll", "əl"), ("'re", "ɚ"), ("'ve", "əv"), ("'d", "d"), ("n't", "ənt"),
        };

        private static string SibilantTail(string basePhonemes)
        {
            char last = ' ';
            for (int i = basePhonemes.Length - 1; i >= 0; i--)
            {
                char c = basePhonemes[i];
                if (c is not ('ˈ' or 'ˌ' or 'ː' or ' '))
                {
                    last = c;
                    break;
                }
            }

            if (SibilantFinals.IndexOf(last) >= 0)
            {
                return "ɪz";
            }

            return VoicelessFinals.IndexOf(last) >= 0 ? "s" : "z";
        }
    }
}
