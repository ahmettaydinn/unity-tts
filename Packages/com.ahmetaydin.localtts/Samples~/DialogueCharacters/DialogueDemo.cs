using LocalTTS;
using UnityEngine;

namespace LocalTTS.Samples
{
    /// <summary>
    /// Scripted two-character conversation: each character speaks when the other
    /// finishes. Press I to interrupt whoever is talking (barge-in demo).
    /// </summary>
    public class DialogueDemo : MonoBehaviour
    {
        [SerializeField] private CharacterVoice guard;
        [SerializeField] private CharacterVoice traveler;

        private static readonly (bool guardSpeaks, string line)[] Script =
        {
            (true, "Halt! Who goes there?"),
            (false, "Just a humble traveler, seeking shelter for the night."),
            (true, "The inn is 200 yards down the road. It costs $5 a night."),
            (false, "Thank you, friend. The 1st round is on me!"),
        };

        private int index;

        private void Start()
        {
            guard.FinishedSpeaking += Advance;
            traveler.FinishedSpeaking += Advance;
            Advance();
        }

        private void Advance()
        {
            if (index >= Script.Length)
            {
                return;
            }

            var (guardSpeaks, line) = Script[index++];
            (guardSpeaks ? guard : traveler).Speak(line);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                guard.Interrupt();
                traveler.Interrupt();
                Debug.Log("Interrupted both characters.");
            }
        }
    }
}
