using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Oyuncu avatarı. Host-authoritative: girdi (PlayerInput) host'a gider, NetworkCharacterController hareketi simüle eder.
    /// Yerel oyuncuda client-side prediction, uzak oyuncuda snapshot enterpolasyonu (NCC.Render). Rol: giriş sırası.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class PlayerAvatar : NetworkBehaviour
    {
        static readonly int SpeedParam = Animator.StringToHash("Speed");

        VoiceNetworkObject voice;
        NetworkCharacterController cc;
        Animator animator;
        Vector3 lastPos;
        float smoothSpeed;

        public bool IsLocal => Object != null && Object.IsValid && Object.HasInputAuthority;
        public int RoleIndex => Roles.Index(Runner, Object.InputAuthority);

        /// <summary>Yerel oyuncu için mikrofon iletiyor mu, uzak oyuncu için ses çalıyor mu.</summary>
        public bool Speaking => voice != null && voice.Object != null && voice.Object.IsValid &&
                                (voice.IsLocal ? voice.IsRecording : voice.IsSpeaking);

        public override void Spawned()
        {
            voice = GetComponent<VoiceNetworkObject>();
            cc = GetComponent<NetworkCharacterController>();
            animator = GetComponentInChildren<Animator>();
            lastPos = transform.position;

            int role = RoleIndex;
            name = "Avatar_" + Roles.Name(role) + (IsLocal ? "_local" : "");
            var color = role == 0 ? new Color(0.25f, 0.55f, 1f) : new Color(1f, 0.55f, 0.2f);
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.gameObject.name != "Nose") r.material.color = color;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInput input))
                cc.Move(new Vector3(input.Move.x, 0f, input.Move.y));
        }

        public override void Render()
        {
            // Animasyon hızı: enterpole edilmiş görsel konumdan (hem yerel hem uzak oyuncu için aynı yol).
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var p = transform.position;
            var delta = p - lastPos;
            delta.y = 0f;
            lastPos = p;
            smoothSpeed = Mathf.Lerp(smoothSpeed, delta.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
            if (animator != null) animator.SetFloat(SpeedParam, smoothSpeed);
        }
    }
}
