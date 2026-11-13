using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Oyuncu avatarı. Host-authoritative: girdi (PlayerInput) host'a gider, NetworkCharacterController hareketi simüle eder.
    /// Yerelde tahmin, uzakta snapshot enterpolasyonu. Rol (X/O): Lobi Holü plakalarıyla host atar. Alan kuralı: uzak avatar yalnızca aynı odadaysa görünür
    /// (renderer yerel kapanır; ağ durumu ve ses etkilenmez).
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class PlayerAvatar : NetworkBehaviour
    {
        static readonly int SpeedParam = Animator.StringToHash("Speed");

        public static PlayerAvatar Local { get; private set; }
        /// <summary>Yerel tercih: uzak oyuncunun sesini kapat (oyun içi menü). Yalnız bu cihazı etkiler.</summary>
        public static bool RemoteMuted;
        /// <summary>Spawn edilmiş tüm avatarlar (host'ta basınç plakası vb. için).</summary>
        public static readonly System.Collections.Generic.List<PlayerAvatar> All = new System.Collections.Generic.List<PlayerAvatar>();

        VoiceNetworkObject voice;
        NetworkCharacterController cc;
        Animator animator;
        Renderer[] renderers;
        AudioSource[] voiceSources;
        bool remoteVisible = true;
        int shownRole = -99;
        Vector3 lastPos;
        float smoothSpeed;

        public bool IsLocal => Object != null && Object.IsValid && Object.HasInputAuthority;
        /// <summary>0 = seçilmedi, 1 = X, 2 = O (host yazar).</summary>
        [Networked] int RoleCode { get; set; }
        /// <summary>Bakış eğimi (santiderece). Host yazar: en fazla ~15 Hz ve yalnız ≥0.5° değişince (Fusion yalnız değişeni yollar).</summary>
        [Networked] short LookPitch { get; set; }

        const float PitchClamp = 60f;
        float shownPitch;
        Transform headBone, neckBone;
        Animator boneAnimator;

        /// <summary>Roles.X / Roles.O; holde henüz seçilmediyse Roles.None.</summary>
        public int RoleIndex => RoleCode - 1;
        /// <summary>Giriş sırası (0/1).</summary>
        public int Slot => Roles.Slot(Runner, Object.InputAuthority);
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
            voiceSources = GetComponentsInChildren<AudioSource>(true);
            lastPos = transform.position;
            if (IsLocal) Local = this;
            if (!All.Contains(this)) All.Add(this);

            ApplyRoleLook();
        }

        /// <summary>Host: rolü atar ve avatarı o rolün başlangıç odasına ışınlar.</summary>
        public void ServerAssignRole(int role, bool teleport = true)
        {
            if (!Object.HasStateAuthority) return;
            RoleCode = role + 1;
            if (teleport)
            {   // Başlangıç bulmacası için kontrol noktası varsa oraya (yoksa rolün ilk odası)
                var pos = Checkpoint.TryFind(role, Progress.Current(PuzzleManager.Instance), out var cp) ? cp : Layout.Spawn(role);
                cc.Teleport(pos, Quaternion.identity); hasSafe = false;
            }
            Dbg.Puzzle(name + " rol=" + Roles.Name(role));
        }

        /// <summary>Host, spawn öncesi (OnBeforeSpawned): yeniden bağlanan oyuncuya boştaki rol.</summary>
        public void InitRole(int role) => RoleCode = role + 1;

        void ApplyRoleLook()
        {
            int role = RoleIndex;
            if (role == shownRole) return;
            shownRole = role;
            name = "Avatar_" + Roles.Name(role) + (IsLocal ? "_local" : "");
            // Karakter modelleri (Model/Model_X, Model/Model_O): rol yoksa giriş sırasına göre (0 → X, 1 → O)
            var mx = transform.Find("Model/Model_X"); var mo = transform.Find("Model/Model_O");
            if (mx != null && mo != null)
            {
                bool isO = role == Roles.O || (role == Roles.None && Slot == 1);
                mx.gameObject.SetActive(!isO); mo.gameObject.SetActive(isO);
                animator = (isO ? mo : mx).GetComponent<Animator>();
                return;
            }
            var color = role == Roles.X ? new Color(0.25f, 0.55f, 1f) : role == Roles.O ? new Color(1f, 0.55f, 0.2f) : new Color(0.6f, 0.6f, 0.62f);
            foreach (var r in renderers)
                if (r != null && r.gameObject.name != "Nose") r.material.color = color;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Local == this) Local = null;
            All.Remove(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out PlayerInput input))
            {
                cc.Move(new Vector3(input.Move.x, 0f, input.Move.y));
                transform.rotation = Quaternion.Euler(0f, input.Yaw, 0f); // gövde hareket yönüne değil bakış yönüne (NCC dönüşünü ezer; TRSP ile senkron)
                if (Object.HasStateAuthority && !Runner.IsResimulation)
                {
                    int stride = Mathf.Max(1, Runner.TickRate / 15);
                    if (Runner.Tick % stride == 0 && Mathf.Abs(input.Pitch - LookPitch) >= 50) LookPitch = input.Pitch;
                }
            }

            // NCC hızı konum farkından türetir: basamak/eşya kenarına tırmanınca dikey hız fırlatma etkisi yapıyordu.
            // Oyunda zıplama yok → yukarı hız asla taşınmaz.
            var v = cc.Velocity;
            if (v.y > 0f) { v.y = 0f; cc.Velocity = v; }

            // Düşme güvencesi (host): son sağlam zemine geri al.
            if (!Object.HasStateAuthority || Runner.IsResimulation) return;
            var p = transform.position;
            if (cc.Grounded) { lastSafe = p; hasSafe = true; }
            else if (p.y < (hasSafe ? lastSafe.y : 0f) - 6f)
            {
                var back = hasSafe ? lastSafe + Vector3.up * 0.1f
                         : RoleIndex == Roles.None ? Layout.HallSpawn(0) : Layout.Spawn(RoleIndex);
                cc.Teleport(back, transform.rotation);
                cc.Velocity = Vector3.zero;
            }
        }

        Vector3 lastSafe;
        bool hasSafe;

        public override void Render()
        {
            ApplyRoleLook();
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
                foreach (var a in voiceSources) if (a != null && a.mute != RemoteMuted) a.mute = RemoteMuted;
                bool visible = Local != null && Room == Local.Room && !FollowCamera.HidingRemotes;
                if (visible != remoteVisible)
                {
                    remoteVisible = visible;
                    foreach (var r in renderers) if (r != null) r.enabled = visible;
                }
            }
        }

        /// <summary>Uzak avatar: bakış eğimini Animator'dan sonra boyun (%40) + kafa (%60) kemiğine uygular, yumuşatarak. Yerel avatar etkilenmez.</summary>
        void LateUpdate()
        {
            if (Object == null || !Object.IsValid || IsLocal || !remoteVisible || animator == null) return;
            if (boneAnimator != animator)
            {
                boneAnimator = animator;
                headBone = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                neckBone = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Neck) : null;
            }
            if (headBone == null) return;
            float target = Mathf.Clamp(LookPitch * 0.01f, -PitchClamp, PitchClamp);
            shownPitch = Mathf.Lerp(shownPitch, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
            var axis = transform.right;
            if (neckBone != null)
            {
                neckBone.rotation = Quaternion.AngleAxis(shownPitch * 0.4f, axis) * neckBone.rotation;
                headBone.rotation = Quaternion.AngleAxis(shownPitch * 0.6f, axis) * headBone.rotation;
            }
            else headBone.rotation = Quaternion.AngleAxis(shownPitch, axis) * headBone.rotation;
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

        // ---- Yetenek (ör. kibrit yak): konuma bağlı değil; host doğrulamayı dinleyici yapar ----

        /// <summary>Host'ta: (avatar, yetenek id). Dinleyiciler (CryptPuzzle) kendi koşullarını kontrol eder.</summary>
        public static event System.Action<PlayerAvatar, int> ServerAbility;

        public void RequestAbility(int ability) => RPC_Ability(ability);

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        void RPC_Ability(int ability)
        {
            Dbg.Puzzle("Yetenek " + ability + " ← " + name);
            ServerAbility?.Invoke(this, ability);
        }
    }
}
