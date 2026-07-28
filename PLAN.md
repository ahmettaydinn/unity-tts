# Unity Local TTS Package — Development Plan

> Goal: a Unity package (UPM) that game developers can fetch and drop into their project to give
> game characters fully **local / on-device** speech. English first, multilingual later.
> Development on macOS + Windows.

---

## 1. Model selection

### Candidates evaluated

| Model | Params / size | Quality | Languages | Model license | Blocking issue |
|---|---|---|---|---|---|
| **Kokoro-82M** ✅ | 82M, ~330 MB fp32 / ~170 MB fp16 / ~90 MB int8 (ONNX) | Excellent for its size, best quality-per-MB in open TTS | 8 (EN, JA, ZH, ES, FR, HI, IT, PT) | **Apache-2.0** | Default G2P (espeak-ng) is GPL — solvable, see below |
| Piper | 15–60 MB per voice | Good but audibly synthetic | 30+ | **GPL-3.0** (project moved to `piper1-gpl` in Oct 2025, espeak-ng now embedded) | GPL is a non-starter for closed-source games |
| Supertonic 3 | 99M | Very good, extremely fast (1200+ chars/s CPU) | 31 | MIT code, **OpenRAIL-M model** | OpenRAIL-M use restrictions need legal comfort; strong candidate for the multilingual phase |
| XTTS v2 (Coqui) | ~470M | Excellent + voice cloning | 17 | **CPML — non-commercial** | Can't ship in commercial games |
| Chatterbox | ~500M | Excellent | EN | MIT | Too heavy for a game runtime budget |
| KittenTTS | ~15M | Mediocre | EN | Apache-2.0 | Quality too low for character dialogue |

### Decision: **Kokoro-82M (ONNX) on Unity Inference Engine**

Why:
- **License-clean end to end**: Apache-2.0 weights. The one trap — Kokoro's reference pipeline
  phonemizes text with espeak-ng (GPL) — is avoided by using a **pure C# dictionary-based G2P**
  (misaki-style lexicon, ~130k English entries). Unity's own official Sentis sample
  (`Unity-Technologies/sentis-samples` → TextToSpeechSample) proves this exact combination works:
  Kokoro-82M ONNX + C# G2P + GPU inference, running on all Unity platforms.
- **Best quality-per-megabyte** of any permissively-licensed local model in 2026 — repeatedly the
  top recommendation for offline TTS without a GPU requirement.
- **54 built-in voices** across accents/genders → characters get distinct voices with zero training.
  Voice = small style-embedding file (~few hundred KB), so shipping many voices is cheap.
- **Small enough for games**: int8 ~90 MB, fp16 ~170 MB. CPU real-time on desktop; GPU-accelerated
  via Inference Engine compute backend.

Runtime: **Unity Inference Engine** (`com.unity.ai.inference`, formerly Sentis) rather than native
ONNX Runtime plugins — it's a first-party UPM dependency, runs the same ONNX on CPU (Burst) or GPU
(compute) across every Unity platform, supports fp16/uint8 quantization, and means **zero native
binaries to build/maintain per platform** (critical for a two-OS dev team and for consumers).

Fallback/complement: **Supertonic 3** as a second backend in the multilingual phase (31 languages,
built for on-device ONNX, no espeak dependency) if Kokoro's non-English G2P proves too costly to
port — subject to OpenRAIL-M license review.

---

## 2. Package architecture

**Package name**: `com.<yourorg>.localtts` (working title: *VoiceForge*, rename freely).

```
com.<yourorg>.localtts/
├── package.json                  # UPM manifest; depends on com.unity.ai.inference
├── README.md / CHANGELOG.md / LICENSE.md / Third Party Notices.md
├── Runtime/
│   ├── LocalTTS.Runtime.asmdef
│   ├── Core/
│   │   ├── TTSEngine.cs          # loads model, owns Inference Engine Worker, schedules inference
│   │   ├── TTSRequest.cs         # text + voice + speed + priority
│   │   ├── SynthesisResult.cs    # AudioClip + phoneme timings (for lip-sync)
│   │   └── AudioClipBuilder.cs   # float[] 24 kHz → AudioClip (streamed or one-shot)
│   ├── G2P/
│   │   ├── IG2P.cs               # language-pluggable interface
│   │   ├── EnglishG2P.cs         # lexicon lookup + rules (numbers, currency, dates, acronyms)
│   │   ├── Lexicon.cs            # binary-packed 130k-entry dictionary, lazy-loaded
│   │   └── TextNormalizer.cs
│   ├── Voices/
│   │   ├── TTSVoice.cs           # ScriptableObject wrapping a style-embedding asset
│   │   └── (voice .bytes assets via samples/downloader)
│   └── Components/
│       ├── CharacterVoice.cs     # MonoBehaviour: assign voice, call Speak(text)
│       └── SpeechQueue.cs        # per-character queueing, interrupt/barge-in
├── Editor/
│   ├── LocalTTS.Editor.asmdef
│   ├── ModelDownloader.cs        # fetches ONNX + voices from Hugging Face, imports & quantizes
│   ├── VoicePreviewWindow.cs     # audition voices in-editor
│   └── Settings/                 # backend (CPU/GPU), quantization, default voice
├── Tests/
│   ├── Runtime/ (G2P golden tests, synthesis smoke tests)
│   └── Editor/
├── Samples~/
│   ├── BasicSpeech/              # one button, one line, one voice
│   ├── DialogueCharacters/       # two characters, distinct voices, queued conversation
│   └── LipSyncBridge/            # phoneme-timing → blendshape example
└── Documentation~/
```

