using System.IO;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

namespace LocalTTS.Editor
{
    /// <summary>
    /// Creates weight-quantized .sentis copies of the Float32 model. This is the
    /// supported size-reduction path: the pre-quantized ONNX variants on Hugging Face
    /// use operators Inference Engine cannot import (MatMulInteger, ConvInteger,
    /// DynamicQuantizeLSTM…), and fp16 ONNX is pathologically slow. Weight-only
    /// quantization keeps compute in fp32 — same speed, quarter/half the disk size.
    /// </summary>
    public static class ModelQuantizerUtil
    {
        public static string QuantizedAssetPath(QuantizationType type) =>
            $"{ModelPaths.ModelAssetFolder}/kokoro-v1.0-weights-{type.ToString().ToLowerInvariant()}.sentis";

        /// <summary>Quantizes the fp32 model's weights and imports the .sentis copy.</summary>
        public static ModelAsset CreateQuantizedCopy(ModelAsset float32Asset, QuantizationType type)
        {
            string assetPath = QuantizedAssetPath(type);
            Model model = ModelLoader.Load(float32Asset);
            ModelQuantizer.QuantizeWeights(type, ref model);
            ModelWriter.Save(Path.GetFullPath(assetPath), model);
            AssetDatabase.ImportAsset(assetPath);

            var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(assetPath);
            long size = new FileInfo(Path.GetFullPath(assetPath)).Length;
            Debug.Log($"LocalTTS: wrote {assetPath} ({size / (1024 * 1024)} MB, {type} weights).");
            return asset;
        }
    }
}
