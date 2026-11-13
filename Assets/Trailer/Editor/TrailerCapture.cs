using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoopGame.Trailer
{
    /// <summary>
    /// Tanıtım videosu ham çekimleri (yalnız editör, build'e girmez). Game sahnesini açar, UI'yi gizler, her çekim için kamera yolunu
    /// kare kare işler ve Unity'nin MP4 (H.264) kodlayıcısıyla yazar: landscape 1920x1080 + portrait 1080x1920 (aynı hareket, dikey için
    /// geniş açı), 60 fps, sessiz. Ayrıca stills/ içine 3840x2160 PNG. Oyun mantığına dokunmaz; sahne kaydedilmez.
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod CoopGame.Trailer.TrailerCapture.Run [-trailerOut klasör] [-trailerOnly 04,06]
    /// </summary>
    public static class TrailerCapture
    {
        const int Fps = 60;
        const float FovL = 55f, FovP = 80f;

        class Shot
        {
            public string name; public float dur;
            public Vector3 p0, p1, l0, l1;      // konum ve bakış noktası (başlangıç → bitiş)
            public Action<float> anim;          // 0..1 ilerleme ile nesne hareketi
            public bool still;
        }

        static Vector3 A => Layout.RoomX;
        static Vector3 B => Layout.RoomO;
        static Vector3 H => Layout.Hall;
        static Vector3 F => Layout.Final;

        [MenuItem("CoopGame/Trailer/Çekimleri Kaydet")]
        public static void Run()
        {
            string outDir = Arg("-trailerOut") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "EchoesTrailer");
            string only = Arg("-trailerOnly");
            foreach (var d in new[] { "landscape", "portrait", "stills" }) Directory.CreateDirectory(Path.Combine(outDir, d));

            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            var cam = Camera.main;
            foreach (var cv in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) cv.enabled = false;
            PrepareProps();

            var shots = BuildShots();
            int stills = 0;
            foreach (var s in shots)
            {
                if (!string.IsNullOrEmpty(only) && Array.IndexOf(only.Split(','), s.name.Substring(0, 2)) < 0) continue;
                Record(cam, s, Path.Combine(outDir, "landscape", s.name + ".mp4"), 1920, 1080, FovL);
                Record(cam, s, Path.Combine(outDir, "portrait", s.name + ".mp4"), 1080, 1920, FovP);
                if (s.still && stills < 8)
                {
                    Pose(cam, s, 0.5f, FovL);
                    SavePng(cam, Path.Combine(outDir, "stills", s.name + ".png"), 3840, 2160);
                    stills++;
                }
                Debug.Log("[Trailer] " + s.name + " tamam");
            }
            Debug.Log("[Trailer] Çıktı: " + outDir);
        }

        // --- Çekim listesi (dünya koordinatları; odalar CoopSetup Shots ile aynı) ---
        static List<Shot> BuildShots()
        {
            var rf = A + new Vector3(110f, 35f, -90f);   // çatı terası
            var bl = A + new Vector3(95f, 0, 70f);       // çan dairesi
            var cz = B + new Vector3(-50f, 0, 0);        // sarnıç
            var L = new List<Shot>();
            void Add(string n, float d, Vector3 p0, Vector3 l0, Vector3 p1, Vector3 l1, Action<float> anim = null, bool still = false) =>
                L.Add(new Shot { name = n, dur = d, p0 = p0, l0 = l0, p1 = p1, l1 = l1, anim = anim, still = still });

            Add("01_sato_dis", 7f, rf + new Vector3(-1.5f, 1.4f, -11.5f), rf + new Vector3(-2f, 9f, 14f), rf + new Vector3(0.8f, 1.7f, -8.5f), rf + new Vector3(0.5f, 7.5f, 14f), null, true);
            Add("02_lobi", 7f, H + new Vector3(0f, 1.72f, -10.2f), H + new Vector3(0f, 3.4f, 6f), H + new Vector3(0f, 2.3f, -6.2f), H + new Vector3(0f, 4.6f, 6f), null, true);
            Add("03_plaka", 6f, H + new Vector3(2.2f, 1.72f, -2.5f), H + new Vector3(11.5f, 1.0f, 3.2f), H + new Vector3(1.4f, 1.9f, -2.0f), H + new Vector3(0.5f, 3.6f, 10.6f));
            Add("04_mekanizma_x", 6f, A + new Vector3(0.5f, 1.72f, -4.5f), A + new Vector3(0f, 2.8f, 8f), A + new Vector3(0.2f, 1.9f, -1.0f), A + new Vector3(0f, 3.6f, 8f), AnimTower, true);
            Add("05_mahzen_o", 6f, B + new Vector3(-29.4f, 1.72f, -1.4f), B + new Vector3(-38.75f, 1.4f, 1.6f), B + new Vector3(-32.6f, 1.6f, -0.4f), B + new Vector3(-38.75f, 1.2f, 2.2f), null, true);
            Add("06_sarnic", 7f, cz + new Vector3(4.8f, 1.72f, 0.4f), cz + new Vector3(-13f, 1.4f, -1.2f), cz + new Vector3(0.5f, 1.6f, -0.4f), cz + new Vector3(-13f, 1.2f, -1.6f), AnimCistern, true);
            Add("07a_harita", 5f, A + new Vector3(40f, 1.72f, 33.2f), A + new Vector3(40f, 3.5f, 50f), A + new Vector3(41.5f, 1.9f, 36.5f), A + new Vector3(44f, 2.2f, 50f));
            Add("07b_cati", 5f, rf + new Vector3(0, 1.82f, 11f), rf + new Vector3(2f, 1.6f, -10f), rf + new Vector3(-1.5f, 1.82f, 9f), rf + new Vector3(3f, 3f, -10f), null, true);
            Add("07c_can", 5f, bl + new Vector3(3f, 1.6f, -5f), bl + new Vector3(-2f, 8f, 3f), bl + new Vector3(2f, 1.6f, -3f), bl + new Vector3(-1f, 12f, 3f));
            Add("07d_salon", 6f, F + new Vector3(-6f, 1.7f, -24f), F + new Vector3(0f, 18f, -10f), F + new Vector3(-4f, 1.7f, -19f), F + new Vector3(0f, 21f, 0f), null, true);
            Add("08_detay_mum", 3f, H + new Vector3(-5.5f, 1.3f, -9.9f), H + new Vector3(-6.65f, 0.95f, -8.4f), H + new Vector3(-5.8f, 1.2f, -9.5f), H + new Vector3(-6.65f, 0.95f, -8.2f));
            Add("08_detay_sarap", 3f, B + new Vector3(-33.6f, 1.5f, 0.6f), B + new Vector3(-34.5f, 1.4f, 7f), B + new Vector3(-33.9f, 1.45f, 1.6f), B + new Vector3(-34.8f, 1.3f, 7f));
            Add("08_detay_masa", 3f, B + new Vector3(3.8f, 1.45f, -9.0f), B + new Vector3(5.1f, 0.72f, -10.6f), B + new Vector3(4.2f, 1.35f, -9.4f), B + new Vector3(5.1f, 0.72f, -10.7f), null, true);
            Add("08_detay_maket", 3f, A + new Vector3(46.2f, 2.3f, 45.0f), A + new Vector3(48.4f, 0.9f, 45.8f), A + new Vector3(46.4f, 2.1f, 46.4f), A + new Vector3(48.4f, 0.9f, 45.8f));
            Add("08_detay_ziyafet", 3f, F + new Vector3(2.2f, 1.45f, 7.0f), F + new Vector3(0f, 0.85f, 10.5f), F + new Vector3(1.8f, 1.3f, 8.2f), F + new Vector3(0f, 0.85f, 11.5f), null, true);
            return L;
        }

        // --- Sahne hazırlığı: çözülmüş görünüm (mahzen ışıkları, örnek semboller), dekor sabit ---
        static void PrepareProps()
        {
            var ro = GameObject.Find("RoomB_Sarnic");
            var cl = ro != null ? ro.transform.Find("OWest/CellarLit") : null;
            if (cl != null) cl.gameObject.SetActive(true);
            var cv = UnityEngine.Object.FindFirstObjectByType<CryptVisuals>();
            if (cv != null)
            {
                var so = new SerializedObject(cv);
                var mat = so.FindProperty("carveMat").objectReferenceValue as Material;
                var st = CryptPuzzle.Make(12345);
                for (int i = 0; i < CryptPuzzle.Coffins; i++)
                {
                    Glyphs.Symbol((Transform)so.FindProperty("coffinSymbols").GetArrayElementAtIndex(i).objectReferenceValue, st.coffinSymbol[i], mat, 0.5f);
                    Glyphs.Numeral((Transform)so.FindProperty("coffinNumerals").GetArrayElementAtIndex(i).objectReferenceValue, st.coffinNumeral[i], mat, 0.32f);
                }
            }
            foreach (var v in UnityEngine.Object.FindObjectsByType<CisternValve>(FindObjectsInactive.Exclude))
            {
                var so = new SerializedObject(v);
                var an = so.FindProperty("symbolAnchor").objectReferenceValue as Transform;
                if (an != null) Glyphs.Symbol(an, so.FindProperty("valveIndex").intValue, so.FindProperty("symbolMat").objectReferenceValue as Material, 0.5f);
            }
            // çatı ay kartı oyunda bulmaca durumuna göre dokulanır; kayıtta dokusuz kare görünmesin
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
                if (r.name == "Moon") r.enabled = false;
        }

        // Kule: kollar sırayla iner, dişliler döner
        static List<(Transform h, float up, float down)> levers;
        static List<(Transform t, Vector3 axis, float dps, Quaternion rot0)> spins;
        static void AnimTower(float k)
        {
            if (levers == null)
            {
                levers = new List<(Transform, float, float)>();
                foreach (var lv in UnityEngine.Object.FindObjectsByType<SyncedLever>(FindObjectsInactive.Exclude))
                {
                    if (Vector3.Distance(lv.transform.position, A) > 20f) continue;
                    var so = new SerializedObject(lv);
                    var h = so.FindProperty("handle").objectReferenceValue as Transform;
                    if (h != null) levers.Add((h, so.FindProperty("upAngle").floatValue, so.FindProperty("downAngle").floatValue));
                }
                spins = new List<(Transform, Vector3, float, Quaternion)>();
                foreach (var sp in UnityEngine.Object.FindObjectsByType<Spin>(FindObjectsInactive.Exclude))
                {
                    if (Vector3.Distance(sp.transform.position, A) > 25f) continue;
                    var so = new SerializedObject(sp);
                    spins.Add((sp.transform, so.FindProperty("axis").vector3Value, so.FindProperty("degreesPerSecond").floatValue, sp.transform.localRotation));
                }
            }
            for (int i = 0; i < levers.Count; i++)
            {
                float t = Mathf.Clamp01((k - 0.15f - i * 0.17f) / 0.12f);
                t = t * t * (3f - 2f * t);
                levers[i].h.localRotation = Quaternion.Euler(Mathf.Lerp(levers[i].up, levers[i].down, t), 0, 0);
            }
            float time = k * 6f;
            foreach (var s in spins) s.t.localRotation = s.rot0 * Quaternion.AngleAxis(s.dps * 3f * time, s.axis);
        }

        // Sarnıç: su yükselir, vana çarkı döner
        static Transform water; static float water0;
        static List<(Transform w, Quaternion r0)> wheels;
        static void AnimCistern(float k)
        {
            if (wheels == null)
            {
                var go = GameObject.Find("CisternWater");
                if (go != null) { water = go.transform; water0 = water.localPosition.y; }
                wheels = new List<(Transform, Quaternion)>();
                foreach (var v in UnityEngine.Object.FindObjectsByType<CisternValve>(FindObjectsInactive.Exclude))
                {
                    var w = new SerializedObject(v).FindProperty("wheel").objectReferenceValue as Transform;
                    if (w != null) wheels.Add((w, w.localRotation));
                }
            }
            if (water != null) { var p = water.localPosition; p.y = water0 + Mathf.SmoothStep(0f, 0.55f, k); water.localPosition = p; }
            foreach (var w in wheels) w.w.localRotation = w.r0 * Quaternion.Euler(0, 0, -k * 540f);
        }

        // --- Kayıt ---
        static float Ease(float t) => t * t * (3f - 2f * t);

        static void Pose(Camera cam, Shot s, float t, float fov)
        {
            float e = Ease(t);
            cam.transform.position = Vector3.Lerp(s.p0, s.p1, e);
            cam.transform.LookAt(Vector3.Lerp(s.l0, s.l1, e));
            cam.fieldOfView = fov;
            s.anim?.Invoke(t);
        }

        static void Record(Camera cam, Shot s, string path, int w, int h, float fov)
        {
            var attr = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps), width = (uint)w, height = (uint)h, includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High
            };
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            int n = Mathf.RoundToInt(s.dur * Fps);
            if (File.Exists(path)) File.Delete(path);
            using (var enc = new MediaEncoder(path, attr))
            {
                cam.targetTexture = rt;
                Pose(cam, s, 0f, fov); cam.Render(); // ısınma
                for (int i = 0; i < n; i++)
                {
                    Pose(cam, s, i / (float)(n - 1), fov);
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply(false);
                    enc.AddFrame(tex);
                }
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }

        static void SavePng(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
