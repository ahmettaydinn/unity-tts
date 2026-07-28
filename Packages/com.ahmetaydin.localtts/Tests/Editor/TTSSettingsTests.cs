using NUnit.Framework;

namespace LocalTTS.Tests
{
    public class TTSSettingsTests
    {
        [Test]
        public void Default_UsesCpuBackendAndFloat16()
        {
            var settings = TTSSettings.Default;

            Assert.That(settings.Backend, Is.EqualTo(TTSBackend.CpuBurst));
            Assert.That(settings.Quantization, Is.EqualTo(TTSQuantization.Float16));
            Assert.That(settings.DefaultSpeed, Is.EqualTo(1f));
        }

        [Test]
        public void OutputSampleRate_MatchesKokoroVocoder()
        {
            Assert.That(TTSSettings.OutputSampleRate, Is.EqualTo(24000));
        }
    }
}
