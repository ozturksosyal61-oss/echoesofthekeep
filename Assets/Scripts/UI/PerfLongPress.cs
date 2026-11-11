using UnityEngine;
using UnityEngine.EventSystems;

namespace CoopGame
{
    /// <summary>HUD'daki FPS metnine uzun basınca (0,8 sn) gizli debug panelini açar.</summary>
    public class PerfLongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        const float HoldSeconds = 0.8f;
        float downAt = -1f;

        public void OnPointerDown(PointerEventData e) => downAt = Time.unscaledTime;
        public void OnPointerUp(PointerEventData e) => downAt = -1f;

        void Update()
        {
            if (downAt >= 0f && Time.unscaledTime - downAt >= HoldSeconds)
            {
                downAt = -1f;
                var panel = FindFirstObjectByType<DebugPanel>();
                if (panel != null) panel.Toggle();
            }
        }
    }
}
