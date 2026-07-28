using System.Linq;
using LocalTTS.Editor;
using NUnit.Framework;

namespace LocalTTS.Tests
{
    public class KokoroCatalogTests
    {
        [Test]
        public void Catalog_OffersOnlyFloat32Download()
        {
            // Deliberate: fp16 ONNX measured at RTF ~35 on CPU (unusable), and the
            // pre-quantized uint8 ONNX uses operators Inference Engine cannot import
            // (MatMulInteger, ConvInteger, DynamicQuantizeLSTM…). Smaller variants are
            // produced locally via ModelQuantizerUtil weight quantization instead.
            Assert.That(KokoroCatalog.Models.Select(m => m.Name),
                Is.EquivalentTo(new[] { "Float32" }));
        }

        [Test]
        public void Catalog_HasEnglishVoices()
        {
            Assert.That(KokoroCatalog.EnglishVoices.Length, Is.GreaterThanOrEqualTo(25));
            Assert.That(KokoroCatalog.EnglishVoices.Select(v => v.Name), Does.Contain("af_heart"));
        }

        [Test]
        public void Entries_AreWellFormed()
        {
            foreach (var e in KokoroCatalog.Models.Concat(KokoroCatalog.EnglishVoices))
            {
                Assert.That(e.Sha256, Does.Match("^[0-9a-f]{64}$"), e.Name);
                Assert.That(e.SizeBytes, Is.GreaterThan(0), e.Name);
                Assert.That(e.Url, Does.StartWith("https://huggingface.co/"), e.Name);
            }
        }

        [Test]
        public void VoiceEntries_HaveExactStyleTableSize()
        {
            foreach (var v in KokoroCatalog.EnglishVoices)
            {
                Assert.That(v.SizeBytes, Is.EqualTo(510 * 256 * 4), v.Name);
            }
        }
    }
}
