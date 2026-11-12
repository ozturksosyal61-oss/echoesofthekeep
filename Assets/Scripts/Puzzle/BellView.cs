using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 7 görsel + ses + arayüz (iki tarafta, tohumdan/durumdan): çanlardaki, kitabedeki, org tuşlarındaki ve kadrandaki
    /// nota simgeleri; kadran kapağı ve ibresi; sentezlenmiş çan/org/uyumsuzluk sesleri (kuyu şaftından aşağı: çanlar kriptada da duyulur);
    /// çan dairesi ve kriptadaki oyuncuya ilerleme yazısı ve mesajlar.
    /// </summary>
    public class BellView : MonoBehaviour
    {
        public static BellView Instance { get; private set; }

        [SerializeField] BellPuzzle puzzle;
        [SerializeField] Transform[] bellGlyphs = new Transform[4];  // X: çan üstündeki nota levhaları
        [SerializeField] Transform[] scriptGlyphs = new Transform[4];// O: org kitabesi
        [SerializeField] Transform[] keyGlyphs = new Transform[6];   // O: org tuşları
        [SerializeField] Transform[] dialGlyphs = new Transform[4];  // X: kadran pencereleri
        [SerializeField] GameObject dialCover;                       // X: kadran kapağı (aşama 0'da kapalı)
        [SerializeField] Transform dialHand;
        [SerializeField] Material inkMat, boneInkMat;
        [SerializeField] float glyphSize = 0.22f;
        [SerializeField] Font font;
        [SerializeField] Vector3 chamberCenter, chamberSize, cryptCenter, cryptSize; // dünya

        AudioSource source;
        int shownSeed = -1, shownDial = -1, seenErrors = -1, seenStage = -1;
        PuzzleState seenState;
        Text info, message;
        float messageUntil, handAngle;

        static readonly float[] BellHz = { 523.25f, 440f, 349.23f, 261.63f };
        static readonly float[] KeyHz = { 293.66f, 329.63f, 392f, 440f, 523.25f, 587.33f };
        static AudioClip[] bellClips, keyClips;
        static AudioClip greatClip, discordClip;

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0f;
            if (bellClips == null)
            {
                bellClips = new AudioClip[BellHz.Length];
                for (int i = 0; i < BellHz.Length; i++) bellClips[i] = BellClip(BellHz[i], 4f);
                greatClip = BellClip(130.81f, 6.5f);
                keyClips = new AudioClip[KeyHz.Length];
                for (int i = 0; i < KeyHz.Length; i++) keyClips[i] = OrganClip(KeyHz[i]);
                discordClip = Discord();
            }
            var canvas = new GameObject("BellHud", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var cs = canvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1f;
            info = Txt(canvas.transform, new Vector2(0.5f, 0.82f), 30);
            message = Txt(canvas.transform, new Vector2(0.5f, 0.62f), 40);
        }

        Text Txt(Transform parent, Vector2 anchor, int size)
        {
            var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = anchor; t.rectTransform.sizeDelta = new Vector2(1400, 120);
            t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(0.93f, 0.86f, 0.7f); t.raycastTarget = false;
            return t;
        }

        void Say(string t, float sec) { message.text = t; messageUntil = Time.time + sec; }

        int Where()
        {
            var me = PlayerAvatar.Local;
            if (me == null) return 0;
            var p = me.transform.position + Vector3.up * 0.5f;
            if (new Bounds(chamberCenter, chamberSize).Contains(p)) return 1;
            if (new Bounds(cryptCenter, cryptSize).Contains(p)) return 2;
            return 0;
        }

        /// <summary>Düğüm vuruldu (her iki tarafta). Çanlar kriptada şafttan boğuk duyulur, org çan dairesinde hafif.</summary>
        public void Play(BellNode.Kind kind, int index, Vector3 at)
        {
            int w = Where();
            if (w == 0 || source == null) return;
            if (kind == BellNode.Kind.Key) source.PlayOneShot(keyClips[Mathf.Clamp(index, 0, keyClips.Length - 1)], w == 2 ? 0.8f : 0.3f);
            else source.PlayOneShot(kind == BellNode.Kind.GreatBell ? greatClip : bellClips[Mathf.Clamp(index, 0, bellClips.Length - 1)], w == 1 ? 0.9f : 0.5f);
        }

        void Redraw(BellPuzzle.Setup s, bool full)
        {
            if (full)
            {
                for (int i = 0; i < bellGlyphs.Length; i++) if (bellGlyphs[i] != null) { Notes.Clear(bellGlyphs[i]); Notes.Draw(bellGlyphs[i], s.bellNote[i], inkMat, glyphSize); }
                for (int i = 0; i < scriptGlyphs.Length; i++) if (scriptGlyphs[i] != null) { Notes.Clear(scriptGlyphs[i]); Notes.Draw(scriptGlyphs[i], s.InscriptionNote(i), boneInkMat, glyphSize * 1.2f); }
                for (int i = 0; i < keyGlyphs.Length; i++) if (keyGlyphs[i] != null) { Notes.Clear(keyGlyphs[i]); Notes.Draw(keyGlyphs[i], s.keyNote[i], boneInkMat, glyphSize * 0.6f); }
            }
            for (int i = 0; i < dialGlyphs.Length; i++) if (dialGlyphs[i] != null) { Notes.Clear(dialGlyphs[i]); Notes.Draw(dialGlyphs[i], s.dial[i], inkMat, glyphSize * 0.8f); }
        }

        void Update()
        {
            if (puzzle == null || info == null) return;
            int seed = puzzle.Seed, dial = puzzle.DialSeed;
            if (seed != 0 && (seed != shownSeed || dial != shownDial))
            {
                Redraw(BellPuzzle.Make(seed, dial), seed != shownSeed);
                shownSeed = seed; shownDial = dial;
            }
            var st = puzzle.PuzzleState;
            var stage = puzzle.CurrentStage;
            bool open = st == PuzzleState.Solved || (st == PuzzleState.Active && stage != BellPuzzle.Stage.Bells);
            if (dialCover != null && dialCover.activeSelf == open) dialCover.SetActive(!open);
            if (dialHand != null)
            {
                int p = Mathf.Clamp(stage == BellPuzzle.Stage.Bells ? 0 : puzzle.Progress, 0, BellPuzzle.DialLen - 1);
                float target = -p * 90f;
                handAngle = Mathf.MoveTowardsAngle(handAngle, target, 120f * Time.deltaTime);
                dialHand.localRotation = Quaternion.Euler(0, 0, handAngle);
            }

            int w = Where();
            bool show = w != 0 && st == PuzzleState.Active && seed != 0;
            int errors = puzzle.Errors, prog = puzzle.Progress;
            if (seenErrors >= 0 && errors > seenErrors && w != 0)
            {
                source.PlayOneShot(discordClip, 0.7f);
                Say(stage == BellPuzzle.Stage.Bells ? "Çanlar uyumsuz çaldı. Dizi başa döndü."
                    : w == 1 ? "Kadranın dişlileri döndü: yeni bir dizi belirdi." : "Org inledi. Kemikler yer değiştirdi gibi... dizi bozuldu.", 4.5f);
            }
            else if (show && seenStage >= 0 && (int)stage != seenStage)
            {
                if (stage == BellPuzzle.Stage.Organ && seenStage == 0) Say(w == 1 ? "Dört çan bir ağızdan yankılandı. Kadranın kapağı açıldı." : "Yukarıda dört çan bir ağızdan yankılandı.", 4.5f);
                else if (stage == BellPuzzle.Stage.Final) Say("Son nota: büyük çan ve org AYNI ANDA. Sayın: 3-2-1!", 5f);
            }
            if (st != seenState)
            {
                if (st == PuzzleState.Solved && seenState == PuzzleState.Active && w != 0) Say("Çan ve org tek ses oldu. Kafes asansör aşağı iniyor...", 6f);
                seenState = st;
            }
            seenErrors = errors; seenStage = (int)stage;
            if (Time.time > messageUntil) message.text = "";
            if (!show) { info.text = ""; return; }
            info.text = stage == BellPuzzle.Stage.Bells ? "Çanlar " + prog + "/" + BellPuzzle.Inscription
                      : stage == BellPuzzle.Stage.Organ ? "Org " + prog + "/" + (BellPuzzle.DialLen - 1)
                      : "Son nota · 3-2-1";
        }

        // ---------- Ses sentezi (dosya yok) ----------
        const int Rate = 44100;

        static AudioClip BellClip(float f, float len)
        {
            int n = (int)(Rate * len);
            var d = new float[n];
            float[] ratio = { 0.5f, 1f, 1.19f, 1.5f, 2f, 2.52f, 3.01f };
            float[] amp = { 0.35f, 0.5f, 0.3f, 0.18f, 0.22f, 0.1f, 0.07f };
            float[] dec = { 0.45f, 0.7f, 1.0f, 1.3f, 1.6f, 2.4f, 3.2f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, v = 0f;
                for (int k = 0; k < ratio.Length; k++) v += Mathf.Sin(2f * Mathf.PI * f * ratio[k] * t + k) * amp[k] * Mathf.Exp(-t * dec[k] * (220f / Mathf.Max(f, 110f)) * 0.9f);
                d[i] = Mathf.Clamp(v * Mathf.Clamp01(t * 300f) * 0.6f, -1f, 1f);
            }
            var c = AudioClip.Create("Bell" + (int)f, n, 1, Rate, false); c.SetData(d, 0); return c;
        }

        static AudioClip OrganClip(float f)
        {
            const float len = 2.2f;
            int n = (int)(Rate * len);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.06f) * (t > 1.6f ? Mathf.Clamp01(1f - (t - 1.6f) / 0.6f) : 1f);
                float trem = 1f + 0.04f * Mathf.Sin(t * 2f * Mathf.PI * 5.5f);
                float v = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.3f + Mathf.Sin(6f * Mathf.PI * f * t) * 0.18f
                        + Mathf.Sin(Mathf.PI * f * t) * 0.25f + Mathf.Sin(8f * Mathf.PI * f * t) * 0.08f;
                d[i] = Mathf.Clamp(v * env * trem * 0.35f, -1f, 1f);
            }
            var c = AudioClip.Create("Organ" + (int)f, n, 1, Rate, false); c.SetData(d, 0); return c;
        }

        static AudioClip Discord()
        {
            const float len = 1.8f;
            int n = (int)(Rate * len);
            var d = new float[n];
            float[] fs = { 98f, 103.8f, 138.6f, 146.8f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, v = 0f;
                foreach (var f in fs) v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(6f * Mathf.PI * f * t);
                d[i] = Mathf.Clamp(v * 0.12f * Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-t * 1.6f), -1f, 1f);
            }
            var c = AudioClip.Create("Discord", n, 1, Rate, false); c.SetData(d, 0); return c;
        }
    }
}
