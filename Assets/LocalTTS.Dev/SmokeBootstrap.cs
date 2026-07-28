using System;
using System.IO;
using LocalTTS;
using UnityEngine;

/// <summary>
/// Player-build smoke test: synthesizes through the shared engine and the
/// CharacterVoice component, writes a result file, and quits. Driven by
/// BuildSmoke.cs; result directory comes from LOCALTTS_SMOKE_OUT.
/// </summary>
public class SmokeBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterVoice character;

    private async void Start()
    {
        string outDir = Environment.GetEnvironmentVariable("LOCALTTS_SMOKE_OUT")
                        ?? Application.persistentDataPath;
        string resultPath = Path.Combine(outDir, "smoke_result.txt");

        try
        {
            // Diagnostics: is the provider there, and did its model reference survive
            // scene serialization into the player?
            var provider = FindFirstObjectByType<TTSEngineProvider>();
            var modelField = provider == null ? null : typeof(TTSEngineProvider)
                .GetField("model", System.Reflection.BindingFlags.Instance |
                                   System.Reflection.BindingFlags.NonPublic)!
                .GetValue(provider);
            File.WriteAllText(Path.Combine(outDir, "smoke_diag.txt"),
                $"provider={(provider != null)} model={(modelField as UnityEngine.Object) != null} " +
                $"voice={(character.Voice != null && character.Voice.IsValid)}");

            // 1) Direct engine synthesis — verifies model + lexicon inside the build.
            TTSEngine engine = await TTSEngineProvider.GetSharedEngineAsync();
            SynthesisResult result = await engine.SynthesizeAsync(
                "Player build smoke test. All systems are one hundred percent operational.",
                character.Voice.Voice);

            double rms = 0;
            foreach (float s in result.Samples) rms += s * s;
            rms = Math.Sqrt(rms / result.Samples.Length);
            if (result.DurationSeconds < 2f || rms < 1e-3)
            {
                throw new InvalidOperationException(
                    $"suspicious audio: {result.DurationSeconds:F2}s RMS {rms:F5}");
            }

            // 2) Component path — CharacterVoice queue + events in a player.
            bool finished = false;
            character.FinishedSpeaking += () => finished = true;
            character.Speak("Component check.");
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!finished && Time.realtimeSinceStartup < deadline)
            {
                await Awaitable.NextFrameAsync();
            }

            File.WriteAllText(resultPath, finished
                ? $"OK {result.DurationSeconds:F2}s RMS {rms:F4}"
                : "TIMEOUT waiting for CharacterVoice");
        }
        catch (Exception e)
        {
            File.WriteAllText(resultPath, $"FAIL: {e}");
        }

        Application.Quit();
    }
}
