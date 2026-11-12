using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoopGame
{
    /// <summary>Lamba/kapı butonlarının sabit anahtarları. Bulmaca düğümleri PuzzleNode.InteractKeyBase + nodeId kullanır.</summary>
    public enum InteractId { LampButton = 1, DoorButton = 2 }

    /// <summary>
    /// Bakılan/yaklaşılan sahne nesnesi. Kimlik (Key) RPC ile host'a gider. ServerHandler atanmışsa (bulmaca düğümü) host onu çağırır,
    /// yoksa RoomState.Toggle (lamba/kapı). Ağ durumu RoomState/PuzzleManager'da; burada yalnızca kimlik ve konum.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public const float ClientRange = 3f;   // 3. şahıs: "Etkileş" düğmesi bu yakınlıkta görünür
        public const float RayRange = 3f;      // 1. şahıs: nişangâh ışınının menzili (göz hizasından)
        public const float ServerRange = 4.5f; // host doğrulaması (gecikme payı)

        public static readonly List<Interactable> All = new List<Interactable>();

        public int Key;
        public string Label;

        /// <summary>Host tarafı işleyici (PuzzleNode atar). Null ise RoomState.Toggle((InteractId)Key).</summary>
        [NonSerialized] public Action<PlayerAvatar> ServerHandler;

        /// <summary>Yalnız yerel etkileşim (mektup okuma vb.): atanmışsa RPC gönderilmez, bu çağrılır.</summary>
        [NonSerialized] public Action LocalHandler;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Interactable Find(int key)
        {
            foreach (var i in All) if (i.Key == key) return i;
            return null;
        }
    }
}
