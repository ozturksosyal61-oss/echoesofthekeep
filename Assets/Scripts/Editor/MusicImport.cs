using UnityEditor;

namespace CoopGame.EditorTools
{
    /// <summary>Müzik döngüleri: mobilde bellek için akış (streaming) + Vorbis.</summary>
    public class MusicImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("ThirdParty/Music")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.loadType = UnityEngine.AudioClipLoadType.Streaming;
            s.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            ai.defaultSampleSettings = s;
        }
    }
}
