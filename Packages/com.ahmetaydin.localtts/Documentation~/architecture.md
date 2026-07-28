# Architecture

How LocalTTS works, why it's shaped this way, and where it grows. Written for
contributors and for anyone deciding whether to trust it in a game.

## 1. The problem and the three bets

Game characters need voices. The existing options: record voice actors (expensive,
can't speak dynamic text), cloud TTS APIs (latency, per-call cost, no offline play),
or local TTS (historically robotic or legally radioactive). The design goal:
**cloud-quality speech, generated on the player's machine, that a commercial game can
legally ship.**

That goal forced three foundational bets; everything in the codebase follows from them.

**Bet 1 — Kokoro-82M as the model.** Best quality-per-megabyte of any permissively
licensed TTS model at the time of writing: 82M parameters, Apache-2.0, 28+ English
voices. The competition failed on license (Piper moved to GPL-3.0, XTTS is
non-commercial) or size (Chatterbox ~500M parameters).

**Bet 2 — Unity Inference Engine as the runtime.** The model is an ONNX graph;
something must execute it. Native ONNX Runtime would mean shipping per-platform
binaries forever. Inference Engine (`com.unity.ai.inference`) is a first-party Unity
package that runs ONNX on Burst-compiled CPU jobs or GPU compute shaders on every
platform Unity supports. Zero native code in this package.

**Bet 3 — a pure C# G2P pipeline.** TTS models don't eat text; they eat *phonemes*
(`həlˈoʊ`, not "hello"). Nearly every Kokoro integration phonemizes with
**espeak-ng, which is GPL** — shipping it inside a closed-source game would
contaminate the game. LocalTTS replaces it with a dictionary approach: 183k
pronunciations from misaki's Apache-2.0 data packed into a 1.3 MB resource, plus C#
rules. This is also why English shipped first: the license-clean path and the easy
path happened to coincide.

## 2. The pipeline

```
"Dr. Smith paid $1,499.99!"                          ← game dev's string
        │  TextNormalizer            (pure C#, background thread)
"Doctor Smith paid one thousand four hundred..."
        │  sentence splitting        (keeps .!? — the model uses them for prosody)
        │  EnglishG2P                (lexicon lookup + suffix rules + spelling fallback)
"dˈɑktəɹ smˈɪθ pˈAd wˈʌn θˈWzᵊnd ..."               ← phoneme string
        │  KokoroTokenizer           (char → int via vocab table, pad token 0 at ends)
[0, 46, 51, ..., 0]                                   ← token ids
        │  + voice style vector + speed  →  Worker.Schedule()   (main thread)
float[~106k]                                          ← 24 kHz waveform from the model
        │  AudioClip.Create + SetData
AudioSource.Play()                                    ← ordinary game audio from here on
```

Two facts worth internalizing:

- **The model is prosody-aware.** Punctuation and stress marks change delivery —
  that's why the normalizer *preserves* `!` and `?` instead of stripping them.
- **A voice is just data**: 510 rows × 256 floats of style embedding, where row *N*
  is used for an *N*-phoneme input. Swapping voices costs nothing; 28 voices fit in
  ~14 MB.

## 3. The code, layer by layer

Four runtime layers, each depending only on the one below, plus editor tooling.

### Layer 1 — Kokoro primitives (`Runtime/Kokoro/`)

The model's "device drivers":

- [KokoroVocab.cs](../Runtime/Kokoro/KokoroVocab.cs) — char→id table *generated* from
  the model's own tokenizer.json, so it cannot drift from the model.
- [KokoroTokenizer.cs](../Runtime/Kokoro/KokoroTokenizer.cs) — phoneme string →
  padded id array; skips unknown symbols rather than crashing.
- [KokoroVoice.cs](../Runtime/Kokoro/KokoroVoice.cs) — parses the raw 522,240-byte
  voice files and hands out the right style row.
- [KokoroSynthesizer.cs](../Runtime/Kokoro/KokoroSynthesizer.cs) — minimal
  synchronous "phonemes in, samples out" wrapper around an Inference Engine `Worker`.

### Layer 2 — Language processing (`Runtime/G2P/`)

Deliberately **pure C# with no UnityEngine API in the hot paths**, so it runs on
background threads:

- [TextNormalizer.cs](../Runtime/G2P/TextNormalizer.cs) — regex-driven expansion of
  currency / decimals / ordinals / abbreviations; number-to-words up to trillions.
- [Lexicon.cs](../Runtime/G2P/Lexicon.cs) — loads the gzipped TSV from
  `Runtime/Resources/`; `AddOverride()` is the escape hatch for invented names.
- [EnglishG2P.cs](../Runtime/G2P/EnglishG2P.cs) — lookup cascade: overrides →
  lexicon → clitic suffix rules (`cat's` = `cat` + voicing-aware `s`) → hyphen
  split → letter-by-letter spelling for acronyms and unknowns.
- [IG2P.cs](../Runtime/G2P/IG2P.cs) — **the multilingual extension point**. A new
  language is one new `IG2P` implementation; the engine doesn't change.

### Layer 3 — The engine ([TTSEngine.cs](../Runtime/Core/TTSEngine.cs))

Where async orchestration lives:

- `CreateAsync` parses the lexicon off the main thread and runs a **warmup
  synthesis** — GPU shader compilation costs ~19 s once per machine, and that must
  never land on the first dialogue line.
- `SynthesizeAsync` hops to a background thread for text processing, back to the
  main thread for tensor work (an Inference Engine requirement), then per sentence:
  schedule → async readback → concatenate with 120 ms gaps. Sentence chunking is
  what makes long paragraphs feel responsive.
- **Concurrency**: a FIFO ownership-handoff gate. Many `CharacterVoice`s can call
  simultaneously; requests serialize with no queue-jumping. The subtlety: a
  finishing request passes ownership *directly* to the next waiter — the busy flag
  never drops to false in between, so a newcomer can't cut the line.
- `frameBudgetMs` schedules the graph layer by layer, yielding when the frame's
  budget is spent — **play mode only**: edit mode has no reliably ticking player
  loop, and suspending mid-graph corrupts the worker (see Scars, below).

### Layer 4 — Game-facing components (`Runtime/Components/`, `Runtime/Voices/`)

- [TTSVoice.cs](../Runtime/Voices/TTSVoice.cs) — voice-as-ScriptableObject: voices
  are drag-and-drop Inspector assets, not file paths in code.
- [TTSEngineProvider.cs](../Runtime/Components/TTSEngineProvider.cs) — scene
  singleton owning the one shared engine (the model is 100–400 MB in memory; you
  never want two). Statics reset via `RuntimeInitializeOnLoadMethod` for "Enter Play
  Mode without domain reload".
- [CharacterVoice.cs](../Runtime/Components/CharacterVoice.cs) — per-character
  queue; `Interrupt()` barge-in via a generation counter (in-flight synthesis from
  before the interrupt is discarded, not played); `LineStarted` /
  `FinishedSpeaking` events for subtitles and animation; playback on the
  character's own AudioSource so 3D audio and mixer routing come free.

### Editor tooling (`Editor/`)

- [ModelManagerWindow.cs](../Editor/ModelManager/ModelManagerWindow.cs) exists
  because of a packaging rule: **models never live in the package or in git**.
  Downloads come from Hugging Face and are verified against
  [KokoroCatalog.cs](../Editor/ModelManager/KokoroCatalog.cs) — a *generated,
  pinned* manifest of file sizes and SHA-256 hashes. A corrupted or tampered
  download fails loudly instead of producing garbled speech.
- [ModelQuantizerUtil.cs](../Editor/ModelManager/ModelQuantizerUtil.cs) creates the
  recommended 108 MB uint8-weights `.sentis` variant locally (see Scars for why the
  pre-quantized downloads are deliberately unsupported).
- [VoicePreviewWindow.cs](../Editor/ModelManager/VoicePreviewWindow.cs) auditions
  voices without play mode, playing through the OS audio player (`afplay` /
  `SoundPlayer`) because the editor's internal preview API is silenced by the
  editor mute toggle and moves between assemblies across Unity versions.
- `Editor/Spike/` is the dev lab: benchmark matrix, headless pipeline runners, and
  the standalone player smoke-test build.

### Tests

Mirroring the risk profile: golden tests for the pure C# layers (cheap and
exhaustive — `$1,499.99`, `cat's`, `GPU`); one integration test that speaks a real
line through the whole component stack (skips automatically where the model isn't
downloaded, so CI stays green); and a standalone-build smoke test for what unit
tests can't see.

