# Changelog

All notable changes to this package are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

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
