# Local TTS for Unity

Fully local, on-device text-to-speech for game characters. Runs
[Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) on
[Unity Inference Engine](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/index.html)
— no cloud, no API keys, no per-character audio recording sessions.

> **Status: pre-release scaffolding (v0.1.0).** The synthesis pipeline lands in upcoming
> milestones — see [PLAN.md](../../PLAN.md) at the repo root.

## Why

- **License-clean for commercial games**: Apache-2.0 model, MIT package code, and a pure C#
  grapheme-to-phoneme pipeline — no GPL espeak-ng anywhere in the chain.
- **54 voices** out of the box; a voice is a tiny style-embedding asset, so every character
  can sound different.
- **All Unity platforms**: pure C# + Inference Engine (CPU Burst or GPU compute). No native
  plugins to manage.

## Install

Via git URL in Unity Package Manager:

```
https://github.com/ahmettaydinn/unity-tts.git?path=/Packages/com.ahmetaydin.localtts
```

Requires Unity 6000.0+.

## Planned API

```csharp
var engine = await TTSEngine.CreateAsync(TTSSettings.Default);
AudioClip clip = (await engine.SynthesizeAsync("Hello, traveler!", voice)).Clip;
```

Model weights (~90–330 MB depending on quantization) are fetched once by an editor tool and
are never committed to your repo.

## License

Package code: [MIT](LICENSE.md). Model and data: see [Third Party Notices](Third%20Party%20Notices.md).
