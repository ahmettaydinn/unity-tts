using LocalTTS;
using UnityEngine;

namespace LocalTTS.Samples
{
    /// <summary>Minimal LocalTTS usage: one component, one line, one keypress.</summary>
    [RequireComponent(typeof(CharacterVoice))]
    public class SpeakOnClick : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string line = "Hello, traveler! Welcome to the village.";

        private CharacterVoice character;

        private void Awake() => character = GetComponent<CharacterVoice>();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                character.Speak(line);
            }
        }

        private void OnMouseDown() => character.Speak(line);
    }
}
