using System.Collections.Generic;
using LocalTTS.G2P;
using NUnit.Framework;

namespace LocalTTS.Tests
{
    public class TextNormalizerTests
    {
        [TestCase("$5", "five dollars")]
        [TestCase("$1", "one dollar")]
        [TestCase("$1,499.99", "one thousand four hundred ninety nine dollars and ninety nine cents")]
        [TestCase("£20", "twenty pounds")]
        [TestCase("42%", "forty two percent")]
        [TestCase("3.14", "three point one four")]
        [TestCase("1st", "first")]
        [TestCase("2nd", "second")]
        [TestCase("3rd", "third")]
        [TestCase("21st", "twenty first")]
        [TestCase("40th", "fortieth")]
        [TestCase("100", "one hundred")]
        [TestCase("1,000,000", "one million")]
        [TestCase("0", "zero")]
        public void Normalize_ExpandsTokens(string input, string expected)
        {
            Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
        }

        [TestCase("Dr. Smith lives on St. Mary Ave.", "Doctor Smith lives on Saint Mary Avenue")]
        [TestCase("cats vs. dogs, etc.", "cats versus dogs, et cetera")]
        public void Normalize_ExpandsAbbreviations(string input, string expected)
        {
            Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
        }

        [Test]
        public void Normalize_CollapsesWhitespace()
        {
            Assert.That(TextNormalizer.Normalize("  hello \n  world  "), Is.EqualTo("hello world"));
        }

        [Test]
        public void NormalizeAndSplit_SplitsSentencesKeepingTerminators()
        {
            List<string> s = TextNormalizer.NormalizeAndSplit("Stop! Who goes there? The cave is ahead.");
            Assert.That(s, Is.EqualTo(new[] { "Stop!", "Who goes there?", "The cave is ahead." }));
        }

        [Test]
        public void NumberToWords_HandlesLargeNumbers()
        {
            Assert.That(TextNormalizer.NumberToWords(1_234_567),
                Is.EqualTo("one million two hundred thirty four thousand five hundred sixty seven"));
        }
    }
}
