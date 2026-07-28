# Changelog

All notable changes to this package are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Phase 4, hardening & performance:
  - Local weight quantization (`ModelQuantizerUtil` + Model Manager buttons):
    Uint8-weights .sentis is the shipping recommendation — 108 MB (vs 310 MB fp32),
    identical output and speed, less than half the GPU memory.
  - `TTSSettings.frameBudgetMs`: frame-budgeted layer-by-layer scheduling, with
    worst-stall instrumentation on `TTSEngine`; documented honest limits (individual
    heavy layers still stall 150–650 ms).
  - Benchmark matrix runner (`LocalTTS/Spike/Run Benchmark Matrix`) and a standalone
    player smoke-test build (`BuildSmoke`), verified end-to-end on macOS (Mono).
  - Domain-reload safety: provider statics reset via RuntimeInitializeOnLoadMethod.

### Removed

- Float16 and Uint8 ONNX downloads from the catalog: fp16 ONNX measured at RTF ~35
  on CPU (unusable); pre-quantized uint8 ONNX uses operators Inference Engine cannot
  import (MatMulInteger, ConvInteger, DynamicQuantizeLSTM…). Local weight
  quantization replaces both.

### Fixed

- Scene generators now assign asset references via reflection:
  `SerializedObject.objectReferenceValue` silently drops asset references in
  batch mode (observed on Unity 6000.5.5f1).

- Phase 3, package UX:
  - `TTSVoice` ScriptableObject voices; **LocalTTS → Model Manager** downloads the
    model (3 quantization variants) and 29 English voices with pinned SHA-256
    verification, auto-creating ready-to-assign voice assets.
  - **LocalTTS → Voice Preview**: audition voices in-editor without play mode.
  - `TTSEngineProvider` (scene-shared engine with warmup on Awake) and
    `CharacterVoice` (per-character queue, `Interrupt()` barge-in, `LineStarted`/
    `FinishedSpeaking` events, plays on the object's own AudioSource).
  - `TTSEngine` now serializes concurrent requests FIFO instead of throwing.
  - Samples: *Basic Speech* and *Dialogue Characters* (registered in package.json).
  - Documentation~: quickstart, core API, runtime size table.

- Phase 2, English pipeline: `TTSEngine` — plain English text → speech, fully async
  (background-thread G2P, awaitable GPU readback, backend warmup at creation).
- `EnglishG2P`: 183k-word lexicon (misaki gold+silver, packed to a 1.3 MB gzipped
  resource, every entry validated against the Kokoro vocab), voicing-aware possessive
  and clitic suffix rules, acronym/unknown-word letter spelling, per-project
  pronunciation overrides (`Lexicon.AddOverride`).
- `TextNormalizer`: currency ($/£/€ with cents), percentages, decimals, ordinals,
  large cardinals, common abbreviations, sentence splitting with prosody punctuation.
- Golden-test suite (28 new tests). Measured: 13.9 s of audio from a 4-sentence
  paragraph in ~2.0 s on CPU; engine cold start ~2.8 s including lexicon and warmup.

- Phase 1 spike: Kokoro-82M runs end-to-end in Unity Inference Engine on CPU and GPU.
  `KokoroSynthesizer` (phonemes → 24 kHz waveform → AudioClip), `KokoroTokenizer` +
  generated vocab table, `KokoroVoice` style-embedding loader.
- Spike editor tooling: model/voice downloader, headless benchmark runner
  (`LocalTTS/Spike` menu), WAV writer, and a playable spike scene generator.
- Measured on Apple Silicon (fp32 model): CPU RTF ≈ 0.13, GPU (Metal) RTF ≈ 0.07;
  ~0.5 s to synthesize a 4.5 s line on CPU.

## [0.1.0] - 2026-07-28

### Added

- Package skeleton: Runtime/Editor/Tests assembly definitions.
- `TTSSettings` (backend, quantization, speed) and the `IG2P` language interface.
- Editor `ModelPaths` constants for the upcoming model downloader.
- CI: EditMode/PlayMode tests via GameCI, package layout validation, line-ending check.
