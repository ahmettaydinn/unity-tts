using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>
    /// Gives a game character a voice. Queues lines, synthesizes them through the shared
    /// engine, and plays them on this object's AudioSource — so 3D spatialization,
    /// mixer routing, and volume all behave like any other game audio.
    /// </summary>
    [AddComponentMenu("LocalTTS/Character Voice")]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CharacterVoice : MonoBehaviour
    {
        [SerializeField] private TTSVoice voice;

        [Tooltip("Speech rate multiplier. 1 = normal.")]
        [SerializeField, Range(0.5f, 2f)] private float speed = 1f;

        private AudioSource source;
        private readonly Queue<string> pending = new Queue<string>();
        private bool pumping;
        private int generation; // bumped by Interrupt() to discard in-flight synthesis

        /// <summary>Raised when a queued line starts playing.</summary>
        public event Action<string> LineStarted;

        /// <summary>Raised when the queue empties and playback finishes.</summary>
        public event Action FinishedSpeaking;

        public TTSVoice Voice { get => voice; set => voice = value; }
        public bool IsSpeaking => pumping || (source != null && source.isPlaying);
        public int QueuedLines => pending.Count;

        private void Awake() => source = GetComponent<AudioSource>();

        /// <summary>Queues a line; lines play in order.</summary>
        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (voice == null || !voice.IsValid)
            {
                Debug.LogError("CharacterVoice: no valid TTSVoice assigned.", this);
                return;
            }

            pending.Enqueue(text);
            if (!pumping)
            {
                _ = PumpAsync();
            }
        }

        /// <summary>Stops the current line immediately and clears the queue (barge-in).</summary>
        public void Interrupt()
        {
            generation++;
            pending.Clear();
            if (source != null)
            {
                source.Stop();
            }
        }

        private async Awaitable PumpAsync()
        {
            pumping = true;
            try
            {
                while (pending.Count > 0)
                {
                    string line = pending.Dequeue();
                    int myGeneration = generation;

                    TTSEngine engine = await TTSEngineProvider.GetSharedEngineAsync();
                    SynthesisResult result = await engine.SynthesizeAsync(line, voice.Voice, speed);

                    if (myGeneration != generation || destroyCancellationToken.IsCancellationRequested)
                    {
                        continue; // interrupted while synthesizing — drop the audio
                    }

                    LineStarted?.Invoke(line);
                    AudioClip clip = result.ToAudioClip($"tts_{name}");
                    source.clip = clip;
                    source.Play();

                    while (source != null && source.isPlaying && myGeneration == generation)
                    {
                        await Awaitable.NextFrameAsync(destroyCancellationToken);
                    }

                    Destroy(clip); // procedural clips are not pooled; free the samples
                }
            }
            catch (OperationCanceledException)
            {
                // Object destroyed mid-line; nothing to clean up beyond the finally.
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                pumping = false;
                FinishedSpeaking?.Invoke();
            }
        }

        private void OnDestroy() => Interrupt();
    }
}
