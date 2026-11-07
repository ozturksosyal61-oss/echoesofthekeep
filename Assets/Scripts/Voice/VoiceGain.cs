using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Konuşma kaynağına %100 üstü kazanç verir (AudioSource.volume en fazla 1 olabilir). Ses iş parçacığında çalışır;
    /// kazanç ana iş parçacığında atanır. Taşmayı yumuşak sınırlar (sert kırpma çatırtı yapar).
    /// </summary>
    [DisallowMultipleComponent]
    public class VoiceGain : MonoBehaviour
    {
        volatile float gain = 1f;

        public float Gain { get => gain; set => gain = Mathf.Max(0f, value); }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float g = gain;
            if (g <= 1.001f) return;
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i] * g;
                // 0,8'e kadar doğrusal, üstü tanh benzeri yumuşak sınır
                float a = x < 0f ? -x : x;
                if (a > 0.8f) x = (x < 0f ? -1f : 1f) * (0.8f + 0.2f * (float)System.Math.Tanh((a - 0.8f) / 0.2f));
                data[i] = x;
            }
        }
    }
}