## 4. Scars — decisions made by measurement, not taste

1. **fp16 ONNX is a trap**: measured 35× *slower* than real time on CPU in Inference
   Engine 2.6. **Pre-quantized uint8 ONNX doesn't import at all** (`MatMulInteger`,
   `ConvInteger`, `DynamicQuantizeLSTM` are unsupported ops). Hence local weight
   quantization: same speed, identical audio, one third the size.
2. **The "<2 ms frame stall" goal is unachievable** with current APIs: individual
   layers (LSTM/vocoder) block 150–650 ms atomically, and layer-level yielding
   cannot split one layer. The docs say so honestly and teach the workaround
   (request lines a beat early) rather than pretending.
3. **`SerializedObject.objectReferenceValue` silently drops asset references in
   `-batchmode`** (observed on Unity 6000.5.5f1). Editor scene generators assign
   private fields via reflection instead.
4. **Editor tooling is a different runtime**: frames don't tick reliably in edit
   mode (which corrupted mid-graph scheduling) and the internal audio-preview API
   is unreliable (silent under the editor mute toggle). Both bugs were found by
   manual testing that automated suites missed.

## 5. Expansion joints

The architecture has three prepared growth points:

- **Languages** plug in at `IG2P` (Spanish/Italian/Portuguese first — Latin scripts
  are rule-friendly; misaki has data to port for ja/zh later).
- **Lip-sync**: surface phoneme timings from the model's duration predictor through
  `SynthesisResult` — the type already carries per-sentence phonemes for this
  reason.
- **Backends**: if Inference Engine gains job-thread or command-buffer execution,
  the 650 ms stall problem dies inside `RunModelAsync` with no public API change.
