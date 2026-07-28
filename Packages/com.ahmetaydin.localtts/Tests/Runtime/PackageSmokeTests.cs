using NUnit.Framework;
using UnityEngine;

namespace LocalTTS.Tests
{
    public class PackageSmokeTests
    {
        [Test]
        public void RuntimeAssembly_IsLoaded()
        {
            // Fails at compile time if the Runtime asmdef or its Inference Engine
            // reference is broken — the real point of this smoke test.
            Assert.That(typeof(TTSSettings).Assembly.GetName().Name,
                Is.EqualTo("LocalTTS.Runtime"));
        }

        [Test]
        public void AudioSystem_IsAvailable()
        {
            // Playmode guard: CI batch runners sometimes disable audio; synthesis
            // output needs AudioClip creation to work.
            Assert.That(AudioSettings.outputSampleRate, Is.GreaterThan(0));
        }
    }
}
