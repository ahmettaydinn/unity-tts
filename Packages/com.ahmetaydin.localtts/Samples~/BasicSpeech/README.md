# Basic Speech sample

Speak any line through a character in three steps.

## Setup

1. **LocalTTS → Model Manager**: download a model variant (Float32 recommended) and at
   least one voice (e.g. *Heart (US female)*).
2. Create an empty GameObject, add **TTS Engine Provider**, and assign the downloaded
   model asset (`Assets/LocalTTS/Models/…onnx`).
3. Add the `SpeakOnClick` component from this sample to any object with an
   **AudioSource**, assign a **TTSVoice** asset, enter Play mode, and click the object
   (or press **Space**).

## Notes

- The AudioSource's *Spatial Blend* controls 2D/3D voice positioning — TTS audio is
  ordinary game audio.
- First speech after startup includes engine warmup unless the provider has a warmup
  voice assigned.
