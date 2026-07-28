# unity-tts

**Fully local, on-device text-to-speech for Unity game characters** —
[Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) running on Unity Inference
Engine. No cloud, no API keys, license-clean for commercial games.

🎧 **Hear it** (synthesized entirely inside Unity, CPU backend):
[dialogue demo](media/demo-dialogue.wav) · [pangram demo](media/demo-pangram.wav)

> *"Dr. Smith paid $1,499.99 for the 3rd GPU! Was it worth it? The dragon's lair
> lies 40 miles north. Good luck, traveler."* — plain text in, that audio out.

## The package

Lives in [`Packages/com.ahmetaydin.localtts/`](Packages/com.ahmetaydin.localtts/) —
see its [README](Packages/com.ahmetaydin.localtts/README.md) for features and
quickstart, and [Documentation~](Packages/com.ahmetaydin.localtts/Documentation~/index.md)
for the full docs. Install into any Unity 6 project via Package Manager git URL:

```
https://github.com/ahmettaydinn/unity-tts.git?path=/Packages/com.ahmetaydin.localtts
```

**Highlights**: 28 voices as drag-and-drop assets · per-character speech queues with
barge-in · automatic text normalization ($1,499.99 → words) · 108 MB shippable model ·
CPU ~7× / GPU ~13× faster than real time · MIT + Apache-2.0, no GPL.

## This repository

The repo root is the Unity host project used to develop and test the embedded package
([PLAN.md](PLAN.md) has the roadmap; phases 0–5 complete).

### Working on it

1. Unity 6 (6000.x) via Unity Hub; open the repo root as a project.
2. `git lfs install` once per machine.
3. **LocalTTS → Model Manager** to fetch the model (weights are never committed).
4. Tests: **Window → General → Test Runner** (or CI headless — see
   [.github/workflows/ci.yml](.github/workflows/ci.yml)).
5. Dev utilities live under the **LocalTTS → Spike** menu: benchmark matrix,
   pipeline runners, smoke-test player build.

### CI

Repo-hygiene checks run on every push. Unity test jobs activate when license secrets
are configured ([GameCI activation](https://game.ci/docs/github/activation)) —
currently pending GameCI support for Unity's new entitlement licensing.
