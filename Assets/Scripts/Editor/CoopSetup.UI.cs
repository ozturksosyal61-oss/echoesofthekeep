using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Arayüz teması (koyu şato + altın): prosedürel sprite'lar, fontlar, temalı bileşenler; Lobby ve Game sahnelerinin kurulumu.
    /// Referans çözünürlük 1920x1080 (yükseklik eşlemeli); kenar öğeleri safe area içinde köşelere sabitlenir.
    /// </summary>
    public static partial class CoopSetup
    {
        /// <summary>Menüde görünen oyun adı (çalışma adı; buradan değiştir).</summary>
        const string GameTitle = "ECHOES";
        const string GameTitleSub = "OF THE KEEP";

        const string UiDir = "Assets/Generated/UI";
        const string FontDir = "Assets/ThirdParty/Fonts/";

        static readonly Color Gold = new Color(0.86f, 0.68f, 0.36f);
        static readonly Color GoldBright = new Color(0.98f, 0.86f, 0.56f);
        static readonly Color Cream = new Color(0.96f, 0.91f, 0.8f);
        static readonly Color Ink = new Color(0.05f, 0.045f, 0.06f);

        class UiKit
        {
            public Font title, head, body;
            public Sprite panel, button, buttonDark, field, ring, knob, crosshair, glow, vignette, shadeH, shadeV;
        }

        static UiKit U;

        // ---------- Sprite üretimi ----------

        static Sprite MakeSprite(string name, int w, int h, int border, System.Func<float, float, Color> pixel)
        {
            Directory.CreateDirectory(UiDir);
            string path = UiDir + "/" + name + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x + 0.5f, y + 0.5f);
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Yuvarlak köşeli dikdörtgene işaretli uzaklık (içeride negatif).</summary>
        static float RoundRect(float x, float y, float w, float h, float r)
        {
            float dx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r), dy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            return Mathf.Min(Mathf.Max(dx, dy), 0f) + new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude - r;
        }

        static float Cover(float d) => Mathf.Clamp01(0.5f - d); // d<0 içeride

        /// <summary>Çerçeveli plaka: dolgu (dikey geçiş) + dış altın çizgi + içte ince ikinci çizgi.</summary>
        static Sprite Framed(string name, int size, float radius, Color top, Color bottom, Color border, float bw, float innerLine)
        {
            return MakeSprite(name, size, size, Mathf.RoundToInt(radius + bw + 8), (x, y) =>
            {
                float d = RoundRect(x, y, size, size, radius);
                var c = Color.Lerp(bottom, top, y / size);
                if (y > size * 0.55f) c = Color.Lerp(c, Color.white, 0.035f); // üstte hafif parlama
                float b = Mathf.Clamp01(d + bw + 0.5f);
                c = Color.Lerp(c, border, b);
                if (innerLine > 0f)
                {
                    float l = Cover(Mathf.Abs(d + bw + 5f) - 0.6f) * innerLine;
                    c = Color.Lerp(c, new Color(border.r, border.g, border.b, 1f), l);
                }
                c.a *= Cover(d);
                return c;
            });
        }

        static Sprite Disc(string name, int size, System.Func<float, Color> byRadius)
        {
            return MakeSprite(name, size, size, 0, (x, y) =>
            {
                float r = new Vector2(x - size * 0.5f, y - size * 0.5f).magnitude / (size * 0.5f); // 0 merkez, 1 kenar
                return byRadius(r);
            });
        }

        static void PrepareUi()
        {
            U = new UiKit
            {
                title = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "CinzelDecorative-Bold.ttf"),
                head = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Cinzel-Bold.ttf"),
                body = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Spectral-Medium.ttf"),
            };
            if (U.title == null || U.head == null || U.body == null) Debug.LogError("[CoopSetup] Font bulunamadı: " + FontDir);

            U.panel = Framed("panel", 128, 20, new Color(0.085f, 0.075f, 0.095f, 0.9f), new Color(0.04f, 0.035f, 0.05f, 0.94f), Gold, 3f, 0.45f);
            U.button = Framed("button", 128, 18, new Color(0.62f, 0.44f, 0.17f, 1f), new Color(0.3f, 0.19f, 0.07f, 1f), GoldBright, 3f, 0.3f);
            U.buttonDark = Framed("button_dark", 128, 18, new Color(0.13f, 0.12f, 0.15f, 0.86f), new Color(0.05f, 0.045f, 0.06f, 0.9f), Gold, 2.5f, 0f);
            U.field = Framed("field", 128, 14, new Color(0.02f, 0.02f, 0.03f, 0.92f), new Color(0.06f, 0.055f, 0.07f, 0.92f), new Color(Gold.r, Gold.g, Gold.b, 0.85f), 2.5f, 0f);

            const int R = 256;
            float px = 2f / R; // bir pikselin yarıçap birimi
            U.ring = Disc("ring", R, r =>
            {
                float edge = Mathf.Clamp01((1f - r) / px);                         // dış kenar yumuşatma
                float band = Mathf.Clamp01((r - 0.94f) / px) * edge;               // dış halka
                float fill = (0.1f + 0.1f * r * r) * edge;
                var c = Color.Lerp(new Color(0f, 0f, 0f, fill * 1.6f), new Color(Gold.r, Gold.g, Gold.b, 0.9f), band);
                return c;
            });
            U.knob = Disc("knob", 128, r =>
            {
                float edge = Mathf.Clamp01((1f - r) / (2f / 128));
                var c = Color.Lerp(GoldBright, new Color(0.45f, 0.3f, 0.1f), r * r);
                c = Color.Lerp(c, new Color(1f, 0.95f, 0.8f), Mathf.Clamp01((r - 0.86f) / 0.04f) * 0.7f);
                c.a = 0.92f * edge;
                return c;
            });
            U.crosshair = Disc("crosshair", 64, r =>
            {
                float p = 2f / 64;
                float dot = Mathf.Clamp01((0.13f - r) / p);
                float ring = Mathf.Clamp01((0.09f - Mathf.Abs(r - 0.78f)) / p);
                float shadow = Mathf.Clamp01((0.2f - r) / p) * 0.35f + Mathf.Clamp01((0.16f - Mathf.Abs(r - 0.78f)) / p) * 0.3f;
                float a = Mathf.Max(dot, ring * 0.85f);
                return a > 0.01f ? new Color(1, 1, 1, a) : new Color(0, 0, 0, shadow);
            });
            U.glow = Disc("glow", 64, r => new Color(1, 1, 1, Mathf.Clamp01(1f - r) * Mathf.Clamp01((1f - r) * 3f)));
            U.vignette = MakeSprite("vignette", 256, 256, 0, (x, y) =>
            {
                float r = new Vector2((x - 128f) / 128f, (y - 128f) / 128f).magnitude;
                return new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.3f, r)) * 0.9f);
            });
            U.shadeH = MakeSprite("shade_h", 128, 4, 0, (x, y) => new Color(0, 0, 0, Mathf.SmoothStep(0.82f, 0f, x / 128f)));
            U.shadeV = MakeSprite("shade_v", 4, 128, 0, (x, y) => new Color(0, 0, 0, Mathf.SmoothStep(0f, 0.6f, y / 128f)));
        }

        // ---------- Temalı bileşenler ----------

        static Camera MakeCamera(Vector3 pos, Vector3 lookAt, Color bg)
        {
            var go = new GameObject("Camera");
            var cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.transform.position = pos;
            go.transform.LookAt(lookAt);
            go.AddComponent<AudioListener>();
            return cam;
        }

        static RectTransform MakeCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // yatayda yükseklik sabit (1080): geniş ekranda üst HUD kırpılmaz
            go.AddComponent<GraphicRaycaster>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Tüm HUD/Lobby UI'ının altına konduğu panel; anchor'ları Screen.safeArea'ya göre çalışma anında güncellenir.</summary>
        static RectTransform MakeSafeArea(RectTransform canvas)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<SafeAreaPanel>();
            return rt;
        }

        /// <summary>Öğeyi bir köşeye/kenara bağlar (pivot sabitlenir): safe area daralınca öğe içeride kalır.</summary>
        static void Corner(Graphic g, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
        }

        static void Stretch(Graphic g, Vector2 min, Vector2 max)
        {
            var rt = g.rectTransform;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static RectTransform Place(GameObject go, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Text MakeText(RectTransform parent, string name, string text, int size, Vector2 pos, Vector2 dim, TextAnchor anchor,
                             Font font = null, Color? color = null, bool shadow = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var t = go.AddComponent<Text>();
            t.font = font != null ? font : U.body;
            t.text = text; t.fontSize = size; t.alignment = anchor; t.color = color ?? Cream;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.75f);
                s.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        static Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 dim, Sprite sprite = null, Color? color = null, bool sliced = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            if (sliced) img.type = Image.Type.Sliced;
            img.color = color ?? Color.white;
            return img;
        }

        /// <summary>primary: altın dolgulu ana düğme; değilse koyu, altın çerçeveli ikincil düğme.</summary>
        static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos, Vector2 dim, bool primary = true, int fontSize = 46)
        {
            var img = MakeImage(parent, name, pos, dim, primary ? U.button : U.buttonDark, Color.white, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.97f, 0.9f);
            cb.pressedColor = new Color(0.72f, 0.68f, 0.62f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            img.gameObject.AddComponent<UiPress>();
            var t = MakeText(img.rectTransform, "Label", label, fontSize, new Vector2(0, 2), dim - new Vector2(40, 16), TextAnchor.MiddleCenter,
                             U.head, primary ? new Color(1f, 0.96f, 0.86f) : GoldBright);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 22; t.resizeTextMaxSize = fontSize;
            return btn;
        }

        static InputField MakeInput(RectTransform parent, string name, Vector2 pos, Vector2 dim)
        {
            var img = MakeImage(parent, name, pos, dim, U.field, Color.white, true);
            var field = img.gameObject.AddComponent<InputField>();
            var rt = img.rectTransform;
            var text = MakeText(rt, "Text", "", 84, Vector2.zero, dim - new Vector2(40, 0), TextAnchor.MiddleCenter, U.head, GoldBright, false);
            var ph = MakeText(rt, "Placeholder", "0000", 84, Vector2.zero, dim - new Vector2(40, 0), TextAnchor.MiddleCenter, U.head,
                              new Color(Gold.r, Gold.g, Gold.b, 0.22f), false);
            text.supportRichText = false;
            field.targetGraphic = img;
            field.textComponent = text;
            field.placeholder = ph;
            field.caretColor = GoldBright;
            field.customCaretColor = true;
            field.selectionColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
            return field;
        }

        // ---------- Ortak paneller: ayarlar, günlük, mektup okuma, oyun içi menü ----------

        static RectTransform Overlay(RectTransform parent, string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        /// <summary>Ayar satırları (başlık y0'da; konumlar a çapasına göre, pivot = çapa).</summary>
        static SettingsPanel BuildSettings(RectTransform p, Vector2 a, float x0, float y0, out Button back)
        {
            var title = MakeText(p, "SettingsTitle", "AYARLAR", 50, Vector2.zero, new Vector2(700, 70), TextAnchor.MiddleLeft, U.head, GoldBright);
            Corner(title, a, a, new Vector2(x0, y0));
            Slider Row(string label, float y, out Text val)
            {
                var l = MakeText(p, "Label_" + label, label, 32, Vector2.zero, new Vector2(330, 56), TextAnchor.MiddleLeft, U.body, Cream);
                Corner(l, a, a, new Vector2(x0, y));
                var sl = MakeSlider(p, "Slider_" + label, 380);
                Corner(sl.GetComponent<Graphic>(), a, a, new Vector2(x0 + 330, y));
                val = MakeText(p, "Value_" + label, "", 28, Vector2.zero, new Vector2(120, 56), TextAnchor.MiddleRight, U.head, Gold, false);
                Corner(val, a, a, new Vector2(x0 + 720, y));
                return sl;
            }
            Button RowButton(string label, float y)
            {
                var l = MakeText(p, "Label_" + label, label, 32, Vector2.zero, new Vector2(330, 56), TextAnchor.MiddleLeft, U.body, Cream);
                Corner(l, a, a, new Vector2(x0, y));
                var b = MakeButton(p, "Button_" + label, "-", Vector2.zero, new Vector2(240, 70), false, 28);
                Corner((Graphic)b.targetGraphic, a, a, new Vector2(x0 + 330, y + 6));
                return b;
            }
            var sMusic = Row("Müzik", y0 - 84, out var vMusic);
            var sVoice = Row("Konuşma sesi", y0 - 164, out var vVoice);
            var sLook = Row("Bakış hassasiyeti", y0 - 244, out var vLook);
            var bMic = RowButton("Mikrofon", y0 - 324);
            var bFps = RowButton("Kare hızı", y0 - 409);
            back = MakeButton(p, "SettingsBack", "GERİ", Vector2.zero, new Vector2(200, 84), false, 34);
            Corner((Graphic)back.targetGraphic, a, a, new Vector2(x0, y0 - 504));
            var sp = p.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(sp);
            SetRef(so, "music", sMusic); SetRef(so, "voice", sVoice); SetRef(so, "look", sLook);
            SetRef(so, "musicVal", vMusic); SetRef(so, "voiceVal", vVoice); SetRef(so, "lookVal", vLook);
            SetRef(so, "micButton", bMic); SetRef(so, "fpsButton", bFps);
            so.ApplyModifiedPropertiesWithoutUndo();
            return sp;
        }

        /// <summary>Günlük listesi: iki sütun x 8 satır (LoreTexts.Order), başlık + sayaç, geri düğmesi.</summary>
        static JournalPanel BuildJournal(RectTransform p, Vector2 a, float x0, float y0, out Button back)
        {
            var title = MakeText(p, "JournalTitle", "GÜNLÜK", 50, Vector2.zero, new Vector2(400, 70), TextAnchor.MiddleLeft, U.head, GoldBright);
            Corner(title, a, a, new Vector2(x0, y0));
            var count = MakeText(p, "JournalCount", "", 26, Vector2.zero, new Vector2(400, 70), TextAnchor.MiddleLeft, U.body, new Color(Gold.r, Gold.g, Gold.b, 0.8f), false);
            Corner(count, a, a, new Vector2(x0 + 270, y0));
            int n = LoreTexts.Order.Length, perCol = (n + 1) / 2;
            var rows = new Button[n]; var labels = new Text[n];
            for (int i = 0; i < n; i++)
            {
                float x = x0 + (i / perCol) * 430f, y = y0 - 76 - (i % perCol) * 48f;
                var hit = MakeImage(p, "Entry" + i, Vector2.zero, new Vector2(410, 46), null, new Color(0, 0, 0, 0));
                Corner(hit, a, a, new Vector2(x, y));
                var b = hit.gameObject.AddComponent<Button>(); b.targetGraphic = hit; b.transition = Selectable.Transition.None;
                var t = MakeText(hit.rectTransform, "Label", "", 26, Vector2.zero, new Vector2(400, 46), TextAnchor.MiddleLeft, U.body, Cream, false);
                Corner(t, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                rows[i] = b; labels[i] = t;
            }
            back = MakeButton(p, "JournalBack", "GERİ", Vector2.zero, new Vector2(200, 84), false, 34);
            Corner((Graphic)back.targetGraphic, a, a, new Vector2(x0, y0 - 96 - perCol * 48f));
            var jp = p.gameObject.AddComponent<JournalPanel>();
            var so = new SerializedObject(jp);
            SetRef(so, "count", count);
            SetRefArray(so, "rows", rows);
            SetRefArray(so, "labels", labels);
            so.ApplyModifiedPropertiesWithoutUndo();
            return jp;
        }

        /// <summary>Jenerik: ekip + üçüncü taraf varlıklar (CC-BY yazarları zorunlu; Docs/CREDITS.md ile aynı tut).</summary>
        const string CreditsBody =
            "<color=#E8C77A>Yapım, tasarım ve geliştirme</color>  OZT Digital
" +
            "<color=#E8C77A>Motor</color>  Unity · Photon Fusion & Voice

" +
            "<color=#E8C77A>Modeller (CC-BY 4.0, Sketchfab)</color>
" +
            "Bird_Amphora – GetDeadEntertainment · Armillary sphere – KRei · old church bell – vertexgrocer
" +
            "Engel Statue Scan – Refref1990 · Medieval Chandelier – Kevin.Popescu

" +
            "<color=#E8C77A>Doku, model ve gökyüzü (CC0)</color>  Poly Haven (polyhaven.com) · ambientCG (ambientcg.com)
" +
            "<color=#E8C77A>Müzik (CC0, OpenGameArt)</color>  Cathedral in the forest · Abandoned Castle – StarNinjas · Castle Cellar – Bobjt
" +
            "<color=#E8C77A>Yazı tipleri (SIL OFL 1.1)</color>  Cinzel, Cinzel Decorative, Spectral

" +
            "Oynadığın için teşekkürler.";

        static RectTransform BuildCredits(RectTransform canvas, Vector2 a, float x0, float y0, out Button back)
        {
            var p = Overlay(canvas, "CreditsPanel");
            var title = MakeText(p, "CreditsTitle", "JENERİK", 50, Vector2.zero, new Vector2(700, 70), TextAnchor.MiddleLeft, U.head, GoldBright);
            Corner(title, a, a, new Vector2(x0, y0));
            var body = MakeText(p, "CreditsBody", CreditsBody, 28, Vector2.zero, new Vector2(1300, 560), TextAnchor.UpperLeft, U.body, Cream, false);
            Corner(body, a, a, new Vector2(x0, y0 - 76));
            body.supportRichText = true; body.lineSpacing = 1.1f;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 18; body.resizeTextMaxSize = 28;
            back = MakeButton(p, "CreditsBack", "GERİ", Vector2.zero, new Vector2(200, 84), false, 34);
            Corner((Graphic)back.targetGraphic, a, a, new Vector2(x0, y0 - 660));
            p.gameObject.SetActive(false);
            return p;
        }

        /// <summary>Mektup okuma ekranı (en üstte; tam ekran karartma dokununca kapatır).</summary>
        static void BuildLetterView(RectTransform root)
        {
            var dim = MakeImage(root, "LetterView", Vector2.zero, Vector2.zero, null, new Color(0f, 0f, 0f, 0.74f));
            Stretch(dim, Vector2.zero, Vector2.one);
            var closeBtn = dim.gameObject.AddComponent<Button>(); closeBtn.targetGraphic = dim; closeBtn.transition = Selectable.Transition.None;
            var panel = MakeImage(dim.rectTransform, "LetterPanel", Vector2.zero, new Vector2(1500, 920), null, new Color(0.12f, 0.095f, 0.075f, 0.97f));
            panel.raycastTarget = false;
            var ol = panel.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.8f); ol.effectDistance = new Vector2(3f, -3f);
            var lt = MakeText(panel.rectTransform, "LetterTitle", "", 52, new Vector2(0, 380), new Vector2(1300, 76), TextAnchor.MiddleCenter, U.head, GoldBright);
            var rule = MakeImage(panel.rectTransform, "LetterRule", new Vector2(0, 330), new Vector2(560, 3), null, Gold); rule.raycastTarget = false;
            var lb = MakeText(panel.rectTransform, "LetterBody", "", 34, new Vector2(0, -20), new Vector2(1320, 640), TextAnchor.UpperLeft, U.body, Cream);
            lb.lineSpacing = 1.12f; lb.verticalOverflow = VerticalWrapMode.Truncate;
            lb.resizeTextForBestFit = true; lb.resizeTextMinSize = 22; lb.resizeTextMaxSize = 34;
            var ls = MakeText(panel.rectTransform, "LetterSign", "", 36, new Vector2(0, -372), new Vector2(1300, 60), TextAnchor.MiddleRight, U.body, Gold);
            ls.fontStyle = FontStyle.Italic;
            MakeText(panel.rectTransform, "LetterHint", "kapatmak için dokun", 24, new Vector2(0, -428), new Vector2(800, 40), TextAnchor.MiddleCenter, U.body,
                     new Color(Cream.r, Cream.g, Cream.b, 0.5f), false);
            var lv = new GameObject("LetterViewCtl").AddComponent<LetterView>();
            var lso = new SerializedObject(lv);
            SetRef(lso, "root", dim.gameObject);
            SetRef(lso, "title", lt); SetRef(lso, "body", lb); SetRef(lso, "sign", ls); SetRef(lso, "close", closeBtn);
            lso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Oyun içi menü: karartma + ortada panel; ana görünüm (Devam/Günlük/Ayarlar/Ana menü), ayarlar ve günlük görünümleri.</summary>
        static void BuildGameMenu(RectTransform root)
        {
            var dim = MakeImage(root, "GameMenu", Vector2.zero, Vector2.zero, null, new Color(0f, 0f, 0f, 0.7f));
            Stretch(dim, Vector2.zero, Vector2.one);
            var panel = MakeImage(dim.rectTransform, "MenuPanel", Vector2.zero, new Vector2(1000, 860), U.panel, new Color(1f, 1f, 1f, 0.97f), true);
            var pr = panel.rectTransform;
            var TLp = new Vector2(0f, 1f);

            var main = Overlay(pr, "Main");
            MakeText(main, "MenuTitle", "MENÜ", 60, new Vector2(0, 330), new Vector2(800, 80), TextAnchor.MiddleCenter, U.head, GoldBright);
            Divider(main, new Vector2(0, 282), 420, 0.5f);
            var resume = MakeButton(main, "ResumeButton", "DEVAM ET", new Vector2(0, 170), new Vector2(560, 104), true, 44);
            var journal = MakeButton(main, "JournalButton", "GÜNLÜK", new Vector2(0, 40), new Vector2(560, 100), false, 40);
            var settings = MakeButton(main, "SettingsButton", "AYARLAR", new Vector2(0, -85), new Vector2(560, 100), false, 40);
            var leave = MakeButton(main, "LeaveButton", "ANA MENÜYE DÖN", new Vector2(0, -230), new Vector2(560, 100), false, 34);
            MakeText(main, "MenuHint", "Oyun durmaz; arkadaşın seni bekliyor olabilir.", 24, new Vector2(0, -340), new Vector2(800, 40), TextAnchor.MiddleCenter, U.body,
                     new Color(Cream.r, Cream.g, Cream.b, 0.5f), false);

            var setV = Overlay(pr, "Settings");
            BuildSettings(setV, TLp, 70, -60, out var setBack);
            var jrnV = Overlay(pr, "Journal");
            BuildJournal(jrnV, TLp, 70, -60, out var jrnBack);

            var gm = new GameObject("GameMenuCtl").AddComponent<GameMenu>();
            var so = new SerializedObject(gm);
            SetRef(so, "root", dim.gameObject);
            SetRef(so, "mainView", main.gameObject); SetRef(so, "settingsView", setV.gameObject); SetRef(so, "journalView", jrnV.gameObject);
            SetRef(so, "resume", resume); SetRef(so, "journal", journal); SetRef(so, "settings", settings); SetRef(so, "leave", leave);
            SetRef(so, "settingsBack", setBack); SetRef(so, "journalBack", jrnBack);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Altın dolgulu ince kaydırıcı (kök: görünmez dokunma alanı).</summary>
        static Slider MakeSlider(RectTransform parent, string name, float width)
        {
            var hit = MakeImage(parent, name, Vector2.zero, new Vector2(width, 56), null, new Color(0, 0, 0, 0));
            var rt = hit.rectTransform;
            var bg = MakeImage(rt, "Background", Vector2.zero, new Vector2(0, 8), null, new Color(0f, 0f, 0f, 0.6f));
            bg.rectTransform.anchorMin = new Vector2(0, 0.5f); bg.rectTransform.anchorMax = new Vector2(1, 0.5f); bg.rectTransform.sizeDelta = new Vector2(0, 8);
            var area = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(rt, false); area.anchorMin = new Vector2(0, 0.5f); area.anchorMax = new Vector2(1, 0.5f); area.sizeDelta = new Vector2(-24, 8);
            var fill = MakeImage(area, "Fill", Vector2.zero, Vector2.zero, null, Gold);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1); fill.rectTransform.sizeDelta = new Vector2(12, 0);
            var harea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
            harea.SetParent(rt, false); harea.anchorMin = Vector2.zero; harea.anchorMax = Vector2.one; harea.sizeDelta = new Vector2(-24, 0);
            var handle = MakeImage(harea, "Handle", Vector2.zero, new Vector2(36, 36), U.knob, GoldBright);
            handle.rectTransform.anchorMin = new Vector2(0, 0.5f); handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            bg.raycastTarget = false; fill.raycastTarget = false;
            var s = hit.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            return s;
        }

        static void Divider(RectTransform parent, Vector2 pos, float width, float alpha = 0.6f)
        {
            var img = MakeImage(parent, "Divider", pos, new Vector2(width, 2f), null, new Color(Gold.r, Gold.g, Gold.b, alpha));
            img.raycastTarget = false;
        }

        // ---------- Lobby: şato kapısı önünde ana menü ----------

        const string MenuVideoPath = "Assets/Art/Menu/menu_loop.mp4";

        /// <summary>Ana menü (ref Docs/Reference/anasayfa.png): arka planda döngü video (yoksa 3B şato kapısı), solda başlık + üç seçenek,
        /// altta sekmeler, sağ altta ilerleme kartı.</summary>
        static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(new Vector3(-2.2f, 1.75f, -8.5f), new Vector3(1.2f, 3.3f, 6f), Color.black);
            cam.name = "Main Camera";
            cam.fieldOfView = 52f;
            var clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(MenuVideoPath);
            if (clip != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                var vp = cam.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                vp.clip = clip; vp.isLooping = true; vp.playOnAwake = true; vp.skipOnDrop = true;
                vp.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane; vp.targetCamera = cam; vp.targetCameraAlpha = 1f;
                vp.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitOutside;
                vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
                var cd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (cd != null) cd.renderPostProcessing = false; // video zaten renklendirilmiş
            }
            else
            {
                cam.gameObject.AddComponent<CameraDrift>();
                ApplyAtmosphere(cam, new Color(0.1f, 0.12f, 0.19f), new Color(0.05f, 0.07f, 0.12f), 0.022f);
                Moon(new Vector3(32f, 38f, 0f), 0.9f);
                BuildLobbyBackdrop();
            }

            var root = MakeCanvas();
            var vig = MakeImage(root, "Vignette", Vector2.zero, Vector2.zero, U.vignette);
            Stretch(vig, Vector2.zero, Vector2.one); vig.raycastTarget = false;
            var shade = MakeImage(root, "ShadeLeft", Vector2.zero, Vector2.zero, U.shadeH);
            Stretch(shade, Vector2.zero, new Vector2(0.6f, 1f)); shade.raycastTarget = false;

            var canvas = MakeSafeArea(root);
            var TL = new Vector2(0f, 1f); var BL = Vector2.zero; var BR = new Vector2(1f, 0f);
            var dimCream = new Color(Cream.r, Cream.g, Cream.b, 0.6f);
            const float X0 = 116f;

            // Sol kenar çizgisi + başlık
            var edge = MakeImage(canvas, "EdgeLine", Vector2.zero, new Vector2(2, 0), null, new Color(Gold.r, Gold.g, Gold.b, 0.35f));
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = new Vector2(0, 0); edge.rectTransform.anchorMax = new Vector2(0, 1);
            edge.rectTransform.pivot = new Vector2(0.5f, 0.5f); edge.rectTransform.anchoredPosition = new Vector2(70, 0); edge.rectTransform.sizeDelta = new Vector2(2, -170);
            var title = MakeText(canvas, "Title", GameTitle, 150, Vector2.zero, new Vector2(1000, 170), TextAnchor.MiddleLeft, U.title, GoldBright);
            Corner(title, TL, TL, new Vector2(X0 - 6, -44));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ts = title.GetComponent<Shadow>(); ts.effectDistance = new Vector2(4f, -5f); ts.effectColor = new Color(0, 0, 0, 0.85f);
            var title2 = MakeText(canvas, "TitleSub", "O F    T H E    K E E P", 38, Vector2.zero, new Vector2(700, 56), TextAnchor.MiddleLeft, U.head, Gold);
            Corner(title2, TL, TL, new Vector2(X0, -206));
            title2.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tl = MakeImage(canvas, "TitleLine", Vector2.zero, new Vector2(120, 2), null, Gold);
            Corner(tl, TL, new Vector2(0f, 0.5f), new Vector2(X0 + 470, -234)); tl.raycastTarget = false;

            // Menü öğeleri (şeffaf düğme + başlık + alt satır)
            Button Item(RectTransform parent, string name, string head, string sub, float y, bool hero, out Text headT, out Text subT)
            {
                var hit = MakeImage(parent, name, Vector2.zero, new Vector2(720, hero ? 128 : 112), null, new Color(0, 0, 0, 0));
                Corner(hit, TL, TL, new Vector2(X0 - 10, y));
                var b = hit.gameObject.AddComponent<Button>(); b.targetGraphic = hit; b.transition = Selectable.Transition.None;
                hit.gameObject.AddComponent<UiPress>();
                headT = MakeText(hit.rectTransform, "Head", head, hero ? 64 : 50, Vector2.zero, new Vector2(700, 72), TextAnchor.MiddleLeft, U.head, hero ? GoldBright : Cream);
                Corner(headT, TL, TL, new Vector2(10, 0));
                subT = MakeText(hit.rectTransform, "Sub", sub, hero ? 30 : 27, Vector2.zero, new Vector2(700, 44), TextAnchor.MiddleLeft, U.body, hero ? Cream : dimCream, false);
                Corner(subT, TL, TL, new Vector2(10, hero ? -74 : -64));
                if (hero) subT.fontStyle = FontStyle.Italic;
                return b;
            }
            var main = new GameObject("MainPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            main.SetParent(canvas, false); main.anchorMin = Vector2.zero; main.anchorMax = Vector2.one; main.offsetMin = main.offsetMax = Vector2.zero;
            var cont = Item(main, "ContinueButton", "DEVAM ET", "Bulmaca 1 · kaldığın yerden sürdür", -372, true, out var contHead, out var contSub);
            var dia = MakeImage(main, "Marker", Vector2.zero, new Vector2(18, 18), null, Gold); dia.raycastTarget = false;
            Corner(dia, TL, new Vector2(0.5f, 0.5f), new Vector2(70, -412)); dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var arrow = MakeText(main, "Arrow", "→", 52, Vector2.zero, new Vector2(80, 70), TextAnchor.MiddleCenter, U.body, GoldBright, false);
            Corner(arrow, TL, new Vector2(0.5f, 0.5f), new Vector2(X0 + 690, -410));
            var ul = MakeImage(main, "ContinueLine", Vector2.zero, new Vector2(700, 2), null, new Color(Gold.r, Gold.g, Gold.b, 0.55f));
            Corner(ul, TL, TL, new Vector2(X0, -500)); ul.raycastTarget = false;
            var create = Item(main, "CreateButton", "ODA KUR", "Arkadaşını davet et", -526, false, out _, out _);
            var openJoin = Item(main, "OpenJoinButton", "ODAYA KATIL", "Oda kodunu gir", -660, false, out _, out _);
            // Başlangıç seçici (test/ilerleme): küçük metin düğmesi
            var startImg = MakeImage(main, "StartButton", Vector2.zero, new Vector2(700, 46), null, new Color(0, 0, 0, 0));
            Corner(startImg, TL, TL, new Vector2(X0 - 10, -800));
            var startSel = startImg.gameObject.AddComponent<Button>(); startSel.targetGraphic = startImg; startSel.transition = Selectable.Transition.None;
            var stl = MakeText(startImg.rectTransform, "Label", "BAŞLANGIÇ: BAŞTAN", 22, Vector2.zero, new Vector2(690, 46), TextAnchor.MiddleLeft, U.head,
                               new Color(Gold.r, Gold.g, Gold.b, 0.55f), false);
            Corner(stl, TL, TL, new Vector2(10, 0));

            // Katıl paneli (menü öğelerinin yerinde)
            var joinP = new GameObject("JoinPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            joinP.SetParent(canvas, false); joinP.anchorMin = Vector2.zero; joinP.anchorMax = Vector2.one; joinP.offsetMin = joinP.offsetMax = Vector2.zero;
            var jt = MakeText(joinP, "JoinTitle", "ODA KODU", 50, Vector2.zero, new Vector2(700, 70), TextAnchor.MiddleLeft, U.head, GoldBright);
            Corner(jt, TL, TL, new Vector2(X0, -372));
            var input = MakeInput(joinP, "CodeInput", Vector2.zero, new Vector2(520, 124));
            Corner((Graphic)input.targetGraphic, TL, TL, new Vector2(X0, -460));
            var back = MakeButton(joinP, "BackButton", "GERİ", Vector2.zero, new Vector2(200, 104), false, 38);
            Corner((Graphic)back.targetGraphic, TL, TL, new Vector2(X0, -612));
            var join = MakeButton(joinP, "JoinButton", "KATIL", Vector2.zero, new Vector2(300, 104), true, 44);
            Corner((Graphic)join.targetGraphic, TL, TL, new Vector2(X0 + 220, -612));

            // Ayarlar ve günlük panelleri (menü öğelerinin yerinde)
            var setP = Overlay(canvas, "SettingsPanel");
            BuildSettings(setP, TL, X0, -296, out var sBack);
            setP.gameObject.SetActive(false);
            var jrnP = Overlay(canvas, "JournalPanel");
            BuildJournal(jrnP, TL, X0, -296, out var jBack);
            jrnP.gameObject.SetActive(false);
            var crdP = BuildCredits(canvas, TL, X0, -296, out var cBack);

            var status = MakeText(canvas, "Status", "", 26, Vector2.zero, new Vector2(760, 44), TextAnchor.MiddleLeft, U.body, Cream);
            Corner(status, TL, TL, new Vector2(X0, -852));

            // Alt: ayraç + sekmeler (mağaza yok; günlük = okunan mektuplar, ileride)
            var bl = MakeImage(canvas, "BottomLine", Vector2.zero, new Vector2(600, 2), null, new Color(Gold.r, Gold.g, Gold.b, 0.3f));
            Corner(bl, BL, BL, new Vector2(X0, 168)); bl.raycastTarget = false;
            string[] tabs = { "GÜNLÜK", "ARKADAŞLAR", "AYARLAR", "JENERİK" };
            float tx = X0;
            Button settingsTab = null, journalTab = null, creditsTab = null;
            foreach (var tab in tabs)
            {
                float w = 80 + tab.Length * 24;
                bool live = tab != "ARKADAŞLAR";
                var t = MakeText(canvas, "Tab_" + tab, tab, 32, Vector2.zero, new Vector2(w, 50), TextAnchor.MiddleLeft, U.head,
                                 live ? GoldBright : new Color(Cream.r, Cream.g, Cream.b, 0.6f));
                Corner(t, BL, BL, new Vector2(tx, 100));
                if (live)
                {
                    t.raycastTarget = true;
                    var tb = t.gameObject.AddComponent<Button>(); tb.targetGraphic = t; tb.transition = Selectable.Transition.None;
                    if (tab == "AYARLAR") settingsTab = tb; else if (tab == "JENERİK") creditsTab = tb; else journalTab = tb;
                    t.gameObject.AddComponent<UiPress>();
                }
                else
                {
                    var soon = MakeText(canvas, "Soon", "Y A K I N D A", 17, Vector2.zero, new Vector2(w, 26), TextAnchor.MiddleLeft, U.body,
                                        new Color(Gold.r, Gold.g, Gold.b, 0.5f), false);
                    Corner(soon, BL, BL, new Vector2(tx, 70));
                }
                tx += w;
            }

            // Sağ alt: ilerleme kartı
            var card = MakeImage(canvas, "ProgressCard", Vector2.zero, new Vector2(600, 190), U.panel, new Color(1f, 1f, 1f, 0.92f), true);
            Corner(card, BR, BR, new Vector2(-60, 56));
            var cr = card.rectTransform;
            MakeText(cr, "ProgressLabel", "İ L E R L E M E", 24, new Vector2(-120, 52), new Vector2(320, 40), TextAnchor.MiddleLeft, U.head, Gold, false);
            var pv = MakeText(cr, "ProgressValue", "0 / 8", 34, new Vector2(150, 52), new Vector2(220, 46), TextAnchor.MiddleRight, U.head, GoldBright, false);
            var barBg = MakeImage(cr, "BarBg", new Vector2(0, 2), new Vector2(520, 6), null, new Color(0f, 0f, 0f, 0.55f)); barBg.raycastTarget = false;
            var fill = MakeImage(barBg.rectTransform, "BarFill", Vector2.zero, new Vector2(0, 6), null, Gold); fill.raycastTarget = false;
            fill.rectTransform.anchorMin = new Vector2(0, 0.5f); fill.rectTransform.anchorMax = new Vector2(0, 0.5f); fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.anchoredPosition = Vector2.zero;
            MakeText(cr, "MicHint", "Sesli sohbet · Kulaklık önerilir", 24, new Vector2(0, -50), new Vector2(520, 40), TextAnchor.MiddleLeft, U.body, dimCream, false);

            joinP.gameObject.SetActive(false);
            var lobby = new GameObject("LobbyUI").AddComponent<LobbyUI>();
            var so = new SerializedObject(lobby);
            SetRef(so, "codeInput", input);
            SetRef(so, "createButton", create);
            SetRef(so, "joinButton", join);
            SetRef(so, "openJoinButton", openJoin);
            SetRef(so, "backButton", back);
            SetRef(so, "mainPanel", main.gameObject);
            SetRef(so, "joinPanel", joinP.gameObject);
            SetRef(so, "status", status);
            SetRef(so, "startButton", startSel);
            SetRef(so, "continueButton", cont);
            SetRef(so, "settingsButton", settingsTab);
            SetRef(so, "settingsPanel", setP.gameObject);
            SetRef(so, "settingsBack", sBack);
            SetRef(so, "journalButton", journalTab);
            SetRef(so, "journalPanel", jrnP.gameObject);
            SetRef(so, "journalBack", jBack);
            SetRef(so, "creditsButton", creditsTab);
            SetRef(so, "creditsPanel", crdP.gameObject);
            SetRef(so, "creditsBack", cBack);
            SetRef(so, "continueHead", contHead);
            SetRef(so, "continueSub", contSub);
            SetRef(so, "progressValue", pv);
            SetRef(so, "progressFill", fill.rectTransform);
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildLetterView(root);
            EditorSceneManager.SaveScene(scene, LobbyPath);
        }

        // ---------- Game: dünya + HUD ----------

        static void CreateGameScene(GameObject avatarPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera: varsayılan 1. şahıs, "Kamera" düğmesiyle 3. şahıs (yerel avatarı bulur).
            var cam = MakeCamera(new Vector3(0, 1.7f, -5), new Vector3(0, 2.5f, 7f), Color.black);
            cam.name = "Main Camera";
            cam.gameObject.AddComponent<FollowCamera>();
            new GameObject("InputRelay").AddComponent<InputRelay>();

            // Ay ışığı güney pencerelerinden kuleye girer; zindan kapalı olduğu için oraya ulaşmaz (gölge).
            ApplyAtmosphere(cam, new Color(0.17f, 0.18f, 0.26f), new Color(0.04f, 0.05f, 0.08f), 0.014f);
            Moon(new Vector3(36f, 28f, 0f), 1.7f);

            BuildWorld();

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            var sso = new SerializedObject(session);
            SetRef(sso, "avatarPrefab", avatarPrefab);
            SetRef(sso, "roomStatePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(RoomStatePath));
            sso.ApplyModifiedPropertiesWithoutUndo();

            var root = MakeCanvas();
            var TL = new Vector2(0f, 1f); var TC = new Vector2(0.5f, 1f); var TR = new Vector2(1f, 1f);

            // Bakış alanı: ekranın sağ yarısı (en arkada; safe area dışındaki dokunuşu da yakalar)
            var lookGo = new GameObject("LookPad", typeof(RectTransform));
            var lrt = lookGo.GetComponent<RectTransform>();
            lrt.SetParent(root, false);
            lrt.anchorMin = new Vector2(0.5f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            lookGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            lookGo.AddComponent<LookPad>();

            var topShade = MakeImage(root, "TopShade", Vector2.zero, Vector2.zero, U.shadeV);
            Stretch(topShade, new Vector2(0f, 0.74f), Vector2.one); topShade.raycastTarget = false;

            // Nişangâh: ekran ortası (root'ta, safe area kaymasın). Dokunmayı engellemez.
            var cross = MakeImage(root, "Crosshair", Vector2.zero, new Vector2(46, 46), U.crosshair, new Color(1, 1, 1, 0.7f));
            cross.raycastTarget = false;

            var canvas = MakeSafeArea(root);

            // Üst orta: bölge başlık kartı (birkaç saniye görünür, söner). Alt orta: lobide küçük oda kodu + yönerge.
            var role = MakeText(canvas, "RoleText", "", 60, Vector2.zero, new Vector2(1200, 80), TextAnchor.MiddleCenter, U.head, GoldBright);
            Corner(role, TC, TC, new Vector2(0, -70));
            var cardSub = MakeText(canvas, "CardSub", "", 32, Vector2.zero, new Vector2(1400, 56), TextAnchor.MiddleCenter, U.body, Cream);
            Corner(cardSub, TC, TC, new Vector2(0, -138));
            cardSub.fontStyle = FontStyle.Italic;
            var info = MakeText(canvas, "InfoText", "", 26, Vector2.zero, new Vector2(1100, 44), TextAnchor.MiddleCenter, U.body,
                                new Color(Cream.r, Cream.g, Cream.b, 0.7f));
            Corner(info, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40));
            var roomLabel = MakeText(canvas, "RoomText", "", 30, Vector2.zero, new Vector2(1100, 50), TextAnchor.MiddleCenter, U.body, Cream);
            Corner(roomLabel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 92));

            // Sol üst: FPS (uzun bas: gizli debug paneli) + konuşma göstergeleri
            var perfText = MakeText(canvas, "PerfText", "FPS -", 26, Vector2.zero, new Vector2(460, 56), TextAnchor.MiddleLeft, U.body,
                                    new Color(Cream.r, Cream.g, Cream.b, 0.7f));
            Corner(perfText, TL, TL, new Vector2(60, -30));
            perfText.raycastTarget = true;
            perfText.gameObject.AddComponent<PerfLongPress>();
            new GameObject("DebugPanel").AddComponent<DebugPanel>();
            var perf = new GameObject("PerfHud").AddComponent<PerfHud>();
            var pso = new SerializedObject(perf);
            SetRef(pso, "label", perfText);
            pso.ApplyModifiedPropertiesWithoutUndo();

            var dots = new Image[2];
            var labels = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                float y = -100 - i * 58;
                var dot = MakeImage(canvas, "SpeakDot" + i, Vector2.zero, new Vector2(46, 46), U.glow);
                Corner(dot, TL, TL, new Vector2(58, y));
                dot.raycastTarget = false;
                dots[i] = dot;
                labels[i] = MakeText(canvas, "SpeakLabel" + i, "-", 34, Vector2.zero, new Vector2(380, 50),
                                     TextAnchor.MiddleLeft, U.head, Cream);
                Corner(labels[i], TL, TL, new Vector2(118, y + 2));
            }

            // Sol alt: sanal joystick
            var stickBg = MakeImage(canvas, "Joystick", Vector2.zero, new Vector2(340, 340), U.ring);
            Corner(stickBg, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(250, 240));
            var handle = MakeImage(stickBg.rectTransform, "Handle", Vector2.zero, new Vector2(136, 136), U.knob);
            handle.raycastTarget = false;
            var js = stickBg.gameObject.AddComponent<VirtualJoystick>();
            var jso = new SerializedObject(js);
            SetRef(jso, "handle", handle.rectTransform);
            jso.ApplyModifiedPropertiesWithoutUndo();

            // Sağ üst: Çık / Kamera. Sağ alt: Etkileş (yalnızca hedef varken görünür).
            var leave = MakeButton(canvas, "LeaveButton", "MENÜ", Vector2.zero, new Vector2(230, 104), false, 38);
            Corner(leave.targetGraphic, TR, TR, new Vector2(-56, -30));
            var camBtn = MakeButton(canvas, "CameraButton", "KAMERA", Vector2.zero, new Vector2(230, 104), false, 38);
            Corner(camBtn.targetGraphic, TR, TR, new Vector2(-56, -150));
            var interactBtn = MakeButton(canvas, "InteractButton", "Etkileş", Vector2.zero, new Vector2(460, 150), true, 46);
            Corner(interactBtn.targetGraphic, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-90, 110));

            var prompt = new GameObject("InteractPrompt").AddComponent<InteractPrompt>();
            var pro = new SerializedObject(prompt);
            SetRef(pro, "button", interactBtn);
            SetRef(pro, "crosshair", cross);
            pro.ApplyModifiedPropertiesWithoutUndo();

            BuildGameMenu(root);   // MENÜ düğmesi / Esc
            BuildLetterView(root); // en üstte

            var camToggle = new GameObject("CameraToggle").AddComponent<CameraToggle>();
            var cto = new SerializedObject(camToggle);
            SetRef(cto, "button", camBtn);
            cto.ApplyModifiedPropertiesWithoutUndo();

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            var so = new SerializedObject(hud);
            SetRef(so, "roleText", role);
            SetRef(so, "infoText", info);
            SetRef(so, "roomText", roomLabel);
            SetRef(so, "cardSub", cardSub);
            SetRef(so, "leaveButton", leave);
            var dp = so.FindProperty("speakDots"); dp.arraySize = 2;
            var lp = so.FindProperty("speakLabels"); lp.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                dp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
                lp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Mahzen (bulmaca 2): kibrit düğmesi, durum, korku/ölüm karartması, ortada mesaj (en üstte)
            var matchBtn = MakeButton(canvas, "MatchButton", "KİBRİT YAK", Vector2.zero, new Vector2(400, 120), true, 40);
            Corner(matchBtn.targetGraphic, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120, 290));
            var cryptStatus = MakeText(canvas, "CryptStatus", "", 34, Vector2.zero, new Vector2(1000, 60), TextAnchor.MiddleCenter, U.body, Cream);
            Corner(cryptStatus, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60));
            var dread = MakeImage(root, "Dread", Vector2.zero, Vector2.zero, U.vignette, Color.clear);
            Stretch(dread, Vector2.zero, Vector2.one); dread.raycastTarget = false;
            var fade = MakeImage(root, "Fade", Vector2.zero, Vector2.zero, null, Color.clear);
            Stretch(fade, Vector2.zero, Vector2.one); fade.raycastTarget = false;
            var msg = MakeText(root, "CryptMessage", "", 52, Vector2.zero, new Vector2(1500, 200), TextAnchor.MiddleCenter, U.head, GoldBright);
            Corner(msg, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var ch = new GameObject("CryptHud").AddComponent<CryptHud>();
            var cso = new SerializedObject(ch);
            SetRef(cso, "matchButton", matchBtn);
            SetRef(cso, "matchLabel", matchBtn.GetComponentInChildren<Text>());
            SetRef(cso, "status", cryptStatus);
            SetRef(cso, "message", msg);
            SetRef(cso, "dread", dread);
            SetRef(cso, "fade", fade);
            SetRef(cso, "flameMat", A.flame);
            cso.ApplyModifiedPropertiesWithoutUndo();

            // Sarnıç (bulmaca 3): su altı örtüsü, nefes durumu, boğulma karartması, mesaj
            var cisStatus = MakeText(canvas, "CisternStatus", "", 34, Vector2.zero, new Vector2(1000, 60), TextAnchor.MiddleCenter, U.body, Cream);
            Corner(cisStatus, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 120));
            var under = MakeImage(root, "Underwater", Vector2.zero, Vector2.zero, null, Color.clear);
            Stretch(under, Vector2.zero, Vector2.one); under.raycastTarget = false;
            var cisFade = MakeImage(root, "CisternFade", Vector2.zero, Vector2.zero, null, Color.clear);
            Stretch(cisFade, Vector2.zero, Vector2.one); cisFade.raycastTarget = false;
            var cisMsg = MakeText(root, "CisternMessage", "", 52, Vector2.zero, new Vector2(1500, 200), TextAnchor.MiddleCenter, U.head, GoldBright);
            Corner(cisMsg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var cisHud = new GameObject("CisternHud").AddComponent<CisternHud>();
            var cho = new SerializedObject(cisHud);
            SetRef(cho, "status", cisStatus);
            SetRef(cho, "message", cisMsg);
            SetRef(cho, "under", under);
            SetRef(cho, "fade", cisFade);
            var waterGo = GameObject.Find("CisternWater");
            SetRef(cho, "water", waterGo != null ? waterGo.transform : null);
            cho.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GamePath);
        }

        // ---------- Ekran görüntüsü (görsel doğrulama; batch: -executeMethod ...CoopSetup.Shots -shotDir <klasör>) ----------

        public static void SetupAndShots()
        {
            SetupAll();
            Shots();
        }

        public static void Shots()
        {
            string dir = Arg("-shotDir") ?? "Temp/Shots";
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(LobbyPath);
            Shot(dir + "/lobby.png", null, Vector3.zero, true);
            EditorSceneManager.OpenScene(GamePath);
            var a = Layout.RoomX; var b = Layout.RoomO; var h = Layout.Hall;
            Shot(dir + "/hall_entry.png", h + new Vector3(0f, 1.72f, -9.6f), h + new Vector3(0f, 3.2f, 6f), false);
            Shot(dir + "/hall_west.png", h + new Vector3(3.5f, 1.72f, -6.5f), h + new Vector3(-8f, 2.4f, -3.5f), false);
            Shot(dir + "/hall_east.png", h + new Vector3(-3.5f, 1.72f, -7.5f), h + new Vector3(9f, 2.2f, -2.5f), false);
            Shot(dir + "/hall_stairs.png", h + new Vector3(-2.5f, 1.72f, -1.5f), h + new Vector3(1.5f, 4.2f, 10f), false);
            Shot(dir + "/hall_plateO.png", h + new Vector3(4.2f, 1.72f, 0.4f), h + new Vector3(11.5f, 1.0f, 3.2f), false);
            Shot(dir + "/hall_plateX.png", h + new Vector3(-3.2f, 4.72f, 8.7f), h + new Vector3(0.5f, 3.6f, 10.6f), false);
            Shot(dir + "/hall_back.png", h + new Vector3(0f, 4.72f, 8.4f), h + new Vector3(0f, 2.5f, -6f), false);
            Shot(dir + "/hall_hud.png", h + new Vector3(0f, 1.72f, -8.5f), h + new Vector3(0f, 2.6f, 4f), true);
            Shot(dir + "/x_levers.png", a + new Vector3(0.5f, 1.72f, -3.5f), a + new Vector3(0, 2.6f, 8f), false);
            Shot(dir + "/x_windows.png", a + new Vector3(4.5f, 1.72f, 1f), a + new Vector3(-8f, 2.2f, 1.5f), false);
            Shot(dir + "/x_bench.png", a + new Vector3(0.5f, 1.72f, -1.5f), a + new Vector3(8f, 1.3f, 2.2f), false);
            Shot(dir + "/x_south.png", a + new Vector3(0f, 1.72f, 4.5f), a + new Vector3(0f, 1.8f, -8f), false);
            Shot(dir + "/x_hud.png", a + new Vector3(-2f, 1.72f, 2.5f), a + new Vector3(-1.5f, 1.5f, 7f), true);
            Shot(dir + "/o_fresco.png", b + new Vector3(4.5f, 1.72f, -0.5f), b + new Vector3(12.9f, 2f, 0.3f), false);
            Shot(dir + "/o_vault.png", b + new Vector3(2.5f, 1.72f, -1.5f), b + new Vector3(-8f, 1.5f, 2.5f), false);
            {   // Kasa rotundası (merkez zindan yerel x -17.5)
                var v = b + new Vector3(VtCx, 0, 0);
                Shot(dir + "/o_vt_entry.png", v + new Vector3(8.2f, 1.72f, -0.6f), v + new Vector3(-6f, 3.2f, 1.5f), false);
                Shot(dir + "/o_vt_dome.png", v + new Vector3(4.5f, 1.6f, -4.5f), v + new Vector3(-1f, 10f, 1.5f), false);
                Shot(dir + "/o_vt_table.png", v + new Vector3(5.6f, 1.72f, -4.6f), v + new Vector3(2.4f, 0.9f, -1.4f), false);
                Shot(dir + "/o_vt_bay.png", v + new Vector3(-1.5f, 1.72f, -2.5f), v + new Vector3(0.5f, 1.3f, -10f), false);
                Shot(dir + "/o_vt_west.png", v + new Vector3(3f, 1.72f, 2.5f), v + new Vector3(-9f, 1.6f, -1.5f), false);
                Shot(dir + "/o_vt_gallery.png", v + new Vector3(-2.2f, VtGal + 1.72f, 7.2f), v + new Vector3(3f, 3.4f, -6f), false);
                Shot(dir + "/o_vt_stair.png", v + new Vector3(3.5f, 1.72f, 0.5f), v + new Vector3(-1.6f, 2.2f, 4.3f), false);
                Shot(dir + "/o_vt_exit.png", v + new Vector3(1.6f, VtGal + 1.72f, 8.0f), v + new Vector3(-1.6f, VtGal + 0.4f, 6.2f), false);
                Shot(dir + "/o_vt_exit2.png", v + new Vector3(-1.6f, VtGal + 1.72f, 8.3f), v + new Vector3(-1.6f, 3.4f, 4.3f), false);
            }
            Shot(dir + "/o_dg_entry.png", b + new Vector3(1.8f, 1.72f, -11f), b + new Vector3(0.5f, 2.6f, 4f), false);
            Shot(dir + "/o_dg_cells.png", b + new Vector3(-1.5f, 1.72f, 3.5f), b + new Vector3(0.5f, 1.4f, 12f), false);
            Shot(dir + "/o_dg_back.png", b + new Vector3(1.5f, 1.72f, 8f), b + new Vector3(-2.5f, 1.8f, -12f), false);
            Shot(dir + "/o_dg_watch.png", b + new Vector3(-6.5f, 4.32f, -11.5f), b + new Vector3(3f, 1.5f, 2f), false);
            Shot(dir + "/o_dg_east.png", b + new Vector3(-3f, 1.72f, -3f), b + new Vector3(8f, 1.6f, 6f), false);
            Shot(dir + "/o_dg_desk.png", b + new Vector3(2.2f, 1.72f, -6.5f), b + new Vector3(6.5f, 0.9f, -11f), false);
            Shot(dir + "/x_clock.png", a + new Vector3(-17.5f, -2.28f, 1.5f), a + new Vector3(-26f, -2.2f, 0), false);
            Shot(dir + "/x_hazne.png", a + new Vector3(-17f, -5.28f, -14f), a + new Vector3(-20f, -5.5f, -21.5f), false);
            Shot(dir + "/x_archive.png", a + new Vector3(40f, 1.72f, -9.5f), a + new Vector3(40f, 3.5f, 6f), false);
            Shot(dir + "/x_archive_back.png", a + new Vector3(37f, 5.72f, 9f), a + new Vector3(42f, 1.5f, -8f), false);
            Shot(dir + "/o_library.png", b + new Vector3(40f, 1.72f, 12f), b + new Vector3(40f, 2.5f, -12f), false);
            Shot(dir + "/o_library_back.png", b + new Vector3(42f, 1.72f, -9f), b + new Vector3(37f, 2.5f, 10f), false);
            Shot(dir + "/o_library_aisle.png", b + new Vector3(34.6f, 1.72f, -12.5f), b + new Vector3(33f, 1.4f, 4f), false);
            Shot(dir + "/o_library_east.png", b + new Vector3(41.5f, 1.72f, 2f), b + new Vector3(48.5f, 1.3f, -9f), false);
            Shot(dir + "/x_maproom.png", a + new Vector3(40f, 1.72f, 33.5f), a + new Vector3(40f, 4f, 50f), false);
            {   // Çatı terası (kule yerel 110,35,-90; ±7 x ±14) ve çan dairesi (kule yerel 95,0,70; ±9)
                var rf = a + new Vector3(110f, 35f, -90f);
                Shot(dir + "/x_roof_entry.png", rf + new Vector3(0.6f, 1.82f, -12.6f), rf + new Vector3(0, 1.5f, 6f), false);
                Shot(dir + "/x_roof_back.png", rf + new Vector3(0, 1.82f, 11f), rf + new Vector3(2f, 1.2f, -10f), false);
                var bl = a + new Vector3(95f, 0, 70f);
                Shot(dir + "/x_bell_entry.png", bl + new Vector3(0, 1.72f, -7.4f), bl + new Vector3(0, 4f, 6f), false);
                Shot(dir + "/x_bell_up.png", bl + new Vector3(3f, 1.6f, -5f), bl + new Vector3(-2f, 10f, 3f), false);
                Shot(dir + "/x_bell_dial.png", bl + new Vector3(3.4f, 1.72f, 6.2f), bl + new Vector3(6.9f, 1.8f, 6.0f), false);
            }
            Shot(dir + "/x_maproom_entry.png", a + new Vector3(40f, 1.72f, 33.6f), a + new Vector3(40f, 2.5f, 46f), false);
            Shot(dir + "/x_maproom_se.png", a + new Vector3(44f, 1.72f, 36f), a + new Vector3(48.6f, 0.8f, 34f), false);
            Shot(dir + "/x_maproom_model.png", a + new Vector3(46.2f, 2.3f, 45.8f), a + new Vector3(48.4f, 0.9f, 45.8f), false);
            Shot(dir + "/o_catacomb_ante.png", b + new Vector3(34.4f, 1.72f, -65.2f), b + new Vector3(31f, 1.4f, -58f), false);
            Shot(dir + "/o_catacomb_camp.png", b + new Vector3(37f, 1.72f, -61f), b + new Vector3(30.2f, 0.6f, -60.8f), false);
            {   // Kemik org kriptası (zindan yerel 90,0,60; iç x ±8, z ±15)
                var oc = b + new Vector3(90f, 0, 60f);
                Shot(dir + "/o_organ_entry.png", oc + new Vector3(0, 1.72f, -13.4f), oc + new Vector3(0, 2.4f, 10f), false);
                Shot(dir + "/o_organ_niche.png", oc + new Vector3(4.2f, 1.72f, -0.5f), oc + new Vector3(7.9f, 1.7f, 1.6f), false);
                var wl = b + new Vector3(90f, 0, 0); // kuyu odası (R 8)
                Shot(dir + "/o_well.png", wl + new Vector3(0, 1.72f, -6.6f), wl + new Vector3(0, 1.0f, 6f), false);
                Shot(dir + "/o_well_up.png", wl + new Vector3(3f, 1.6f, -4f), wl + new Vector3(-2f, 5.5f, 3f), false);
                var ct = b + new Vector3(40f, 0, -48f); // katakomb (labirent 19.2)
                Shot(dir + "/o_catacomb_in.png", ct + new Vector3(-1.2f, 1.72f, -8.4f), ct + new Vector3(-1.2f, 1.5f, 0f), false);
                Shot(dir + "/o_catacomb_wall.png", ct + new Vector3(-6f, 1.72f, -4f), ct + new Vector3(-9.6f, 1.5f, -2f), false);
            }
            {   // Saat odası v2 (kök kule yerel -18,-4,0)
                var ck = a + new Vector3(-18f, -4f, 0);
                Shot(dir + "/x_ck_entry.png", ck + new Vector3(0.6f, 1.72f, 0f), ck + new Vector3(-9.5f, 3.6f, 2.6f), false);
                Shot(dir + "/x_ck_north.png", ck + new Vector3(-3f, 1.72f, 0f), ck + new Vector3(-5f, 1.5f, 10f), false);
                Shot(dir + "/x_ck_alcove.png", ck + new Vector3(-11f, 1.72f, 5.5f), ck + new Vector3(-18f, 2.2f, -0.5f), false);
                Shot(dir + "/x_ck_south.png", ck + new Vector3(-3f, 1.72f, 6f), ck + new Vector3(-7f, 1.8f, -5f), false);
            }
            Shot(dir + "/x_clock_ne.png", a + new Vector3(-23.5f, -2.28f, -3.5f), a + new Vector3(-17.5f, -2.5f, 3.5f), false);
            Shot(dir + "/x_clock_s.png", a + new Vector3(-21f, -2.28f, 3.5f), a + new Vector3(-22f, -2.6f, -5f), false);
            {   // Hazne v2 (kök kule yerel -18.5,-7,-17)
                var hz = a + new Vector3(-18.5f, -7f, -17f);
                Shot(dir + "/x_hz_entry.png", hz + new Vector3(0, 1.72f, 4.2f), hz + new Vector3(-2f, 2f, -8f), false);
                Shot(dir + "/x_hz_west.png", hz + new Vector3(-1f, 1.72f, 3.5f), hz + new Vector3(-12f, 2f, 0f), false);
                Shot(dir + "/x_hz_chart.png", hz + new Vector3(-8.5f, 1.72f, -0.5f), hz + new Vector3(-12f, 2.2f, 0f), false);
                Shot(dir + "/x_hz_up.png", hz + new Vector3(1.5f, 1.6f, 2f), hz + new Vector3(-4f, 6f, -6f), false);
            }
            Shot(dir + "/x_hazne_w.png", a + new Vector3(-15f, -5.28f, -16f), a + new Vector3(-23.5f, -5f, -18f), false);
            Shot(dir + "/x_hazne_e.png", a + new Vector3(-22f, -5.28f, -14.5f), a + new Vector3(-14f, -5.6f, -18.5f), false);
            foreach (var cv in Object.FindObjectsByType<CisternValve>(FindObjectsInactive.Exclude))
            {   // Sarnıç: örnek vana sembolleri; sahne kaydedilmez
                var so = new SerializedObject(cv);
                var an = so.FindProperty("symbolAnchor").objectReferenceValue as Transform;
                if (an != null) Glyphs.Symbol(an, so.FindProperty("valveIndex").intValue, so.FindProperty("symbolMat").objectReferenceValue as Material, 0.5f);
            }
            {   // Sarnıç v3: kök b + (-50, 0, 0), iç x -13.75..5.75
                var cz = b + new Vector3(-50f, 0, 0);
                Shot(dir + "/o_cistern.png", cz + new Vector3(4.8f, 1.72f, 0.4f), cz + new Vector3(-13f, 1.6f, -1.2f), false);
                Shot(dir + "/o_cistern_back.png", cz + new Vector3(-12.2f, 1.72f, 2.5f), cz + new Vector3(3f, 1.2f, -5f), false);
                Shot(dir + "/o_cistern_valves.png", cz + new Vector3(-12.4f, 1.72f, -0.6f), cz + new Vector3(-10.3f, 1.4f, -1.8f), false);
                Shot(dir + "/o_cistern_up.png", cz + new Vector3(-1f, 1.4f, -5f), cz + new Vector3(-6f, 5.5f, 3f), false);
                Shot(dir + "/o_cistern_dais.png", cz + new Vector3(-1.5f, 1.72f, 3.5f), cz + new Vector3(4.5f, 1f, 8f), false);
                Shot(dir + "/o_cistern_statue.png", cz + new Vector3(-1.5f, 1.72f, 0.3f), cz + new Vector3(-10.5f, 0.6f, 5.5f), false);
            }
            {   // Mahzen: çözülmüş hali (ışıklar açık) + örnek semboller; sahne kaydedilmez
                var ro = GameObject.Find("RoomB_Sarnic");
                var cl = ro != null ? ro.transform.Find("OWest/CellarLit") : null;
                if (cl != null) cl.gameObject.SetActive(true);
                var cv = Object.FindFirstObjectByType<CryptVisuals>();
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
                Shot(dir + "/o_cellar.png", b + new Vector3(-29.9f, 1.72f, -0.9f), b + new Vector3(-38.75f, 1.55f, 1.6f), false);
                Shot(dir + "/o_cellar_back.png", b + new Vector3(-37.6f, 1.72f, 2.2f), b + new Vector3(-30.5f, 0.9f, -3.2f), false);
                Shot(dir + "/o_cellar_racks.png", b + new Vector3(-33.4f, 1.72f, -1.5f), b + new Vector3(-34.5f, 1.6f, 7f), false);
                Shot(dir + "/o_cellar_press.png", b + new Vector3(-31.5f, 1.72f, 1.5f), b + new Vector3(-36.5f, 1f, -4f), false);
                Shot(dir + "/o_cellar_up.png", b + new Vector3(-31.2f, 1.6f, -5.5f), b + new Vector3(-35.5f, 3.6f, 2f), false);
            }
            Shot(dir + "/x_corner.png", a + new Vector3(-4f, 1.72f, 3.5f), a + new Vector3(-6.7f, 0.7f, 6.4f), false);
            Shot(dir + "/x_tw_entry.png", a + new Vector3(1f, 1.72f, -8.4f), a + new Vector3(1f, 4f, 8f), false);
            Shot(dir + "/x_tw_east.png", a + new Vector3(-3f, 1.72f, -2f), a + new Vector3(9.5f, 1.2f, -5f), false);
            Shot(dir + "/x_tw_cat.png", a + new Vector3(-1f, TwCat + 1.72f, 8.8f), a + new Vector3(6f, 2f, -4f), false);
            Shot(dir + "/x_tw_stair.png", a + new Vector3(2f, 1.72f, -4f), a + new Vector3(6f, 2.6f, 3f), false);
            Shot(dir + "/x_tw_back.png", a + new Vector3(2f, 1.72f, 6f), a + new Vector3(1f, 1.2f, -10f), false);
            Debug.Log("[CoopSetup] Shots -> " + Path.GetFullPath(dir));
        }

        static void Shot(string path, Vector3? pos, Vector3 lookAt, bool ui)
        {
            var filter = Arg("-shotFilter"); // virgüllü önekler: yalnızca eşleşen görüntüler
            if (!string.IsNullOrEmpty(filter))
            {
                bool hit = false;
                foreach (var f in filter.Split(',')) if (Path.GetFileName(path).StartsWith(f)) hit = true;
                if (!hit) return;
            }
            const int W = 1600, H = 720;
            var cam = Camera.main;
            if (pos.HasValue) { cam.transform.position = pos.Value; cam.transform.LookAt(lookAt); }
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!ui) { cv.enabled = false; continue; }
                cv.enabled = true;
                cv.renderMode = RenderMode.ScreenSpaceCamera;
                cv.worldCamera = cam;
                cv.planeDistance = 0.5f;
            }
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.Render(); // ilk karede dinamik font atlası dolar
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
