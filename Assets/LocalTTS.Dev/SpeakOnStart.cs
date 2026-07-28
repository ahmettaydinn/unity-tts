using System.IO;
using LocalTTS;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// Dev scene driver: speaks plain English text through the full TTSEngine pipeline on
/// Start. Press Space to speak again. Host-project tool, not part of the package.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpeakOnStart : MonoBehaviour
{
    [SerializeField] private ModelAsset model;
    [SerializeField] private TTSBackend backend = TTSBackend.CpuBurst;

    [TextArea]
    [SerializeField] private string text =
        "Hello, traveler! The dragon's lair lies 40 miles north. It cost me $1,499.99 to find out.";

    private TTSEngine engine;
    private KokoroVoice voice;
    private AudioSource audioSource;

    private async void Start()
    {
        audioSource = GetComponent<AudioSource>();
        voice = new KokoroVoice("af_heart",
            File.ReadAllBytes(Path.Combine(Application.dataPath, "LocalTTS/Models/af_heart.bin")));
        engine = await TTSEngine.CreateAsync(model, new TTSSettings(backend), warmupVoice: voice);
        Speak();
    }

    private void Update()
    {
        if (engine != null && Input.GetKeyDown(KeyCode.Space))
        {
            Speak();
        }
    }

    private async void Speak()
    {
        float t = Time.realtimeSinceStartup;
        SynthesisResult result = await engine.SynthesizeAsync(text, voice);
        Debug.Log($"Synthesized {result.DurationSeconds:F2}s of audio in " +
                  $"{(Time.realtimeSinceStartup - t) * 1000f:F0} ms on {backend}.");
        audioSource.PlayOneShot(result.ToAudioClip());
    }

    private void OnDestroy() => engine?.Dispose();
}
