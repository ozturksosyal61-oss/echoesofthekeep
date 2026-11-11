using UnityEngine;

namespace CoopGame
{
    /// <summary>Meşale/fener titremesi (yalnızca görsel): ışık şiddetini ve alev ölçeğini gürültüyle oynatır.</summary>
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] Light target;
        [SerializeField] Transform flame;
        [SerializeField] float amount = 0.22f;
        [SerializeField] float speed = 6f;

        float baseIntensity, seed;
        Vector3 baseScale;

        void Awake()
        {
            seed = Random.value * 100f;
            if (target != null) baseIntensity = target.intensity;
            if (flame != null) baseScale = flame.localScale;
        }

        void Update()
        {
            float t = Time.time * speed;
            float n = Mathf.PerlinNoise(t, seed) * 0.7f + Mathf.PerlinNoise(t * 2.7f, seed + 9f) * 0.3f; // 0..1
            if (target != null) target.intensity = baseIntensity * (1f - amount + 2f * amount * n);
            if (flame != null) flame.localScale = new Vector3(baseScale.x * (0.92f + 0.16f * n), baseScale.y * (0.85f + 0.3f * n), baseScale.z * (0.92f + 0.16f * n));
        }
    }
}
