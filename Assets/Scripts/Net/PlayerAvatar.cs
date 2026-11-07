using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Oyuncu temsili (kapsül). Rol, InputAuthority'nin giriş sırasından hesaplanır.</summary>
    public class PlayerAvatar : NetworkBehaviour
    {
        VoiceNetworkObject voice;

        public bool IsLocal => Object != null && Object.IsValid && Object.HasInputAuthority;
        public int RoleIndex => Roles.Index(Runner, Object.InputAuthority);

        /// <summary>Yerel oyuncu için mikrofon iletiyor mu, uzak oyuncu için ses çalıyor mu.</summary>
        public bool Speaking => voice != null && voice.Object != null && voice.Object.IsValid &&
                                (voice.IsLocal ? voice.IsRecording : voice.IsSpeaking);

        public override void Spawned()
        {
            voice = GetComponent<VoiceNetworkObject>();
            int role = RoleIndex;
            name = "Avatar_" + Roles.Name(role) + (IsLocal ? "_local" : "");
            var r = GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = role == 0 ? new Color(0.25f, 0.55f, 1f) : new Color(1f, 0.55f, 0.2f);
        }
    }
}
