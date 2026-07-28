using LocalTTS.Editor;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the Phase 1 spike scene with a wired-up SpeakOnStart object.</summary>
public static class CreateSpikeScene
{
    private const string ScenePath = "Assets/LocalTTS.Dev/SpikeScene.unity";

    [MenuItem("LocalTTS/Spike/Create Spike Scene")]
    public static void Create()
    {
        var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(SpikeModelDownloader.ModelAssetPath);
        if (model == null)
        {
            Debug.LogError("Download the spike model first (LocalTTS/Spike/Download Model + Voice).");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var speaker = new GameObject("Speaker");
        speaker.AddComponent<AudioSource>();
        var speak = speaker.AddComponent<SpeakOnStart>();

        // Reflection, not SerializedObject: the latter drops asset refs in -batchmode.
        typeof(SpeakOnStart)
            .GetField("model", System.Reflection.BindingFlags.Instance |
                               System.Reflection.BindingFlags.NonPublic)!
            .SetValue(speak, model);
        UnityEditor.EditorUtility.SetDirty(speak);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Spike scene saved to {ScenePath}. Open it and press Play.");
    }
}
