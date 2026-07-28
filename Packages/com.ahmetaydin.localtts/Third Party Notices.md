# Third Party Notices

This package contains or downloads third-party software components governed by
the license(s) indicated below.

## Kokoro-82M (model weights — downloaded at editor time, not distributed with this package)

- Source: https://huggingface.co/hexgrad/Kokoro-82M (ONNX export: https://huggingface.co/onnx-community/Kokoro-82M-v1.0-ONNX)
- License: Apache License 2.0
- Includes the released voice style embeddings distributed with the model.

## Misaki (G2P lexicon data)

- Source: https://github.com/hexgrad/misaki (us_gold.json + us_silver.json)
- License: Apache License 2.0
- Used as: data source for the packed English lexicon
  (`Runtime/Resources/LocalTTS/lexicon-en-us.bytes`). No Misaki code is included.
- Note: this package deliberately does NOT use or link espeak-ng (GPL-3.0).

## Unity Inference Engine (com.unity.ai.inference)

- Declared as a package dependency; distributed by Unity under the Unity Companion License.
