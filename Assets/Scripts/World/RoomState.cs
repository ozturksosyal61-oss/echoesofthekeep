using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Paylaşılan dünya durumu. State authority host; istemciler yalnızca okur (RPC ile ister).</summary>
    public class RoomState : NetworkBehaviour
    {
        public static RoomState Instance { get; private set; }

        [Networked] public NetworkBool LampOn { get; set; }
        [Networked] public NetworkBool DoorOpen { get; set; }

        ChangeDetector changes;

        public override void Spawned()
        {
            Instance = this;
            changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Yalnızca host çağırır (PlayerAvatar RPC'si doğruladıktan sonra).</summary>
        public void Toggle(InteractId id)
        {
            if (!HasStateAuthority) return;
            if (id == InteractId.LampButton) LampOn = !LampOn;
            else if (id == InteractId.DoorButton) DoorOpen = !DoorOpen;
        }

        public override void Render()
        {
            foreach (var change in changes.DetectChanges(this))
            {
                if (change == nameof(LampOn)) Debug.Log("[Room] Lamba: " + (LampOn ? "açık" : "kapalı"));
                else if (change == nameof(DoorOpen)) Debug.Log("[Room] Kapı: " + (DoorOpen ? "açık" : "kapalı"));
            }
        }
    }
}
