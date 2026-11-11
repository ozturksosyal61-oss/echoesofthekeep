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
        /// <summary>Spawn edilmiş tüm avatarlar (host'ta basınç plakası vb. için).</summary>
        public static readonly System.Collections.Generic.List<PlayerAvatar> All = new System.Collections.Generic.List<PlayerAvatar>();

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
            if (!All.Contains(this)) All.Add(this);

            int role = RoleIndex;
            name = "Avatar_" + Roles.Name(role) + (IsLocal ? "_local" : "");
            var color = role == 0 ? new Color(0.25f, 0.55f, 1f) : new Color(1f, 0.55f, 0.2f);
            foreach (var r in renderers)
                if (r.gameObject.name != "Nose") r.material.color = color;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Local == this) Local = null;
            All.Remove(this);
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

            // Alan kuralı + 1. şahıs: yalnızca yerel görünürlük. Yerel avatarı FollowCamera yönetir.
            if (!IsLocal)
            {
                bool visible = Local != null && Room == Local.Room && !FollowCamera.HidingRemotes;
                if (visible != remoteVisible)
                {
                    remoteVisible = visible;
                    foreach (var r in renderers) if (r != null) r.enabled = visible;
                }
            }
        }

        // ---- Etkileşim: istemci → host (RPC), host doğrular, RoomState (state authority host) günceller ----

        public void RequestInteract(int id)
        {
            // (2a) RPC gönderilmeden önce: yetki durumu
            Dbg.Interact("(2a) RPC gönderiliyor id=" + id + " avatar=" + name +
                         " HasInputAuthority=" + Object.HasInputAuthority + " HasStateAuthority=" + Object.HasStateAuthority +
                         " inputAuth=p" + Object.InputAuthority.PlayerId + " stateAuth=p" + Object.StateAuthority.PlayerId +
                         " RoomState.Instance=" + (RoomState.Instance != null));
            RPC_Interact(id);
            Dbg.Interact("(2b) RPC_Interact çağrısı döndü (host'a iletilmiş olmalı)");
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        void RPC_Interact(int id)
        {
            // (3a) Host RPC'yi aldı mı
            var target = Interactable.Find(id);
            var state = RoomState.Instance;
            Dbg.Interact("(3a) RPC ALINDI id=" + id + " gönderen=p" + Object.InputAuthority.PlayerId +
                         " avatarKonum=" + transform.position.ToString("F1") + " oda=" + Room +
                         " target=" + (target != null ? target.name : "YOK") + " state=" + (state != null));
            if (target == null) { Dbg.Interact("(3a) REDDEDİLDİ: target yok"); return; }
            // Host doğrulaması: aynı odada ve yeterince yakın mı.
            int targetRoom = Layout.RoomOf(target.transform.position);
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (targetRoom != Room) { Dbg.Interact("(3a) REDDEDİLDİ: farklı oda hedef=" + targetRoom + " avatar=" + Room); return; }
            if (dist > Interactable.ServerRange) { Dbg.Interact("(3a) REDDEDİLDİ: uzak d=" + dist.ToString("F1")); return; }
            if (target.ServerHandler != null) { target.ServerHandler(this); return; } // bulmaca düğümü
            if (state == null) { Dbg.Interact("(3a) REDDEDİLDİ: RoomState yok"); return; }
            state.Toggle((InteractId)id);
        }
    }
}
