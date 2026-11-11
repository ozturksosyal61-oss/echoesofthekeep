using UnityEngine;

namespace CoopGame
{
    /// <summary>Sabit hızda yerel eksen etrafında döner (dişliler; yalnızca görsel).</summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.forward;
        [SerializeField] float degreesPerSecond = 10f;

        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
