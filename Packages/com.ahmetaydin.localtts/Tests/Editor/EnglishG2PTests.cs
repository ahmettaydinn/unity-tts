using LocalTTS.G2P;
using NUnit.Framework;

namespace LocalTTS.Tests
{
    public class EnglishG2PTests
    {
        private static Lexicon lexicon;
        private static EnglishG2P g2p;

        [OneTimeSetUp]
        public void LoadLexicon()
        {
            lexicon = Lexicon.FromGzipBytes(Lexicon.ReadPackedBytes());
            g2p = new EnglishG2P(lexicon);
        }

        [Test]
        public void Lexicon_LoadsFullDictionary()
        {
            Assert.That(lexicon.Count, Is.GreaterThan(180_000));
        }

        [TestCase("hello")]
        [TestCase("Hello")]  // case fallback
        [TestCase("traveler")]
        [TestCase("cave")]
        public void KnownWords_ComeFromLexicon(string word)
        {
            Assert.That(lexicon.TryGetPronunciation(word, out string p), Is.True, $"'{word}' missing");
            Assert.That(p, Is.Not.Empty);
        }

        [Test]
        public void Possessive_AppendsVoicingAwareSuffix()
        {
            // 'cat' ends voiceless /t/ → possessive tail must be 's', not 'z'.
            string phonemes = g2p.PhonemizeToString("the cat's tail");
            lexicon.TryGetPronunciation("cat", out string cat);
            Assert.That(phonemes, Does.Contain(cat + "s"));
        }

        [Test]
        public void Acronyms_AreSpelledOut()
        {
            // GPU → letter names: ʤˈi pˈi jˈu (G, P, U).
            string phonemes = g2p.PhonemizeToString("GPU");
            Assert.That(phonemes, Does.Contain("ʤˈi").And.Contain("pˈi").And.Contain("jˈu"));
        }

        [Test]
        public void Punctuation_IsPreservedForProsody()
        {
            string phonemes = g2p.PhonemizeToString("Wait, stop!");
            Assert.That(phonemes, Does.Contain(",").And.Contain("!"));
        }

        [Test]
        public void UnknownWord_FallsBackToSpelling_AndIsRecorded()
        {
            var local = new EnglishG2P(lexicon);
            string phonemes = local.PhonemizeToString("zzyzxq");
            Assert.That(phonemes, Is.Not.Empty);
            Assert.That(local.UnknownWords, Does.Contain("zzyzxq"));
        }

        [Test]
        public void Override_WinsOverLexiconAndSpelling()
        {
            var lex = Lexicon.FromGzipBytes(Lexicon.ReadPackedBytes());
            lex.AddOverride("Aldrith", "ˈɔldɹɪθ"); // invented game-character name
            var local = new EnglishG2P(lex);
            Assert.That(local.PhonemizeToString("Aldrith"), Is.EqualTo("ˈɔldɹɪθ"));
        }

        [Test]
        public void FullSentence_ProducesOnlyVocabSymbols()
        {
            string phonemes = g2p.PhonemizeToString(
                TextNormalizer.Normalize("Dr. Smith paid $1,499.99 for the 3rd GPU!"));
            foreach (char c in phonemes)
            {
                Assert.That(LocalTTS.Kokoro.KokoroVocab.SymbolToId.ContainsKey(c), Is.True,
                    $"symbol '{c}' (U+{(int)c:X4}) not in Kokoro vocab: {phonemes}");
            }
        }
    }
}
