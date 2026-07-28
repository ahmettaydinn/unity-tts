using System;
using System.IO;
using LocalTTS;
using LocalTTS.Editor;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds a standalone player containing the smoke-test scene (uint8-weights model,
/// full component stack). Headless: -executeMethod BuildSmoke.Build
/// </summary>
public static class BuildSmoke
{
    private const string ScenePath = "Assets/LocalTTS.Dev/SmokeScene.unity";
    private const string VoiceBytesPath = "Assets/LocalTTS/Voices/af_heart.bytes";
    private const string VoiceAssetPath = "Assets/LocalTTS/Voices/af_heart.asset";

    public static void Build()
    {
        try
        {
            string output = Environment.GetEnvironmentVariable("LOCALTTS_BUILD_OUT")
                            ?? "Builds/Smoke.app";

            // LOCALTTS_SMOKE_MODEL=fp32 builds with the ONNX asset; default is the
            // recommended uint8-weights .sentis (A/B for build-serialization issues).
            ModelAsset model;
            if (Environment.GetEnvironmentVariable("LOCALTTS_SMOKE_MODEL") == "fp32")
            {
                model = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
            }
            else
            {
                model = AssetDatabase.LoadAssetAtPath<ModelAsset>(
                    ModelQuantizerUtil.QuantizedAssetPath(QuantizationType.Uint8));
                if (model == null)
                {
                    var fp32 = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
                    model = ModelQuantizerUtil.CreateQuantizedCopy(fp32, QuantizationType.Uint8);
                }
            }

            if (model == null)
            {
                throw new InvalidOperationException("smoke build: model asset not found");
            }

            TTSVoice voice = EnsureVoiceAsset();
            CreateScene(model, voice);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[smoke-build] {report.summary.result}, " +
                      $"size {report.summary.totalSize / (1024 * 1024)} MB, " +
                      $"errors {report.summary.totalErrors} → {output}");
            EditorApplication.Exit(ok ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static TTSVoice EnsureVoiceAsset()
    {
        Directory.CreateDirectory(Path.GetFullPath("Assets/LocalTTS/Voices"));
        if (!File.Exists(Path.GetFullPath(VoiceBytesPath)))
        {
            File.Copy(Path.GetFullPath(SpikeModelDownloader.VoiceAssetPath),
                Path.GetFullPath(VoiceBytesPath));
            AssetDatabase.ImportAsset(VoiceBytesPath);
        }

        var voice = AssetDatabase.LoadAssetAtPath<TTSVoice>(VoiceAssetPath);
        if (voice == null)
        {
            voice = ScriptableObject.CreateInstance<TTSVoice>();
            voice.Initialize("Heart (US female)",
                AssetDatabase.LoadAssetAtPath<TextAsset>(VoiceBytesPath));
            AssetDatabase.CreateAsset(voice, VoiceAssetPath);
            AssetDatabase.SaveAssets();
        }

        return voice;
    }

    private static void CreateScene(ModelAsset model, TTSVoice voice)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // NOTE: SerializedObject.objectReferenceValue silently drops asset references
        // in -batchmode (verified on 6000.5.5f1); assign fields via reflection instead.
        var providerGo = new GameObject("Provider");
        var provider = providerGo.AddComponent<TTSEngineProvider>();
        SetField(provider, "model", model);
        SetField(provider, "warmupVoice", voice);

        var characterGo = new GameObject("Character");
        characterGo.AddComponent<AudioSource>();
        var character = characterGo.AddComponent<CharacterVoice>();
        SetField(character, "voice", voice);

        var bootGo = new GameObject("Smoke");
        var boot = bootGo.AddComponent<SmokeBootstrap>();
        SetField(boot, "character", character);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void SetField(Component target, string field, object value)
    {
        target.GetType().GetField(field, System.Reflection.BindingFlags.Instance |
                                         System.Reflection.BindingFlags.NonPublic)!
            .SetValue(target, value);
        EditorUtility.SetDirty(target);
    }
}
