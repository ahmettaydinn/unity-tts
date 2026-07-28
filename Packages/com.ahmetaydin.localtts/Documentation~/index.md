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

## How big is this at runtime? (measured, Apple Silicon, Inference Engine 2.6)

| Variant | Disk | Synth 5.2 s line (CPU / GPU) | Memory Δ (CPU / GPU) | Quality |
|---|---|---|---|---|
| Float32 ONNX | 310 MB | 636 ms / 384 ms | +410 / +360 MB | reference |
| Weights-Float16 .sentis | 175 MB | 755 ms / 393 ms | +479 / +218 MB | identical |
| **Weights-Uint8 .sentis** ← recommended | **108 MB** | 689 ms / 391 ms | +414 / **+158 MB** | identical |

Plus per voice 0.5 MB, lexicon 1.3 MB on disk (~25 MB dictionary in memory).
Engine cold start ≈ 0.3–3 s (lexicon parse + warmup synth; GPU shader compilation
adds ~19 s once per machine, absorbed by the warmup voice).

Create the quantized variants with one click in **LocalTTS → Model Manager** (they are
produced locally from the Float32 download — the pre-quantized ONNX files on Hugging
Face use operators Inference Engine cannot import, and fp16 ONNX runs ~35× slower
than real time; both are deliberately unsupported). A standalone player build using
the Weights-Uint8 model is exercised by the repo's smoke test.

## Frame-time honesty

`TTSSettings.frameBudgetMs` (default 4) spreads model scheduling across frames, but
Kokoro contains individual layers (LSTM/vocoder) that block **150–650 ms in a single
step** — a hard floor that layer-level yielding cannot split. Practical guidance:

- Synthesize during loading screens, dialogue-open moments, or scene transitions —
  `CharacterVoice` queues lines, so request speech a beat before it must play.
- The GPU backend has the smaller stalls (~150–390 ms) and 1.7× faster synthesis.
- Fully hitch-free synthesis during hot gameplay is future work (job-thread or
  command-buffer execution).

## Current limitations

- English only (multilingual is on the roadmap; `IG2P` is the plug-in point).
- Heteronyms use their most common reading ("read", "lead").
- Unknown words are spelled letter-by-letter — add pronunciation overrides for
  invented names.
- One synthesis at a time per engine (requests queue; latency adds up under load).
- Main-thread stalls of 150–650 ms during synthesis (see above).
