# Dialogue Characters sample

Two characters with distinct voices holding a queued conversation, including barge-in
(interrupting mid-line).

## Setup

1. **LocalTTS → Model Manager**: download a model and two different voices
   (e.g. *Heart (US female)* and *Michael (US male)*).
2. Create an empty GameObject with **TTS Engine Provider** + the model asset assigned.
3. Create two GameObjects, each with an **AudioSource** + **Character Voice**
   (assign a different TTSVoice to each).
4. Add `DialogueDemo` from this sample to any object and drag both CharacterVoices in.
5. Enter Play mode: the conversation plays line by line. Press **I** to demonstrate
   interruption.

## What to look at

- `CharacterVoice.Speak()` queues lines per character; the shared engine serializes
  synthesis FIFO across characters automatically.
- `LineStarted` / `FinishedSpeaking` events drive the turn-taking — the same hooks you
  would use for subtitles or animation triggers.
