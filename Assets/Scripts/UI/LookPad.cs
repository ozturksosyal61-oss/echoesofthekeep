using UnityEngine;
using UnityEngine.EventSystems;

namespace CoopGame
{
    /// <summary>Ekranın sağ yarısı: sürükleyerek kamera döndürme (dokunma ve fare).</summary>
    public class LookPad : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => InputSource.AddLook(e.delta);
    }
}
