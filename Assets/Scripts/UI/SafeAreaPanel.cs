using UnityEngine;

namespace CoopGame
{
    /// <summary>Panelin anchor'larını Screen.safeArea'ya göre ayarlar (çentik, kavisli kenar, gezinme çubuğu). Yön/çözünürlük değişince yeniden uygular.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        Rect lastArea;
        Vector2Int lastScreen;
        ScreenOrientation lastOrientation;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != lastArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y ||
                Screen.orientation != lastOrientation)
                Apply();
        }

        void Apply()
        {
            var area = Screen.safeArea;
            lastArea = area;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            var min = area.position;
            var max = area.position + area.size;
            rt.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
            rt.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
