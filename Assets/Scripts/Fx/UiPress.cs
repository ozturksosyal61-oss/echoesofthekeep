using UnityEngine;
using UnityEngine.EventSystems;

namespace CoopGame
{
    /// <summary>Dokunma geri bildirimi: basılıyken düğme hafifçe küçülür (yalnızca görsel).</summary>
    public class UiPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] float pressedScale = 0.94f;

        float target = 1f;

        public void OnPointerDown(PointerEventData e) => target = pressedScale;
        public void OnPointerUp(PointerEventData e) => target = 1f;
        void OnDisable() { target = 1f; transform.localScale = Vector3.one; }

        void Update()
        {
            float s = Mathf.MoveTowards(transform.localScale.x, target, 1.2f * Time.unscaledDeltaTime);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
