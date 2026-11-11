using UnityEngine;

namespace CoopGame
{
    /// <summary>RoomState'i yerel sahne nesnelerine uygular: lamba (ışık + parlama) ve kapı (LiftDoor). Sahne tarafı, ağ kodu içermez.</summary>
    public class RoomVisuals : MonoBehaviour
    {
        [SerializeField] Light lampLight;
        [SerializeField] Renderer lampRenderer;
        [SerializeField] LiftDoor door;

        Material lampMat;
        bool hasLast, lastLamp, lastOpen;
        RoomState lastState;

        void Awake()
        {
            lampMat = lampRenderer.material;
            lampMat.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            var s = RoomState.Instance;
            bool lamp = s != null && s.LampOn;
            bool open = s != null && s.DoorOpen;

            // (4b) Görsel, değişen durumu okudu mu
            if (!hasLast || s != lastState || lamp != lastLamp || open != lastOpen)
            {
                Dbg.Interact("(4b) RoomVisuals: state=" + (s != null ? "var" : "YOK") + " lamba=" + lamp + " kapı=" + open +
                             " (lampLight.enabled→" + lamp + ", door.Open→" + open + ")");
                hasLast = true; lastState = s; lastLamp = lamp; lastOpen = open;
            }

            lampLight.enabled = lamp;
            var glow = lamp ? new Color(1f, 0.9f, 0.5f) : new Color(0.2f, 0.2f, 0.2f);
            lampMat.color = glow;
            lampMat.SetColor("_EmissionColor", lamp ? glow * 2f : Color.black);

            door.Open = open;
        }

        public bool LampOn => RoomState.Instance != null && RoomState.Instance.LampOn;
        public bool DoorOpen => RoomState.Instance != null && RoomState.Instance.DoorOpen;
    }
}
