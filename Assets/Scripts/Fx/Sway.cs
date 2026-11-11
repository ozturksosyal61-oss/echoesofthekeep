using UnityEngine;

namespace CoopGame
{
    /// <summary>Sinüsle hafif salınım (çan, asılı fener; yalnızca görsel).</summary>
    public class Sway : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.forward;
        [SerializeField] float degrees = 2f;
        [SerializeField] float period = 5f;

        Quaternion rest;
        float phase;

        void Awake() { rest = transform.localRotation; phase = Random.value * 6.28f; }
        void Update() => transform.localRotation = rest * Quaternion.AngleAxis(Mathf.Sin(Time.time * 6.2832f / period + phase) * degrees, axis);
    }
}
