using System.IO;
using LocalTTS;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// Phase 1 spike scene driver: synthesizes a hardcoded line on Start and plays it.
/// Press Space to speak again. Host-project dev tool, not part of the package.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpeakOnStart : MonoBehaviour
{
    [SerializeField] private ModelAsset model;
    [SerializeField] private TTSBackend backend = TTSBackend.CpuBurst;

    [TextArea]
    [SerializeField] private string phonemes =
        "həlˈoʊ! ðɪs ɪz ɐ tˈɛst ʌv lˈoʊkəl spˈiːʧ, ɹˈʌnɪŋ ɪnsˈaɪd jˈuːnɪɾi.";

    private KokoroSynthesizer synthesizer;
    private KokoroVoice voice;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        voice = new KokoroVoice("af_heart",
            File.ReadAllBytes(Path.Combine(Application.dataPath, "LocalTTS/Models/af_heart.bin")));
        synthesizer = new KokoroSynthesizer(model, backend);
        Speak();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Speak();
        }
    }

    private void Speak()
    {
        float t = Time.realtimeSinceStartup;
        float[] samples = synthesizer.Synthesize(phonemes, voice);
        Debug.Log($"Synthesized {samples.Length / (float)TTSSettings.OutputSampleRate:F2}s " +
                  $"of audio in {(Time.realtimeSinceStartup - t) * 1000f:F0} ms on {backend}.");
        audioSource.PlayOneShot(KokoroSynthesizer.ToAudioClip(samples));
    }

    private void OnDestroy() => synthesizer?.Dispose();
}
