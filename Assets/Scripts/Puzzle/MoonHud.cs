using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 6 yerel arayüzü: çatıdaki/kuyudaki oyuncuya ay süresi çubuğu (yalnız çatı), tur ilerlemesi (ay mühürleri) ve mesajlar.
    /// </summary>
    public class MoonHud : MonoBehaviour
    {
        [SerializeField] MoonPuzzle puzzle;
        [SerializeField] Font font;
        [SerializeField] Vector3 roofCenter, roofSize, wellCenter, wellSize; // dünya

        Image back, fill, hold;
        Text info, message;
        float messageUntil;
        int seenRound = -1, seenSeed = -1;
        PuzzleState seenState;

        void Start()
        {
            var canvas = new GameObject("MoonHud", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var cs = canvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1f;
            back = Img(canvas.transform, new Vector2(0, -150), new Vector2(360, 14), new Color(0.04f, 0.05f, 0.08f, 0.75f));
            fill = Img(back.transform, Vector2.zero, new Vector2(352, 8), new Color(0.62f, 0.72f, 0.95f, 0.9f));
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(4, 0);
            hold = Img(canvas.transform, new Vector2(0, -172), new Vector2(0, 6), new Color(0.95f, 0.9f, 0.7f, 0.9f));
            info = Txt(canvas.transform, new Vector2(0.5f, 0.82f), 30);
            message = Txt(canvas.transform, new Vector2(0.5f, 0.62f), 40);
        }

        Image Img(Transform parent, Vector2 pos, Vector2 size, Color c)
        {
            var im = new GameObject("Img", typeof(RectTransform)).AddComponent<Image>();
            im.transform.SetParent(parent, false);
            var rt = im.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            im.color = c; im.raycastTarget = false;
            return im;
        }

        Text Txt(Transform parent, Vector2 anchor, int size)
        {
            var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = anchor; t.rectTransform.sizeDelta = new Vector2(1300, 120);
            t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(0.86f, 0.88f, 0.95f); t.raycastTarget = false;
            return t;
        }

        void Say(string t, float sec) { message.text = t; messageUntil = Time.time + sec; }

        void Update()
        {
            if (puzzle == null || info == null) return;
            var me = PlayerAvatar.Local;
            Vector3 p = me != null ? me.transform.position + Vector3.up * 0.5f : Vector3.one * 1e6f;
            bool onRoof = new Bounds(roofCenter, roofSize).Contains(p), inWell = new Bounds(wellCenter, wellSize).Contains(p);
            var st = puzzle.PuzzleState;
            bool show = (onRoof || inWell) && st == PuzzleState.Active && puzzle.Seed != 0;
            int round = puzzle.Round;

            if (show && seenSeed > 0 && puzzle.Seed != seenSeed && round == 0)
                Say(onRoof ? "Bulutlar ayı yuttu. Rüzgâr aynaları savurdu." : "Işık söndü. Zemindeki ay taşları kayıp yer değiştirdi.", 4.5f);
            else if (show && seenRound >= 0 && round > seenRound)
                Say(onRoof ? "Ay yer değiştirdi... (" + round + "/" + MoonPuzzle.Rounds + ")" : "Ay taşı ışıkla mühürlendi (" + round + "/" + MoonPuzzle.Rounds + ")", 4f);
            if (st != seenState) { if (st == PuzzleState.Solved && seenState == PuzzleState.Active && (onRoof || inWell)) Say("Üç ay da kuyuya indi. Derinlerde bir şey açıldı.", 5f); seenState = st; }
            seenRound = round; seenSeed = puzzle.Seed;
            if (Time.time > messageUntil) message.text = "";

            back.gameObject.SetActive(show && onRoof);
            fill.rectTransform.sizeDelta = new Vector2(352f * puzzle.Window01, 8f);
            hold.rectTransform.sizeDelta = new Vector2(show ? 360f * puzzle.Hold01 : 0f, 6f);
            if (!show) { info.text = ""; return; }
            info.text = puzzle.CurrentPhase == MoonPuzzle.Phase.Cloud ? "Bulut..." : "Ay mührü " + round + "/" + MoonPuzzle.Rounds;
        }
    }
}
