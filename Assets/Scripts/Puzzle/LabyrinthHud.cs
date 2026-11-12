using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 5 yerel görünümü: labirentteki oyuncu için zifiri karanlık (ortam/sis kararır, diğer ışıklar söner),
    /// elde fener (yağ azaldıkça söner, sonlara doğru titrer), yağ çubuğu, ölüm kararması ve mesajlar. UI çalışma anında kurulur.
    /// </summary>
    public class LabyrinthHud : MonoBehaviour
    {
        [SerializeField] LabyrinthPuzzle puzzle;
        [SerializeField] Font font;
        static readonly Color DarkAmbient = new Color(0.003f, 0.003f, 0.004f);

        Light lantern;
        Image fade, oilBack, oilFill;
        Text message;
        bool dark;
        Color ambient0, fog0; float density0, refl0; UnityEngine.Rendering.SphericalHarmonicsL2 probe0;
        readonly List<Light> killed = new List<Light>();
        int seenAttempt = -1; PuzzleState seenState;
        float messageUntil;

        void Start()
        {
            ambient0 = RenderSettings.ambientLight; fog0 = RenderSettings.fogColor; density0 = RenderSettings.fogDensity;
            refl0 = RenderSettings.reflectionIntensity; probe0 = RenderSettings.ambientProbe;
            var lg = new GameObject("HandLantern");
            lantern = lg.AddComponent<Light>();
            lantern.type = LightType.Point; lantern.color = new Color(1f, 0.64f, 0.32f); lantern.range = 7f; lantern.shadows = LightShadows.None;
            lantern.enabled = false;

            var canvas = new GameObject("LabyrinthHud", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var cs = canvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1f;
            fade = Img(canvas.transform, "Fade", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0), true);
            oilBack = Img(canvas.transform, "OilBack", new Vector2(0, -150), new Vector2(360, 14), new Color(0.05f, 0.04f, 0.03f, 0.75f), false);
            oilFill = Img(oilBack.transform, "OilFill", Vector2.zero, new Vector2(352, 8), new Color(1f, 0.66f, 0.3f, 0.9f), false);
            oilFill.rectTransform.anchorMin = oilFill.rectTransform.anchorMax = oilFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            oilFill.rectTransform.anchoredPosition = new Vector2(4, 0);
            var mt = new GameObject("Message", typeof(RectTransform)).AddComponent<Text>();
            mt.transform.SetParent(canvas.transform, false);
            mt.rectTransform.anchorMin = mt.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            mt.rectTransform.sizeDelta = new Vector2(1200, 140);
            mt.font = font; mt.fontSize = 40; mt.alignment = TextAnchor.MiddleCenter; mt.color = new Color(0.93f, 0.85f, 0.68f);
            mt.raycastTarget = false;
            message = mt;
            oilBack.gameObject.SetActive(false);
        }

        static Image Img(Transform parent, string name, Vector2 pos, Vector2 size, Color c, bool stretch)
        {
            var im = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            im.transform.SetParent(parent, false);
            var rt = im.rectTransform;
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; }
            else { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
            im.color = c; im.raycastTarget = false;
            return im;
        }

        void OnDestroy() => SetDark(false);

        void SetDark(bool on)
        {
            if (on == dark) return;
            dark = on;
            RenderSettings.ambientLight = on ? DarkAmbient : ambient0;
            RenderSettings.fogColor = on ? Color.black : fog0;
            RenderSettings.fogDensity = on ? 0.2f : density0;
            RenderSettings.reflectionIntensity = on ? 0f : refl0;
            var sh = new UnityEngine.Rendering.SphericalHarmonicsL2(); sh.AddAmbientLight(DarkAmbient);
            RenderSettings.ambientProbe = on ? sh : probe0;
            if (on)
            {
                killed.Clear();
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.enabled && l != lantern && l.type != LightType.Directional) { l.enabled = false; killed.Add(l); }
            }
            else { foreach (var l in killed) if (l != null) l.enabled = true; killed.Clear(); }
        }

        void Say(string t, float sec) { message.text = t; messageUntil = Time.time + sec; }

        void Update()
        {
            if (puzzle == null || message == null) return;
            var me = PlayerAvatar.Local;
            var st = puzzle.PuzzleState;
            bool inMaze = me != null && puzzle.CellOf(me.transform.position, out _) && st != PuzzleState.Solved;
            SetDark(inMaze);

            int att = puzzle.Attempt;
            if (seenAttempt >= 0 && att > seenAttempt) Say(inMaze || (me != null && puzzle.InAnte(me.transform.position)) ? "Fener söndü. Katakomb yeniden şekillendi." : "Aşağıdan bir taş gürültüsü yankılanıyor...", 4.5f);
            seenAttempt = att;
            if (st != seenState) { if (st == PuzzleState.Solved && seenState == PuzzleState.Active) Say("Katakombdan çıkış bulundu.", 4f); seenState = st; }
            if (Time.time > messageUntil) message.text = "";

            float oil = puzzle.Oil01;
            var cam = Camera.main;
            lantern.enabled = inMaze && cam != null && oil > 0f;
            if (lantern.enabled)
            {
                lantern.transform.SetPositionAndRotation(cam.transform.position + cam.transform.right * 0.25f - cam.transform.up * 0.25f + cam.transform.forward * 0.3f, cam.transform.rotation);
                float flick = oil < 0.2f ? Mathf.PerlinNoise(Time.time * 14f, 0.7f) : 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 6f, 0.2f);
                lantern.intensity = Mathf.Lerp(0.6f, 2.4f, Mathf.Sqrt(oil)) * flick;
                lantern.range = Mathf.Lerp(3.5f, 7.5f, oil);
            }
            oilBack.gameObject.SetActive(inMaze);
            oilFill.rectTransform.sizeDelta = new Vector2(352f * oil, 8f);
            float f = puzzle.CurrentPhase == LabyrinthPuzzle.Phase.Dying && (inMaze || (me != null && puzzle.InAnte(me.transform.position))) ? puzzle.Dying01 : 0f;
            fade.color = new Color(0, 0, 0, Mathf.MoveTowards(fade.color.a, f > 0f ? 1f : 0f, Time.deltaTime * (f > 0f ? 1.5f : 0.8f)));
        }
    }
}
