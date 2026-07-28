using LocalTTS.Kokoro;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>
    /// A voice as an assignable asset: wraps the 510×256 Kokoro style-embedding table.
    /// Created automatically by the Model Manager when downloading voices, or manually
    /// via Assets → Create → LocalTTS → Voice with a .bytes style file.
    /// </summary>
    [CreateAssetMenu(fileName = "New Voice", menuName = "LocalTTS/Voice")]
    public sealed class TTSVoice : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private TextAsset styleData;

        private KokoroVoice runtime;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>The runtime voice; parsed once and cached.</summary>
        public KokoroVoice Voice => runtime ??= new KokoroVoice(name, styleData.bytes);

        public bool IsValid => runtime != null || (styleData != null
            && styleData.bytes.Length == KokoroVoice.Rows * KokoroVoice.StyleDim * sizeof(float));

#if UNITY_EDITOR
        /// <summary>Editor-only initializer used by the Model Manager.</summary>
        public void Initialize(string display, TextAsset data)
        {
            displayName = display;
            styleData = data;
            runtime = null;
        }
#endif
    }
}
