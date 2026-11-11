using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bir bulmaca çözülünce iki oyuncuda da onay sesi (yükselen üç çan notası). Ses çalışma zamanında sentezlenir (dosya yok).
    /// PuzzleChanged her iki tarafta Render'dan gelir; geç katılan oyuncu eski çözümler için ses duymaz (ilk saniyeler yok sayılır).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PuzzleChime : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource source;
        AudioClip clip;
        float readyAt = -1f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.8f;
            clip = Build();
        }

        void OnEnable() => PuzzleManager.PuzzleChanged += OnPuzzle;
        void OnDisable() => PuzzleManager.PuzzleChanged -= OnPuzzle;

        void Update()
        {
            if (readyAt < 0f && PuzzleManager.Instance != null) readyAt = Time.time + 1.5f;
        }

        void OnPuzzle(int id, PuzzleState old, PuzzleState now)
        {
            if (now != PuzzleState.Solved || readyAt < 0f || Time.time < readyAt) return;
            source.PlayOneShot(clip);
        }

        static AudioClip Build()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // Do5 Mi5 Sol5 Do6
            float[] starts = { 0f, 0.16f, 0.32f, 0.48f };
            float len = 2.6f;
            int n = (int)(Rate * len);
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int s0 = (int)(starts[k] * Rate);
                float f = notes[k], amp = k == notes.Length - 1 ? 0.32f : 0.24f;
                for (int i = s0; i < n; i++)
                {
                    float t = (i - s0) / (float)Rate;
                    float env = Mathf.Exp(-t * 2.2f) * Mathf.Clamp01(t * 400f);
                    // çan tınısı: temel + uyumsuz kısmi tonlar (hızlı söner)
                    float v = Mathf.Sin(2f * Mathf.PI * f * t)
                            + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 4f)
                            + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t * 7f);
                    data[i] += v * env * amp;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var c = AudioClip.Create("PuzzleChime", n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
