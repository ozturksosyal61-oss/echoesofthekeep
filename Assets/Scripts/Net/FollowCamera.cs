using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Kamera: üçüncü şahıs takip ve birinci şahıs (göz hizası). Yalnızca yerel görüntüyü etkiler; ağ senkronuyla ilgisi yok
    /// (avatarın renderer'ları yalnızca bu cihazda gizlenir, Fusion state'i değişmez).
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public static float Yaw;
        public static FollowCamera Instance { get; private set; }
        public bool FirstPerson { get; private set; }

        [SerializeField] float distance = 6f;
        [SerializeField] float height = 1.6f;
        [SerializeField] float eyeHeight = 1.65f;
        [SerializeField] float sensitivity = 0.15f;

        float pitch = 25f;
        PlayerAvatar target;
        Renderer[] targetRenderers;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Toggle()
        {
            FirstPerson = !FirstPerson;
            pitch = FirstPerson ? 0f : 25f;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (targetRenderers == null) return;
            foreach (var r in targetRenderers)
                if (r != null) r.enabled = !FirstPerson;
        }

        void LateUpdate()
        {
            var look = InputSource.TakeLook();
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(1))
                look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 6f;
#endif
            Yaw += look.x * sensitivity;
            pitch = FirstPerson ? Mathf.Clamp(pitch - look.y * sensitivity, -80f, 80f)
                                : Mathf.Clamp(pitch - look.y * sensitivity, -5f, 70f);

            if (target == null || target.Object == null || !target.Object.IsValid)
            {
                target = null;
                targetRenderers = null;
                foreach (var a in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                    if (a.IsLocal) { target = a; break; }
                if (target == null) return;
                targetRenderers = target.GetComponentsInChildren<Renderer>(true);
                ApplyVisibility();
            }

            var rot = Quaternion.Euler(pitch, Yaw, 0f);
            transform.rotation = rot;

            if (FirstPerson)
            {
                transform.position = target.transform.position + Vector3.up * eyeHeight;
                return;
            }

            var focus = target.transform.position + Vector3.up * height;
            var back = rot * Vector3.back;
            float d = distance;
            if (Physics.SphereCast(focus, 0.25f, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.6f, hit.distance);

            var desired = focus + back * d;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-25f * Time.deltaTime));
        }
    }
}
