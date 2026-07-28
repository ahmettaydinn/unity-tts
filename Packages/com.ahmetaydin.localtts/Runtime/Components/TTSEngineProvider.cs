using System;
using Unity.InferenceEngine;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>
    /// Scene-level owner of the shared <see cref="TTSEngine"/>. Add one to a bootstrap
    /// object, assign the model asset, and every <see cref="CharacterVoice"/> in the
    /// scene shares its engine. Creation starts on Awake so warmup overlaps loading.
    /// </summary>
    [AddComponentMenu("LocalTTS/TTS Engine Provider")]
    public sealed class TTSEngineProvider : MonoBehaviour
    {
        [SerializeField] private ModelAsset model;
        [SerializeField] private TTSSettings settings = new TTSSettings();

        [Tooltip("Optional: voice used for the warmup synthesis that absorbs GPU shader compilation.")]
        [SerializeField] private TTSVoice warmupVoice;

        private static TTSEngineProvider instance;
        private static TTSEngine sharedEngine;
        private static bool creating;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("LocalTTS: multiple TTSEngineProviders in scene; using the first.");
                return;
            }

            instance = this;
            _ = WarmupAsync();
        }

        private async Awaitable WarmupAsync()
        {
            try
            {
                await GetSharedEngineAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>The shared engine, creating it on first use.</summary>
        public static async Awaitable<TTSEngine> GetSharedEngineAsync()
        {
            if (sharedEngine != null)
            {
                return sharedEngine;
            }

            if (creating)
            {
                while (sharedEngine == null && creating)
                {
                    await Awaitable.NextFrameAsync();
                }

                if (sharedEngine == null)
                {
                    throw new InvalidOperationException("LocalTTS: engine creation failed (see earlier errors).");
                }

                return sharedEngine;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<TTSEngineProvider>();
            }

            if (instance == null || instance.model == null)
            {
                throw new InvalidOperationException(
                    "LocalTTS: no TTSEngineProvider with an assigned model asset found in the scene.");
            }

            creating = true;
            try
            {
                sharedEngine = await TTSEngine.CreateAsync(
                    instance.model, instance.settings,
                    instance.warmupVoice != null ? instance.warmupVoice.Voice : null);
                return sharedEngine;
            }
            finally
            {
                creating = false;
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            instance = null;
            sharedEngine?.Dispose();
            sharedEngine = null;
        }
    }
}
