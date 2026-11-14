using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Final (bulmaca 8) "Büyük Salon": iki oyuncu kafes asansörlerle salonun iki yarısına iner; ortadaki dövme demir parmaklık
    /// onları ayırır (ilk kez yüz yüze, parmaklıkların arasından). Her yarıda bir şövalye heykelinin kolu: ikisi de çekilince
    /// parmaklığın kapıları açılır. Mühürlü kapının önündeki iki mühür taşına birlikte basınca kapı sarsılır ama açılmaz → kapanış
    /// (2. oyunun habercisi). Değer = aşama (0 ayrı, 1 kapı açık, 2 son). Editörde tek kişi: tek kol/tek taş yeter.
    /// </summary>
    public class FinalHall : PuzzleNode
    {
        public const int Separated = 0, GateOpen = 1, Ending = 2;

        [SerializeField] SyncedLever[] levers = new SyncedLever[2];
        [SerializeField] PressurePlate[] seals = new PressurePlate[2];
        [SerializeField] Transform[] gateLeaves = new Transform[2]; // menteşe pivotları (sol/sağ)
        [SerializeField] Transform sealDoor;                         // titrer
        [SerializeField] Light doorGlow;
        [SerializeField] Font titleFont, bodyFont;
        [SerializeField] Vector3 hallCenter, hallSize;

        float gateOpen, endStart = -1f;
        int armed = -1; // ilk görüşte zaten çözülmüşse (gezinti) kapanış oynamaz
        Vector3 doorRest;
        CanvasGroup endGroup;
        Text endLine, endTitle, endSub, exitText, statsText;
        Image black;

        static bool Solo => PlayerAvatar.All.Count <= 1;

        public override void ServerFixedUpdate()
        {
            if (PuzzleState != PuzzleState.Active) return;
            int v = Value;
            if (v == Separated)
            {
                int pulled = 0;
                foreach (var l in levers) if (l != null && l.Pulled) pulled++;
                if (pulled >= 2 || (Solo && pulled >= 1)) { ServerSetValue(GateOpen, stamp: true); Dbg.Puzzle("Final: parmaklık açıldı"); }
                return;
            }
            if (v == GateOpen)
            {
                int on = 0;
                foreach (var s in seals) if (s != null && s.Pressed) on++;
                if (on >= 2 || (Solo && on >= 1)) { ServerSetValue(Ending, stamp: true); Dbg.Puzzle("Final: mühür"); ServerSolve(); }
            }
        }

        public override void ServerReset() { }

        protected override void Start()
        {
            base.Start();
            if (sealDoor != null) doorRest = sealDoor.localPosition;
            BuildEndScreen();
        }

        void BuildEndScreen()
        {
            var canvas = new GameObject("FinalEnd", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
            var cs = canvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1f;
            endGroup = canvas.GetComponent<CanvasGroup>(); endGroup.alpha = 0f; endGroup.blocksRaycasts = false; endGroup.interactable = false;
            black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(canvas.transform, false);
            black.rectTransform.anchorMin = Vector2.zero; black.rectTransform.anchorMax = Vector2.one; black.rectTransform.sizeDelta = Vector2.zero;
            black.color = new Color(0.01f, 0.01f, 0.015f, 1f); black.raycastTarget = false;
            endLine = Txt(canvas.transform, new Vector2(0.5f, 0.66f), bodyFont, 38, new Color(0.85f, 0.82f, 0.75f));
            endTitle = Txt(canvas.transform, new Vector2(0.5f, 0.5f), titleFont, 96, new Color(0.93f, 0.8f, 0.52f));
            endSub = Txt(canvas.transform, new Vector2(0.5f, 0.38f), bodyFont, 36, new Color(0.75f, 0.72f, 0.66f));
            endLine.text = "Mühür sarsıldı ama kapı açılmadı.\nArdında bir şey uyanıyor... ve sizi bekliyor.";
            endTitle.text = "ECHOES OF THE KEEP";
            endSub.text = "Kaçış başka bir gecenin hikâyesi.\nDevam edecek...";
            statsText = Txt(canvas.transform, new Vector2(0.5f, 0.27f), bodyFont, 28, new Color(0.85f, 0.82f, 0.75f, 0f));
            exitText = Txt(canvas.transform, new Vector2(0.5f, 0.16f), titleFont, 40, new Color(0.93f, 0.8f, 0.52f, 0f));
            exitText.text = "MENÜYE DÖN";
            exitText.raycastTarget = true;
            var btn = exitText.gameObject.AddComponent<Button>();
            btn.targetGraphic = exitText;
            btn.onClick.AddListener(Session.Leave);
        }

        static Text Txt(Transform parent, Vector2 anchor, Font f, int size, Color c)
        {
            var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = anchor; t.rectTransform.sizeDelta = new Vector2(1600, 260);
            t.font = f; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = c; t.raycastTarget = false;
            return t;
        }

        void Update()
        {
            int v = PuzzleState == PuzzleState.Solved ? Ending : PuzzleState == PuzzleState.Active ? Value : Separated;
            gateOpen = Mathf.MoveTowards(gateOpen, v >= GateOpen ? 1f : 0f, Time.deltaTime * 0.35f);
            float e = Mathf.SmoothStep(0f, 1f, gateOpen);
            if (gateLeaves.Length > 0 && gateLeaves[0] != null) gateLeaves[0].localRotation = Quaternion.Euler(0, -100f * e, 0);
            if (gateLeaves.Length > 1 && gateLeaves[1] != null) gateLeaves[1].localRotation = Quaternion.Euler(0, 100f * e, 0);

            var me = PlayerAvatar.Local;
            bool here = me != null && new Bounds(hallCenter, hallSize).Contains(me.transform.position);
            if (armed < 0 && PuzzleManager.Instance != null) armed = PuzzleState == PuzzleState.Solved ? 0 : 1;
            if (armed == 1 && v == Ending && endStart < 0f && here) { endStart = Time.time; statsText.text = PlayStats.Summary(PuzzleManager.Instance); }
            if (endStart < 0f) return;
            float t = Time.time - endStart;
            if (sealDoor != null)
            {
                float shake = t < 4f ? Mathf.Sin(t * 37f) * 0.025f * (1f - t / 4f) : 0f;
                sealDoor.localPosition = doorRest + new Vector3(shake, Mathf.Abs(shake) * 0.3f, 0);
            }
            if (doorGlow != null) { doorGlow.enabled = t < 4f; doorGlow.intensity = Mathf.PingPong(t * 3f, 1f) * 3f; }
            if (endGroup == null) return;
            endGroup.alpha = Mathf.Clamp01((t - 3.5f) / 2.5f);
            endLine.color = new Color(endLine.color.r, endLine.color.g, endLine.color.b, Mathf.Clamp01((t - 5f) / 1.5f) * Mathf.Clamp01((13f - t) / 1.5f));
            endTitle.color = new Color(endTitle.color.r, endTitle.color.g, endTitle.color.b, Mathf.Clamp01((t - 13.5f) / 2f));
            endSub.color = new Color(endSub.color.r, endSub.color.g, endSub.color.b, Mathf.Clamp01((t - 15.5f) / 2f));
            statsText.color = new Color(statsText.color.r, statsText.color.g, statsText.color.b, Mathf.Clamp01((t - 16.5f) / 2f));
            exitText.color = new Color(exitText.color.r, exitText.color.g, exitText.color.b, Mathf.Clamp01((t - 18f) / 1.5f));
            if (t > 18f && !endGroup.blocksRaycasts)
            {
                endGroup.blocksRaycasts = true; endGroup.interactable = true;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true; // PC: düğmeye tıklanabilsin
            }
        }
    }
}
