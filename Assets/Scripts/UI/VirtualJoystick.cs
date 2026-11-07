using UnityEngine;
using UnityEngine.EventSystems;

namespace CoopGame
{
    /// <summary>Sol alt sanal joystick. Çoklu dokunmada bakış alanıyla birlikte çalışır.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform handle;
        [SerializeField] float radius = 110f;

        RectTransform area;
        Canvas canvas;

        void Awake()
        {
            area = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
        }

        void OnDisable() => InputSource.VirtualMove = Vector2.zero;

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var local);
            var v = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = v;
            InputSource.VirtualMove = v / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            handle.anchoredPosition = Vector2.zero;
            InputSource.VirtualMove = Vector2.zero;
        }
    }
}
