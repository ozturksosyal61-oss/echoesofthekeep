using UnityEngine;

namespace CoopGame
{
    /// <summary>Menü kamerası: yavaş süzülme (yalnızca görsel).</summary>
    public class CameraDrift : MonoBehaviour
    {
        [SerializeField] Vector3 moveAmount = new Vector3(0.5f, 0.12f, 0.3f);
        [SerializeField] Vector3 lookAmount = new Vector3(1.2f, 2f, 0f);
        [SerializeField] float period = 22f;

        Vector3 pos0;
        Quaternion rot0;

        void Awake() { pos0 = transform.position; rot0 = transform.rotation; }

        void Update()
        {
            float t = Time.time * 6.2832f / period;
            transform.position = pos0 + new Vector3(Mathf.Sin(t) * moveAmount.x, Mathf.Sin(t * 1.7f) * moveAmount.y, Mathf.Cos(t * 0.8f) * moveAmount.z);
            transform.rotation = rot0 * Quaternion.Euler(Mathf.Sin(t * 1.3f) * lookAmount.x, Mathf.Sin(t * 0.9f) * lookAmount.y, 0f);
        }
    }
}
