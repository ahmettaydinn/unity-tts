# Changelog

All notable changes to this package are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

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
