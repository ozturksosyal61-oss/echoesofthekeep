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
        const string GameSubtitle = "İki ses. Tek şato.";

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

        static void Divider(RectTransform parent, Vector2 pos, float width, float alpha = 0.6f)
        {
            var img = MakeImage(parent, "Divider", pos, new Vector2(width, 2f), null, new Color(Gold.r, Gold.g, Gold.b, alpha));
            img.raycastTarget = false;
        }

        // ---------- Lobby: şato kapısı önünde ana menü ----------

        static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(new Vector3(-2.2f, 1.75f, -8.5f), new Vector3(1.2f, 3.3f, 6f), Color.black);
            cam.name = "Main Camera";
            cam.fieldOfView = 52f;
            cam.gameObject.AddComponent<CameraDrift>();
            ApplyAtmosphere(cam, new Color(0.1f, 0.12f, 0.19f), new Color(0.05f, 0.07f, 0.12f), 0.022f);
            Moon(new Vector3(32f, 38f, 0f), 0.9f);
            BuildLobbyBackdrop();

            var root = MakeCanvas();
            var vig = MakeImage(root, "Vignette", Vector2.zero, Vector2.zero, U.vignette);
            Stretch(vig, Vector2.zero, Vector2.one); vig.raycastTarget = false;
            var shade = MakeImage(root, "ShadeLeft", Vector2.zero, Vector2.zero, U.shadeH);
            Stretch(shade, Vector2.zero, new Vector2(0.62f, 1f)); shade.raycastTarget = false;

            var canvas = MakeSafeArea(root);
            var L = new Vector2(0f, 0.5f);

            // Sol: başlık bloğu
            var title = MakeText(canvas, "Title", GameTitle, 168, Vector2.zero, new Vector2(1100, 190), TextAnchor.MiddleLeft, U.title, GoldBright);
            Corner(title, L, L, new Vector2(104, 230));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ts = title.GetComponent<Shadow>(); ts.effectDistance = new Vector2(4f, -5f); ts.effectColor = new Color(0, 0, 0, 0.85f);
            var title2 = MakeText(canvas, "TitleSub", GameTitleSub, 62, Vector2.zero, new Vector2(1100, 80), TextAnchor.MiddleLeft, U.head, Gold);
            Corner(title2, L, L, new Vector2(116, 112));
            title2.horizontalOverflow = HorizontalWrapMode.Overflow;
            var line = MakeImage(canvas, "TitleLine", Vector2.zero, new Vector2(520, 3), null, Gold);
            Corner(line, L, L, new Vector2(116, 56)); line.raycastTarget = false;
            var sub = MakeText(canvas, "Subtitle", GameSubtitle, 46, Vector2.zero, new Vector2(1000, 70), TextAnchor.MiddleLeft, U.body, Cream);
            Corner(sub, L, L, new Vector2(116, 6));

            // Alt sol: ana menü sekmeleri (Oyna aktif; diğerleri ileride)
            string[] tabs = { "OYNA", "MAĞAZA", "ARKADAŞLAR", "AYARLAR" };
            float tx = 116;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == 0;
                float w = 70 + tabs[i].Length * 26;
                var tab = MakeText(canvas, "Tab_" + tabs[i], tabs[i], 36, Vector2.zero, new Vector2(w, 56), TextAnchor.MiddleLeft, U.head,
                                   on ? GoldBright : new Color(Cream.r, Cream.g, Cream.b, 0.45f));
                Corner(tab, Vector2.zero, Vector2.zero, new Vector2(tx, 92));
                if (on)
                {
                    var ul = MakeImage(canvas, "TabLine", Vector2.zero, new Vector2(w - 70, 3), null, Gold);
                    Corner(ul, Vector2.zero, Vector2.zero, new Vector2(tx, 82)); ul.raycastTarget = false;
                }
                else
                {
                    var soon = MakeText(canvas, "Soon", "YAKINDA", 18, Vector2.zero, new Vector2(w, 26), TextAnchor.MiddleLeft, U.body,
                                        new Color(Gold.r, Gold.g, Gold.b, 0.5f), false);
                    Corner(soon, Vector2.zero, Vector2.zero, new Vector2(tx, 64));
                }
                tx += w;
            }

            // Sağ: kompakt oyna kartı
            var panel = MakeImage(canvas, "Panel", Vector2.zero, new Vector2(600, 560), U.panel, Color.white, true);
            Corner(panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-104, -10));
            var p = panel.rectTransform;
            // Ana panel: iki büyük seçenek
            var main = new GameObject("MainPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            main.SetParent(p, false); main.sizeDelta = new Vector2(600, 330); main.anchoredPosition = new Vector2(0, 80);
            var create = MakeButton(main, "CreateButton", "ODA KUR", new Vector2(0, 112), new Vector2(500, 112), true, 48);
            var openJoin = MakeButton(main, "OpenJoinButton", "ODAYA KATIL", new Vector2(0, -16), new Vector2(500, 112), false, 44);
            var startSel = MakeButton(main, "StartButton", "BAŞLANGIÇ: BAŞTAN", new Vector2(0, -122), new Vector2(500, 70), false, 24); // ilerleme / test başlangıcı
            // Katıl paneli: kod gir
            var joinP = new GameObject("JoinPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            joinP.SetParent(p, false); joinP.sizeDelta = new Vector2(600, 330); joinP.anchoredPosition = new Vector2(0, 80);
            MakeText(joinP, "JoinTitle", "ODA KODU", 40, new Vector2(0, 126), new Vector2(500, 50), TextAnchor.MiddleCenter, U.head, GoldBright);
            var input = MakeInput(joinP, "CodeInput", new Vector2(0, 30), new Vector2(500, 116));
            var back = MakeButton(joinP, "BackButton", "GERİ", new Vector2(-152, -98), new Vector2(196, 104), false, 38);
            var join = MakeButton(joinP, "JoinButton", "KATIL", new Vector2(102, -98), new Vector2(296, 104), true, 44);
            Divider(p, new Vector2(0, -116), 400, 0.35f);
            var status = MakeText(p, "Status", "", 28, new Vector2(0, -170), new Vector2(520, 80), TextAnchor.MiddleCenter, U.body, Cream);
            MakeText(p, "MicHint", "Sesli sohbet · Kulaklık önerilir", 22, new Vector2(0, -236), new Vector2(520, 34), TextAnchor.MiddleCenter, U.body,
                     new Color(Cream.r, Cream.g, Cream.b, 0.45f), false);

            joinP.gameObject.SetActive(false);
            status.text = "Yeni oda kur ya da bir odaya katıl.";
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
            so.ApplyModifiedPropertiesWithoutUndo();

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

            // Üst orta: rol, oda kodu, durum
            var role = MakeText(canvas, "RoleText", "Rol: -", 50, Vector2.zero, new Vector2(700, 70), TextAnchor.MiddleCenter, U.head, GoldBright);
            Corner(role, TC, TC, new Vector2(0, -26));
            var info = MakeText(canvas, "InfoText", "", 30, Vector2.zero, new Vector2(1200, 80), TextAnchor.UpperCenter, U.body, Cream);
            Corner(info, TC, TC, new Vector2(0, -100));
            var roomLabel = MakeText(canvas, "RoomText", "", 30, Vector2.zero, new Vector2(1000, 120), TextAnchor.UpperCenter, U.body,
                                     new Color(Cream.r, Cream.g, Cream.b, 0.85f));
            Corner(roomLabel, TC, TC, new Vector2(0, -142));

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
            var leave = MakeButton(canvas, "LeaveButton", "ÇIK", Vector2.zero, new Vector2(230, 104), false, 38);
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

            var camToggle = new GameObject("CameraToggle").AddComponent<CameraToggle>();
            var cto = new SerializedObject(camToggle);
            SetRef(cto, "button", camBtn);
            cto.ApplyModifiedPropertiesWithoutUndo();

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            var so = new SerializedObject(hud);
            SetRef(so, "roleText", role);
            SetRef(so, "infoText", info);
            SetRef(so, "roomText", roomLabel);
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
            Shot(dir + "/x_maproom_model.png", a + new Vector3(46.2f, 2.3f, 45.8f), a + new Vector3(48.4f, 0.9f, 45.8f), false);
            Shot(dir + "/o_catacomb_ante.png", b + new Vector3(34f, 1.72f, -64f), b + new Vector3(32f, 1.5f, -57f), false);
            Shot(dir + "/x_clock_ne.png", a + new Vector3(-23.5f, -2.28f, -3.5f), a + new Vector3(-17.5f, -2.5f, 3.5f), false);
            Shot(dir + "/x_clock_s.png", a + new Vector3(-21f, -2.28f, 3.5f), a + new Vector3(-22f, -2.6f, -5f), false);
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
