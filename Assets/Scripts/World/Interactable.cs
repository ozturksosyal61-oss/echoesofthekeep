using System.Collections.Generic;
using UnityEngine;

namespace CoopGame
{
    public enum InteractId { LampButton = 1, DoorButton = 2 }

    /// <summary>Yaklaşıp etkileşilen sahne nesnesi (buton). Ağ durumu RoomState'te; burada yalnızca kimlik ve konum.</summary>
    public class Interactable : MonoBehaviour
    {
        public const float ClientRange = 3f;   // 3. şahıs: "Etkileş" düğmesi bu yakınlıkta görünür
        public const float RayRange = 3f;      // 1. şahıs: nişangâh ışınının menzili (göz hizasından)
        public const float ServerRange = 4.5f; // host doğrulaması (gecikme payı)

        public static readonly List<Interactable> All = new List<Interactable>();

        public InteractId Id;
        public string Label;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Interactable Find(InteractId id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }
    }
}
