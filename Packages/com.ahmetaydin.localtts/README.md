# Local TTS for Unity

Fully local, on-device text-to-speech for game characters. Runs
[Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) on
[Unity Inference Engine](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/index.html)
— no cloud, no API keys, no per-line recording sessions, and **license-clean for
commercial games** (Apache-2.0 model, MIT package code, no GPL anywhere in the chain).

## Features

- **Plain text in, speech out**: `character.Speak("The inn costs $5 a night.")` —
  currency, numbers, ordinals, and abbreviations are normalized automatically.
- **28 English voices** (US + British, male + female) as drag-and-drop assets, with an
  in-editor preview window. Every character can sound different for 0.5 MB each.
- **Game-ready components**: per-character speech queues, interrupt/barge-in,
  `LineStarted`/`FinishedSpeaking` events for subtitles and animation, playback on the
  character's own AudioSource (3D spatialization for free).
- **Small enough to ship**: 108 MB model (uint8 weights, identical quality), CPU
  synthesis ~7× faster than real time on desktop; GPU ~13×.
- **All Unity platforms**: pure C# + Inference Engine. No native plugins.

## Install

Package Manager → *Install package from git URL*:

```
https://github.com/ahmettaydinn/unity-tts.git?path=/Packages/com.ahmetaydin.localtts
```

Requires Unity 6000.0+.

## Quickstart

1. **LocalTTS → Model Manager** — download the model + a voice (SHA-256 verified),
   optionally create the recommended 108 MB uint8 variant with one click.
2. Add **TTS Engine Provider** to a scene object; assign the model.
3. Add **Character Voice** (+ AudioSource) to your character; assign a voice asset.
4. `GetComponent<CharacterVoice>().Speak("Hello, traveler!");`

Full docs: [Documentation~/index.md](Documentation~/index.md) — includes the measured
performance/memory table, frame-time guidance, and the core no-components API.

## License

Package code: [MIT](LICENSE.md). Model (Apache-2.0) and data: see
[Third Party Notices](Third%20Party%20Notices.md).
