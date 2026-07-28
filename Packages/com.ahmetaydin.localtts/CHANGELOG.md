# Changelog

All notable changes to this package are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2026-07-28

### Fixed

- Edit-mode synthesis (Voice Preview, editor tooling) corrupted the inference
  worker's state: frame-budgeted scheduling suspended mid-graph without a ticking
  player loop (`KeyNotFoundException` inside the worker). Frame-yielding now only
  activates in play mode.
- Voice Preview was silent: playback now goes through the OS audio player
  (afplay / SoundPlayer) instead of Unity's internal editor preview API, which is
  affected by the editor mute toggle and moved assemblies in Unity 6.5.

## [1.0.0] - 2026-07-28

First release: fully local, license-clean text-to-speech for game characters.
Kokoro-82M on Unity Inference Engine — English, 28 voices, CPU or GPU inference,
no cloud, no API keys, no native plugins, no GPL anywhere in the chain.

### Added

**Speech engine**
- `TTSEngine`: plain English text → 24 kHz speech, fully async (background-thread
  text processing, awaitable GPU readback, warmup at creation to absorb one-time
  GPU shader compilation). Concurrent requests are serviced FIFO.
- `EnglishG2P`: 183k-word pronunciation lexicon (misaki gold+silver data, packed to
  a 1.3 MB gzipped resource, every entry validated against the Kokoro vocabulary),
  voicing-aware possessive/clitic suffix rules, acronym and unknown-word letter
  spelling, per-project pronunciation overrides (`Lexicon.AddOverride`).
- `TextNormalizer`: currency ($/£/€ with cents), percentages, decimals, ordinals,
  large cardinals, common abbreviations, sentence splitting that preserves
  prosody punctuation.
- `TTSSettings.frameBudgetMs`: frame-budgeted layer-by-layer scheduling with
  worst-stall instrumentation (see documentation for honest limits).

**Components**
- `TTSEngineProvider`: scene-shared engine with warmup on Awake; safe under
  "Enter Play Mode without domain reload".
- `CharacterVoice`: per-character speech queue, `Interrupt()` barge-in,
  `LineStarted`/`FinishedSpeaking` events, playback on the object's own
  AudioSource (spatialization and mixer routing come free).
- `TTSVoice`: voices as assignable ScriptableObject assets.

**Editor tooling**
- **LocalTTS → Model Manager**: downloads the model and 28 English voices with
  pinned SHA-256 verification; one-click local weight quantization — the
  recommended Uint8-weights variant is 108 MB (vs 310 MB fp32) with identical
  speed and output and less than half the GPU memory.
- **LocalTTS → Voice Preview**: audition any voice in-editor without play mode.
- Benchmark matrix and standalone player smoke-test build (verified end-to-end
  on macOS).

**Samples**
- *Basic Speech* and *Dialogue Characters*, installable from the Package Manager.

### Notes

- Pre-quantized fp16/uint8 ONNX variants are deliberately unsupported: fp16 ONNX
  measured ~35× slower than real time on CPU; uint8 ONNX uses operators Inference
  Engine cannot import. Local weight quantization replaces both.
- Measured on Apple Silicon (fp32): CPU synthesis ≈ 7–8× faster than real time,
  GPU ≈ 13×; 13.9 s of audio from a 4-sentence paragraph in ~2.0 s on CPU.

## [0.1.0] - 2026-07-28

### Added

- Package skeleton: Runtime/Editor/Tests assembly definitions.
- `TTSSettings` (backend, quantization, speed) and the `IG2P` language interface.
- Editor `ModelPaths` constants for the upcoming model downloader.
- CI: EditMode/PlayMode tests via GameCI, package layout validation, line-ending check.
