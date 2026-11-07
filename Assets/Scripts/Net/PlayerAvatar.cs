using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Oyuncu avatarı. Host-authoritative: girdi (PlayerInput) host'a gider, NetworkCharacterController hareketi simüle eder.
    /// Yerelde tahmin, uzakta snapshot enterpolasyonu. Rol: giriş sırası. Alan kuralı: uzak avatar yalnızca aynı odadaysa görünür
    /// (renderer yerel kapanır; ağ durumu ve ses etkilenmez).
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class PlayerAvatar : NetworkBehaviour
    {
        static readonly int SpeedParam = Animator.StringToHash("Speed");

        public static PlayerAvatar Local { get; private set; }

        VoiceNetworkObject voice;
        NetworkCharacterController cc;
        Animator animator;
        Renderer[] renderers;
        bool remoteVisible = true;
        Vector3 lastPos;
        float smoothSpeed;

        public bool IsLocal => Object != null && Object.IsValid && Object.HasInputAuthority;
        public int RoleIndex => Roles.Index(Runner, Object.InputAuthority);
        public int Room => Layout.RoomOf(transform.position);

        /// <summary>Yerel oyuncu için mikrofon iletiyor mu, uzak oyuncu için ses çalıyor mu.</summary>
        public bool Speaking => voice != null && voice.Object != null && voice.Object.IsValid &&
                                (voice.IsLocal ? voice.IsRecording : voice.IsSpeaking);

        public override void Spawned()
        {
            voice = GetComponent<VoiceNetworkObject>();
            cc = GetComponent<NetworkCharacterController>();
            animator = GetComponentInChildren<Animator>();
            renderers = GetComponentsInChildren<Renderer>(true);
            lastPos = transform.position;
            if (IsLocal) Local = this;

            int role = RoleIndex;
            name = "Avatar_" + Roles.Name(role) + (IsLocal ? "_local" : "");
            var color = role == 0 ? new Color(0.25f, 0.55f, 1f) : new Color(1f, 0.55f, 0.2f);
            foreach (var r in renderers)
                if (r.gameObject.name != "Nose") r.material.color = color;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Local == this) Local = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInput input))
                cc.Move(new Vector3(input.Move.x, 0f, input.Move.y));
        }

        public override void Render()
        {
            // Animasyon hızı: enterpole edilmiş görsel konumdan.
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var p = transform.position;
            var delta = p - lastPos;
            delta.y = 0f;
            lastPos = p;
            smoothSpeed = Mathf.Lerp(smoothSpeed, delta.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
            if (animator != null) animator.SetFloat(SpeedParam, smoothSpeed);

            // Alan kuralı: yalnızca yerel görünürlük. Yerel avatarı FollowCamera yönetir.
            if (!IsLocal)
            {
                bool visible = Local != null && Room == Local.Room;
                if (visible != remoteVisible)
                {
                    remoteVisible = visible;
                    foreach (var r in renderers) if (r != null) r.enabled = visible;
                }
            }
        }

        // ---- Etkileşim: istemci → host (RPC), host doğrular, RoomState (state authority host) günceller ----

        public void RequestInteract(InteractId id) => RPC_Interact((int)id);

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        void RPC_Interact(int id)
        {
            var target = Interactable.Find((InteractId)id);
            var state = RoomState.Instance;
            if (target == null || state == null) return;
            // Host doğrulaması: aynı odada ve yeterince yakın mı.
            if (Layout.RoomOf(target.transform.position) != Room) return;
            if (Vector3.Distance(transform.position, target.transform.position) > Interactable.ServerRange) return;
            state.Toggle((InteractId)id);
        }
    }
}
