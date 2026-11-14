using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Bağlantı kopması. Client: aynı kodla odaya yeniden girer (host koptuysa onun odayı yeniden kurmasını bekler).
    /// Host: aynı kodla odayı yeniden kurar, kayıtlı ilerlemeden (Progress) ve kendi rol/konumundan devam eder.
    /// Ayrıca host'ta arkadaş koptuğunda "bekleniyor" şeridi gösterir. Sahneler arası yaşar, arayüzü kodla kurar.
    /// </summary>
    public class Reconnect : MonoBehaviour
    {
        const float GiveUp = 90f, Retry = 3f;
        static Reconnect inst;

        public static bool Active { get; private set; }

        Canvas canvas;
        GameObject panel, banner;
        Text title, line, bannerText;
        Coroutine job;
        bool hadTwo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("Reconnect");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Reconnect>();
        }

        public static void Begin(string code, bool host, int role, Vector3 pos)
        {
            if (inst == null) Boot();
            inst.Stop();
            Active = true;
            inst.job = inst.StartCoroutine(inst.Run(code, host, role, pos));
        }

        /// <summary>Session.Leave çağırır: deneme varsa durdur.</summary>
        public static void Cancel() { if (inst != null) inst.Stop(); }

        void Stop()
        {
            if (job != null) StopCoroutine(job);
            job = null; Active = false;
            Session.ResumeRole = Roles.None;
            if (panel != null) panel.SetActive(false);
        }

        IEnumerator Run(string code, bool host, int role, Vector3 pos)
        {
            EnsureUi();
            panel.SetActive(true);
            title.text = "BAĞLANTI KOPTU";
            float until = Time.unscaledTime + GiveUp;
            int attempt = 0;
            while (Time.unscaledTime < until)
            {
                attempt++;
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    line.text = "İnternet bekleniyor...";
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }
                line.text = (host ? "Oda yeniden kuruluyor" : "Odaya yeniden bağlanılıyor") + " (deneme " + attempt + ")\nOda kodu: " + code;
                if (host)
                {   // Kayıtlı ilerlemeden devam (çözülen bulmacalar korunur); yerel oyuncu eski rol ve konumunda doğar.
                    Progress.StartPuzzle = Mathf.Min(Progress.Saved + 1, Progress.LastPuzzle + 1);
                    Session.ResumeRole = role; Session.ResumePos = pos;
                }
                var t = host ? Session.Create(code) : Session.Join(code);
                while (!t.IsCompleted) yield return null;
                var res = t.Result;
                if (res.Ok)
                {
                    line.text = "Bağlandı, oyun yükleniyor...";
                    float wait = Time.unscaledTime + 20f;
                    while (PlayerAvatar.Local == null && Session.Runner != null && Time.unscaledTime < wait) yield return null;
                    if (PlayerAvatar.Local != null) { job = null; Active = false; panel.SetActive(false); yield break; }
                    if (Session.Runner != null) { job = null; Active = false; panel.SetActive(false); yield break; } // yükleniyor; oturum ayakta
                }
                else if (!host && res.ShutdownReason == ShutdownReason.GameNotFound)
                    line.text = "Ev sahibinin geri dönmesi bekleniyor...\nOda kodu: " + code;
                yield return new WaitForSecondsRealtime(Retry);
            }
            line.text = "Yeniden bağlanılamadı. İlerleme kaydedildi;\nmenüden DEVAM ET ile sürdürebilirsiniz.";
            yield return new WaitForSecondsRealtime(3f);
            job = null;
            Session.Leave();
        }

        void Update()
        {
            // Host: arkadaş kopunca bekleme şeridi (o aynı kodla geri döner, kaldığı yerde doğar).
            var r = Session.Runner;
            bool show = false;
            if (r != null && r.IsRunning && r.IsServer && SceneManager.GetActiveScene().name == Session.GameScene)
            {
                int n = 0; foreach (var _ in r.ActivePlayers) n++;
                if (n >= 2) hadTwo = true;
                show = hadTwo && n < 2;
            }
            else hadTwo = false;
            if (show) { EnsureUi(); bannerText.text = "Arkadaşının bağlantısı koptu, geri dönmesi bekleniyor... (oda " + Session.Code + ")"; }
            if (banner != null && banner.activeSelf != show) banner.SetActive(show);
        }

        // ---- arayüz ----
        void EnsureUi()
        {
            if (canvas != null) return;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Font font = null;
            foreach (var t in FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.font != null) { font = t.font; break; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var gold = new Color(0.86f, 0.74f, 0.50f);

            panel = Box("Panel", transform, Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.03f, 0.88f));
            title = Label(panel.transform, font, 54, gold, new Vector2(0, 0.55f), new Vector2(1, 0.68f));
            line = Label(panel.transform, font, 32, new Color(0.85f, 0.82f, 0.76f), new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.55f));
            var btn = Box("Menu", panel.transform, new Vector2(0.4f, 0.2f), new Vector2(0.6f, 0.29f), new Color(0.18f, 0.12f, 0.08f, 0.95f));
            var bl = Label(btn.transform, font, 30, gold, Vector2.zero, Vector2.one); bl.text = "MENÜYE DÖN";
            btn.AddComponent<Button>().onClick.AddListener(() => { Stop(); Session.Leave(); });
            panel.SetActive(false);

            banner = Box("Banner", transform, new Vector2(0.2f, 0.88f), new Vector2(0.8f, 0.94f), new Color(0.06f, 0.04f, 0.03f, 0.8f));
            banner.GetComponent<Image>().raycastTarget = false;
            bannerText = Label(banner.transform, font, 26, gold, Vector2.zero, Vector2.one);
            banner.SetActive(false);
        }

        static GameObject Box(string name, Transform parent, Vector2 min, Vector2 max, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = c;
            return go;
        }

        static Text Label(Transform parent, Font font, int size, Color c, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            return t;
        }
    }
}
