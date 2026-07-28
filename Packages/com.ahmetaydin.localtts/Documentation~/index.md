# Local TTS

Fully local, on-device text-to-speech for game characters: Kokoro-82M on Unity
Inference Engine. No cloud, no API keys, license-clean for commercial games
(Apache-2.0 model, MIT package, no GPL anywhere in the chain).

## Quickstart (5 minutes)

1. **Install** via Package Manager → *Install package from git URL*:
   `https://github.com/ahmettaydinn/unity-tts.git?path=/Packages/com.ahmetaydin.localtts`
2. **Download the model + a voice**: menu **LocalTTS → Model Manager**. Pick *Float32*
   (recommended) and download; then download a voice — each voice becomes a `TTSVoice`
   asset in `Assets/LocalTTS/Voices/`. All downloads are SHA-256 verified.
3. **Bootstrap**: add **TTS Engine Provider** to a scene object; assign the model asset.
   Optionally assign a warmup voice (recommended for GPU backend).
4. **Speak**: add **Character Voice** (+ AudioSource) to your character, assign a
   `TTSVoice`, and call:

```csharp
GetComponent<CharacterVoice>().Speak("Hello, traveler! The inn costs $5 a night.");
```

Text is normalized automatically (currency, numbers, ordinals, abbreviations…),
sentences are chunked and pipelined, and playback happens on the character's own
AudioSource — spatial blend, mixer routing, and volume behave like any game audio.

## Core API (no components)

```csharp
var engine = await TTSEngine.CreateAsync(modelAsset, new TTSSettings(TTSBackend.CpuBurst));
SynthesisResult result = await engine.SynthesizeAsync("Hello!", voiceAsset.Voice);
audioSource.PlayOneShot(result.ToAudioClip());
```

- Concurrent `SynthesizeAsync` calls are serviced FIFO — call it from as many
  characters as you like.
- `CharacterVoice.Interrupt()` implements barge-in: stops audio, clears the queue,
  discards in-flight synthesis.
- Custom pronunciations: `engine.G2P` exposes the lexicon —
  `lexicon.AddOverride("Aldrith", "ˈɔldɹɪθ")` for invented names.

## Voice preview

**LocalTTS → Voice Preview** auditions any downloaded voice from the editor, no play
mode needed.

## How big is this at runtime?

| Item | Disk | Notes |
|---|---|---|
| Model (Float32) | 310 MB | Verified default |
| Model (Float16) | 156 MB | Experimental import |
| Model (Uint8) | 88 MB | Experimental import |
| Each voice | 0.5 MB | Ship as many as you like |
| Lexicon (in package) | 1.3 MB | Decompresses to ~25 MB dictionary in memory |
| Engine RAM overhead | roughly model size + working buffers | Measured properly in Phase 4 |

Performance on Apple Silicon (fp32): CPU synthesis ≈ 7× faster than real time;
GPU ≈ 14×. Engine cold start ≈ 3 s (lexicon parse + warmup synth).

## Current limitations

- English only (multilingual is on the roadmap; `IG2P` is the plug-in point).
- Heteronyms use their most common reading ("read", "lead").
- Unknown words are spelled letter-by-letter — add pronunciation overrides for
  invented names.
- One synthesis at a time per engine (requests queue; latency adds up under load).