### Key design decisions

1. **Models are NOT in the package.** 90–330 MB doesn't belong in a UPM package. The Editor
   `ModelDownloader` pulls the ONNX + voice files from Hugging Face
   (`onnx-community/Kokoro-82M-v1.0-ONNX`) into `Assets/` or `StreamingAssets/` on first use, with
   checksum verification and a pick-your-quantization UI (fp32 / fp16 / int8).
2. **Async, frame-budget-friendly inference.** Inference Engine lets you schedule a model
   layer-by-layer across frames — no hitches during gameplay. Public API is `async`
   (`Awaitable<SynthesisResult>`); long lines are split into sentences and synthesized in a
   pipeline so audio for sentence 1 plays while sentence 2 is still computing (perceived latency
   ≈ first-sentence latency).
3. **G2P is an interface**, not hardcoded — `IG2P` per language. English ships first; other
   languages plug in later without touching the engine.
4. **Lip-sync hooks from day one** (games need this): expose per-phoneme timing from the model's
   duration predictor so devs can drive visemes/blendshapes. Even a coarse
   phoneme→viseme map is a major differentiator over "here's an AudioClip".
5. **Main-thread discipline**: G2P + text normalization run on a background thread
   (pure C#); only worker dispatch and AudioClip creation touch the main thread.
6. **Write our own G2P implementation** (misaki-inspired, from the open CMUdict/misaki data which
   are permissively licensed). Do **not** copy code from Unity's sentis-samples without checking
   its license (Unity Companion License is not redistribution-friendly) — use it as a reference
   architecture only.

### Public API sketch

```csharp
// Setup (once)
var engine = await TTSEngine.CreateAsync(TTSSettings.Default); // loads model, warms up

// Simple
AudioClip clip = (await engine.SynthesizeAsync("Hello, traveler!", voice)).Clip;

// Game-character oriented
[SerializeField] TTSVoice voice;             // ScriptableObject, assigned in Inspector
var speaker = GetComponent<CharacterVoice>();
speaker.Speak("The cave is just ahead.");    // queues, plays on its AudioSource
speaker.Interrupt();                          // barge-in
speaker.OnPhoneme += viseme => ...;           // lip-sync
```

---

## 3. Development phases

### Phase 0 — Foundations (≈1 week)
- Repo layout: package under `Packages/com.<org>.localtts/` inside a Unity host project
  (standard "embedded package" dev workflow — works identically on macOS and Windows).
- Unity 6.x LTS baseline (Inference Engine 2.x requires Unity 6).
- `.gitattributes` (LF normalization — critical for mac/Windows collab), `.editorconfig`,
  Git LFS for any test audio fixtures. **No model weights in git.**
- CI skeleton: GitHub Actions with GameCI — editmode/playmode tests + package validation on both
  a Linux license runner and a Windows runner.

### Phase 1 — Proof of spike (≈1–2 weeks)
- Import Kokoro-82M ONNX into Inference Engine; verify the graph runs (CPU + GPU backends) on
  both macOS (Metal) and Windows (DX12).
- Hardcoded phoneme input → audio out → `AudioClip` playback. Golden-sample comparison against
  reference Python output.
- Measure: cold-load time, per-sentence latency, memory (fp32/fp16/int8), RTF on CPU vs GPU.
- **Exit criteria**: a Unity scene that speaks one hardcoded sentence on both OSes, < 1 s
  first-audio latency for a short sentence on desktop CPU.

### Phase 2 — English pipeline (≈2–3 weeks)
- `EnglishG2P`: lexicon (packed binary format, memory-mapped/lazy), text normalization
  (numbers, currency, dates, abbreviations, punctuation prosody), OOV fallback rules.
- Tokenizer → model input plumbing; sentence chunking; the async scheduling pipeline.
- G2P golden-test suite (hundreds of tricky cases: "Dr. Smith lives at 221B", "$1,499.99",
  "GPU", "read/read"…).

### Phase 3 — Package UX (≈2 weeks)
- `TTSVoice` ScriptableObjects + Editor voice-preview window.
- `ModelDownloader` editor flow (download, verify, quantize, import).
- `CharacterVoice` + `SpeechQueue` components; 3D spatialized playback via standard AudioSource.
- Samples: BasicSpeech, DialogueCharacters.
- Docs: quickstart, API reference, "how big is this at runtime" honesty table.

### Phase 4 — Hardening & performance (≈2 weeks)
- Quantization presets; pick a shipping default (likely fp16 GPU / int8 CPU).
- Frame-time capture in a real scene (no spikes > 2 ms from TTS on main thread).
- Memory lifecycle: engine dispose, voice hot-swap, domain-reload safety.
- IL2CPP builds on both OSes; platform matrix smoke tests (Windows, macOS, then Android/iOS
  as stretch — int8 model makes mobile plausible).
- Package validation (`Unity Package Validation Suite`), semantic versioning, CHANGELOG.

### Phase 5 — Release v1.0 (≈1 week)
- Distribution: git URL install first (`https://github.com/<org>/unity-tts.git`), then OpenUPM
  listing (scoped registry — the standard way game devs "fetch" packages).
- `Third Party Notices.md`: Kokoro (Apache-2.0), lexicon data sources, Inference Engine.
- Tag v1.0.0, README with audio demo clips / webGL or video demo.

### Phase 6 — Multilingual (post-1.0)
- Priority order by G2P difficulty: **ES/IT/PT/FR** (rule-friendly Latin scripts) → **JA/ZH**
  (need dictionary + segmentation; misaki has data to port) → others.
- Per-language G2P plug-ins as **optional samples/sub-packages** so the core stays lean.
- Evaluate **Supertonic 3 as a second backend** (31 languages, on-device ONNX, no espeak) —
  gated on OpenRAIL-M license review. The `IG2P`/backend abstraction from Phase 2 makes this
  a bolt-on, not a rewrite.
- Stretch: phoneme-timing → viseme sample (LipSyncBridge), voice-mixing (blend two style vectors).

---

## 4. Cross-platform (macOS + Windows) workflow

- **Line endings**: `.gitattributes` with `* text=auto eol=lf`; enforce via CI check.
- **Asset serialization**: Force Text + visible meta files (package default).
- **Inference backends differ** (Metal vs DX12): every perf-sensitive change gets measured on
  both; CI runs playmode synthesis smoke test on Windows runner, macOS covered by local
  pre-release checklist (GameCI macOS runners are paid/slow — revisit if budget allows).
- **No native plugins by design** — this is the main reason Inference Engine beats ONNX Runtime
  here; nothing to compile per-OS.

## 5. Risks & mitigations

| Risk | Mitigation |
|---|---|
| GPL contamination via espeak-ng | Never link/ship espeak-ng; pure C# G2P; audit all deps in Phase 0 |
| Kokoro ONNX has ops unsupported by Inference Engine | Phase 1 spike de-risks this first; fallback = export a compatible graph variant or (last resort) sherpa-onnx native path |
| 90–330 MB model too big for some games | int8 quantization; downloader lets dev choose; document budgets honestly |
| G2P quality gap vs espeak (OOV words, names) | Golden tests + rule fallback; games mostly ship known scripts — offer a pronunciation-override dictionary per project |
| Unity sample code license (Companion License) | Treat as reference only; clean-room our G2P from misaki *data* (Apache) + CMUdict |
| First-audio latency on long lines | Sentence chunking + pipelined synthesis; optional pre-synthesis ("bake at load") API for known dialogue |
| Voice licensing of Kokoro's 54 voices | Voices are distributed with the Apache-2.0 model release; keep provenance notes in Third Party Notices |

## 6. Success metrics (v1.0)

- Install-to-first-spoken-line < 10 minutes for a new user.
- First-audio latency < 500 ms for a 10-word line on a 4-core desktop CPU (int8).
- Zero main-thread frame spikes > 2 ms during synthesis.
- Runtime memory overhead < 400 MB fp16 / < 250 MB int8 including buffers.
- Works out-of-the-box: Windows, macOS (Editor + IL2CPP player), both CPU and GPU backends.
