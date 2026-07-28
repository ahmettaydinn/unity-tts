# unity-tts

Fully local, on-device text-to-speech for Unity game characters —
[Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) on Unity Inference Engine.

- **The package** lives in [`Packages/com.ahmetaydin.localtts/`](Packages/com.ahmetaydin.localtts/)
  (see its [README](Packages/com.ahmetaydin.localtts/README.md)).
- **The roadmap** is in [PLAN.md](PLAN.md). Current milestone: Phase 1 (ONNX inference spike).
- The repo root is the Unity host project used to develop and test the embedded package.

## Working on this repo

1. Install **Unity 6 LTS (6000.0.x)** via Unity Hub, then open the repo root as a project.
   If Hub offers a slightly newer 6000.0 patch than `ProjectSettings/ProjectVersion.txt`,
   accepting the upgrade is fine.
2. Install Git LFS once per machine (audio test fixtures): `brew install git-lfs` /
   `winget install GitHub.GitLFS`, then `git lfs install`.
3. Package tests appear in **Window ▸ General ▸ Test Runner** (both EditMode and PlayMode).

Model weights are never committed — they are fetched by the editor ModelDownloader (Phase 3).

## CI

GitHub Actions runs repo hygiene checks on every push. Unity test jobs (GameCI) activate once
Unity license secrets are added to the repo — see
[game.ci/docs/github/activation](https://game.ci/docs/github/activation), then set
`UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD`) in repo settings.
