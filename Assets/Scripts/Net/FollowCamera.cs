using UnityEngine;

namespace CoopGame
{
    /// <summary>Üçüncü şahıs takip kamerası. Bakış girdisi (sürükleme / sağ fare) ile döner; engelde yaklaşır.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public static float Yaw;

        [SerializeField] float distance = 6f;
        [SerializeField] float height = 1.6f;
        [SerializeField] float sensitivity = 0.15f;

        float pitch = 25f;
        PlayerAvatar target;

        void LateUpdate()
        {
            var look = InputSource.TakeLook();
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(1))
                look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 6f;
#endif
            Yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -5f, 70f);

            if (target == null || target.Object == null || !target.Object.IsValid)
            {
                target = null;
                foreach (var a in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                    if (a.IsLocal) { target = a; break; }
                if (target == null) return;
            }

            var focus = target.transform.position + Vector3.up * height;
            var rot = Quaternion.Euler(pitch, Yaw, 0f);
            var back = rot * Vector3.back;
            float d = distance;
            if (Physics.SphereCast(focus, 0.25f, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.6f, hit.distance);

            var desired = focus + back * d;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-25f * Time.deltaTime));
            transform.rotation = rot;
        }
    }
}
