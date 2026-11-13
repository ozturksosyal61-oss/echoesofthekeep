using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Gizli performans paneli (HUD'daki FPS'e uzun basınca açılır): MSAA, render scale, gölge, ek ışıklar.
    /// Her değişiklikten sonra 10 sn FPS ortalaması ekranda ve logcat'te ([Sweep]) yazılır.
    /// "Tümünü ölç" 24 kombinasyonu otomatik dener. persistentDataPath/autosweep.txt varsa Game'de otomatik başlar.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        const float Settle = 2f;    // ayar değişince FPS'in oturması için atılan süre
        const float Window = 10f;   // ölçüm penceresi

        static readonly int[] MsaaOptions = { 1, 2 };
        static readonly float[] ScaleOptions = { 1.0f, 0.85f, 0.7f };

        int msaaIdx = 1, scaleIdx;
        bool shadows = true, addLights = true;

        UniversalRenderPipelineAsset urp;
        FieldInfo addLightsField;
        Light mainLight;
        GameObject root;
        Text status;
        Text[] btnLabels = new Text[4];
        Coroutine measureRoutine, sweepRoutine;
        string lastResult = "-";
        readonly List<string> results = new List<string>();

        string BuildTag => (Debug.isDebugBuild ? "dev" : "release") + "+" + SystemInfo.graphicsDeviceType;

        void Start()
        {
            if (!TestTools.On) { Destroy(gameObject); return; }
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            addLightsField = typeof(UniversalRenderPipelineAsset).GetField("m_AdditionalLightsRenderingMode", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { mainLight = l; break; }

            Build();
            root.SetActive(false);
            Apply();

            string flag = Path.Combine(Application.persistentDataPath, "autosweep.txt");
            if (File.Exists(flag))
            {
                try { File.Delete(flag); } catch { }
                StartCoroutine(AutoStart());
            }
        }

        IEnumerator AutoStart()
        {
            yield return new WaitForSeconds(6f); // sahne ve ağ otursun
            RunSweep();
        }

        public void Toggle()
        {
            root.SetActive(!root.activeSelf);
            RefreshLabels();
        }

        // ---------- ayarları uygula ----------

        void Apply()
        {
            if (urp != null)
            {
                urp.msaaSampleCount = MsaaOptions[msaaIdx];
                urp.renderScale = ScaleOptions[scaleIdx];
                if (addLightsField != null)
                    addLightsField.SetValue(urp, addLights ? LightRenderingMode.PerPixel : LightRenderingMode.Disabled);
            }
            if (mainLight != null) mainLight.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            RefreshLabels();
        }

        string Describe() =>
            "msaa=" + (MsaaOptions[msaaIdx] == 1 ? "off" : MsaaOptions[msaaIdx] + "x") + " scale=" + ScaleOptions[scaleIdx].ToString("F2") +
            " shadows=" + (shadows ? "on" : "off") + " lights=" + (addLights ? "on" : "off");

        // ---------- ölçüm ----------

        IEnumerator Measure(System.Action<float> done)
        {
            yield return new WaitForSecondsRealtime(Settle);
            int f0 = Time.frameCount; float t0 = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(Window);
            float fps = (Time.frameCount - f0) / (Time.realtimeSinceStartup - t0);
            done(fps);
        }

        void MeasureNow()
        {
            if (sweepRoutine != null) return;
            if (measureRoutine != null) StopCoroutine(measureRoutine);
            string desc = Describe();
            lastResult = "ölçülüyor... (" + (Settle + Window) + " sn)";
            RefreshLabels();
            measureRoutine = StartCoroutine(Measure(fps =>
            {
                lastResult = fps.ToString("F1") + " FPS  [" + desc + "]";
                Debug.Log("[Sweep] build=" + BuildTag + " " + desc + " fps=" + fps.ToString("F1"));
                measureRoutine = null;
                RefreshLabels();
            }));
        }

        void RunSweep()
        {
            if (sweepRoutine != null) return;
            if (measureRoutine != null) { StopCoroutine(measureRoutine); measureRoutine = null; }
            sweepRoutine = StartCoroutine(Sweep());
        }

        IEnumerator Sweep()
        {
            results.Clear();
            Debug.Log("[Sweep] START build=" + BuildTag + " res=" + Screen.width + "x" + Screen.height + " hz=" + Screen.currentResolution.refreshRateRatio.value.ToString("F0") +
                      " targetFps=" + Application.targetFrameRate);
            int total = MsaaOptions.Length * ScaleOptions.Length * 2 * 2, n = 0;
            for (int m = 0; m < MsaaOptions.Length; m++)
                for (int s = 0; s < ScaleOptions.Length; s++)
                    for (int sh = 1; sh >= 0; sh--)
                        for (int al = 1; al >= 0; al--)
                        {
                            msaaIdx = m; scaleIdx = s; shadows = sh == 1; addLights = al == 1;
                            Apply();
                            n++;
                            string desc = Describe();
                            lastResult = "tarama " + n + "/" + total + " ölçülüyor [" + desc + "]";
                            RefreshLabels();
                            float fps = 0f;
                            yield return Measure(v => fps = v);
                            string line = "build=" + BuildTag + " " + desc + " fps=" + fps.ToString("F1");
                            results.Add(line);
                            Debug.Log("[Sweep] " + line);
                        }
            var sb = new StringBuilder();
            foreach (var r in results) sb.AppendLine(r);
            lastResult = "tarama bitti (" + total + " kombinasyon)";
            Debug.Log("[Sweep] DONE build=" + BuildTag + " n=" + total);
            RefreshLabels();
            sweepRoutine = null;
        }

        // ---------- arayüz (çalışma anında kurulur, sahneye bağımlı değil) ----------

        void RefreshLabels()
        {
            if (btnLabels[0] == null) return;
            btnLabels[0].text = "MSAA: " + (MsaaOptions[msaaIdx] == 1 ? "kapalı" : MsaaOptions[msaaIdx] + "x");
            btnLabels[1].text = "Ölçek: " + ScaleOptions[scaleIdx].ToString("F2");
            btnLabels[2].text = "Gölge: " + (shadows ? "açık" : "kapalı");
            btnLabels[3].text = "Ek ışık: " + (addLights ? "açık" : "kapalı");
            status.text = BuildTag + "\nSon: " + lastResult;
        }

        void Build()
        {
            var cgo = new GameObject("DebugPanelCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            cgo.AddComponent<GraphicRaycaster>();

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(cgo.transform, false);
            var srt = (RectTransform)safe.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
            safe.AddComponent<SafeAreaPanel>();

            root = new GameObject("Panel", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.SetParent(srt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000, 640);
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            status = MakeText(rt, "Status", new Vector2(0, 250), new Vector2(940, 130), 30, TextAnchor.UpperCenter);

            System.Action[] actions =
            {
                () => { msaaIdx = (msaaIdx + 1) % MsaaOptions.Length; Apply(); MeasureNow(); },
                () => { scaleIdx = (scaleIdx + 1) % ScaleOptions.Length; Apply(); MeasureNow(); },
                () => { shadows = !shadows; Apply(); MeasureNow(); },
                () => { addLights = !addLights; Apply(); MeasureNow(); },
            };
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector2(i % 2 == 0 ? -250 : 250, i < 2 ? 70 : -40);
                int idx = i;
                btnLabels[i] = MakeButton(rt, "B" + i, pos, new Vector2(460, 90), "", actions[idx]);
            }
            MakeButton(rt, "Sweep", new Vector2(-250, -180), new Vector2(460, 90), "Tümünü ölç (24)", RunSweep);
            MakeButton(rt, "Close", new Vector2(250, -180), new Vector2(460, 90), "Kapat", () => root.SetActive(false));
        }

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.font = UiFont; t.fontSize = fontSize; t.alignment = anchor; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        static Text MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            var t = MakeText(rt, "Label", Vector2.zero, size, 36, TextAnchor.MiddleCenter);
            t.text = label;
            return t;
        }
    }
}
