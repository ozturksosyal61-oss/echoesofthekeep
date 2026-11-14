using System;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 3 "Sarnıç" (host mantığı). B sarnıca girince kapı kapanır; su sızıntıyla LeakSeconds'ta bir seviye yükselir.
    /// B'de 4 vana: her birinin üstünde sembol, borusu renkli. A'da (Hazne Odası) gösterge (seviye + hedef) ve tablo:
    /// sembol → miktar, ayrıca "şu renk boru ters akar" (o vananın miktarı ters). A hesaplar, B'ye ne çevireceğini söyler;
    /// seviye hedefe tam oturunca A kolla mühürler. Yanlış mühür: +SurgeLevels. Seviye ≥ DrownLevel iken B'nin başı su altında:
    /// BreathSeconds sonra boğulur → deneme baştan (yeni tohum: semboller, renkler, miktarlar, hedef karışır).
    /// Bu düğümün değeri = tohum.
    /// </summary>
    public class CisternPuzzle : PuzzleNode
    {
        public const int Valves = 4, MaxLevel = 10, DrownLevel = 8, StartLevel = 2, SurgeLevels = 2;
        public const float LeakSeconds = 12f, BreathSeconds = 8f, DyingSeconds = 2.5f, ValveCooldown = 0.8f, LeverResetSeconds = 1.2f;
        public const float LevelHeight = 0.22f; // B odasında seviye başına su yüksekliği (m); 8 → göz hizasının üstü
        public static readonly string[] ColorNames = { "Kırmızı", "Mavi", "Yeşil", "Sarı" };
        public static readonly Color[] Colors =
        {
            // Docs/StyleGuide.md bulmaca paleti (kırmızı, mavi, yeşil, sarı)
            new Color(0.66f, 0.2f, 0.17f), new Color(0.18f, 0.37f, 0.62f), new Color(0.25f, 0.49f, 0.27f), new Color(0.79f, 0.64f, 0.15f),
        };
        static readonly int[] BaseAmounts = { 3, 2, -1, -2 };

        public enum Phase { Open = 0, Sealed = 1, Dying = 2 }

        [SerializeField] int levelNode = 21, phaseNode = 22, attemptNode = 23, timerNode = 24, leakNode = 25, breathNode = 26;
        [SerializeField] CisternValve[] valves = new CisternValve[0];
        [SerializeField] SyncedLever seal;
        [SerializeField] Vector3 zoneCenter, zoneSize;
        [SerializeField] Transform respawn;

        public static CisternPuzzle Instance { get; private set; }

        int leverResetAt = -1;

        public int Seed => Value;
        public int Level => Get(levelNode);
        public Phase CurrentPhase => (Phase)Get(phaseNode);
        public int Attempt => Get(attemptNode);

        static int Get(int node) => M != null ? M.GetValue(node) : 0;
        static int Now => M != null && M.Runner != null ? (int)M.Runner.Tick : 0;
        static float Dt => M != null && M.Runner != null ? M.Runner.DeltaTime : 1f / 60f;
        static int Ticks(float sec) => Mathf.CeilToInt(sec / Dt);

        /// <summary>Kalan nefes 0..1 (1: başı sudan çıkık).</summary>
        public float Breath01 => Get(breathNode) == 0 ? 1f : 1f - Mathf.Clamp01((Now - Get(breathNode)) * Dt / BreathSeconds);
        public float Dying01 => CurrentPhase == Phase.Dying ? Mathf.Clamp01((Now - Get(timerNode)) * Dt / DyingSeconds) : 0f;
        public float LeakProgress01 => CurrentPhase == Phase.Sealed ? Mathf.Clamp01((Now - Get(leakNode)) * Dt / LeakSeconds) : 0f;

        public Bounds Zone => new Bounds(zoneCenter, zoneSize);
        public bool InZone(Vector3 p) => Zone.Contains(p + Vector3.up * 0.5f);

        // ---------- Tohumdan yerleşim ----------

        public struct Setup
        {
            public int[] valveSymbol;  // vana → sembol
            public int[] valveColor;   // vana → boru rengi
            public int[] valveAmount;  // vana → tablodaki miktar (ters akış uygulanmamış)
            public int reversedColor;  // bu renkteki borunun vanası ters çalışır
            public int target;
            public int Effect(int valve) => valveColor[valve] == reversedColor ? -valveAmount[valve] : valveAmount[valve];
        }

        static int[] Shuffled(System.Random r, int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = r.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        public static Setup Make(int seed)
        {
            var r = new System.Random(seed);
            var syms = Shuffled(r, Glyphs.Count);
            var amt = Shuffled(r, Valves);
            var s = new Setup
            {
                valveSymbol = new int[Valves],
                valveColor = Shuffled(r, Valves),
                valveAmount = new int[Valves],
                reversedColor = r.Next(Valves),
                target = 4 + r.Next(4), // 4..7
            };
            for (int i = 0; i < Valves; i++) { s.valveSymbol[i] = syms[i]; s.valveAmount[i] = BaseAmounts[amt[i]]; }
            return s;
        }

        // ---------- Yaşam döngüsü ----------

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            foreach (var v in valves) if (v != null) v.ServerActivated += OnValve;
            if (seal != null) seal.ServerActivated += OnSeal;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this) Instance = null;
            foreach (var v in valves) if (v != null) v.ServerActivated -= OnValve;
            if (seal != null) seal.ServerActivated -= OnSeal;
        }

        void Set(int node, int v) => M.ServerSetValue(node, v);

        bool AnyInZone()
        {
            foreach (var a in PlayerAvatar.All) if (InZone(a.transform.position)) return true;
            return false;
        }

        void SetLevel(int v) => Set(levelNode, Mathf.Clamp(v, 0, MaxLevel));

        void NewAttempt(bool died)
        {
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            ServerSetValue(seed);
            SetLevel(StartLevel);
            Set(phaseNode, (int)Phase.Open);
            Set(timerNode, 0); Set(leakNode, 0); Set(breathNode, 0);
            if (died) Set(attemptNode, Attempt + 1);
            if (seal != null) seal.ServerReset();
            leverResetAt = -1;
            if (died && respawn != null)
                foreach (var a in PlayerAvatar.All)
                    if (InZone(a.transform.position)) a.GetComponent<NetworkCharacterController>().Teleport(respawn.position);
            var s = Make(seed);
            Dbg.Puzzle("Sarnıç: yeni deneme (öldü=" + died + ") hedef=" + s.target + " etkiler=" +
                       s.Effect(0) + "," + s.Effect(1) + "," + s.Effect(2) + "," + s.Effect(3));
        }

        public override void ServerFixedUpdate()
        {
            if (PuzzleState == PuzzleState.Solved)
            {
                if (CurrentPhase != Phase.Open) Set(phaseNode, (int)Phase.Open);
                if (Level > 0 && Now % Ticks(0.25f) == 0) SetLevel(Level - 1); // çözülünce su boşalır
                return;
            }
            if (PuzzleState != PuzzleState.Active) return;
            if (Seed == 0) { NewAttempt(false); return; }

            int now = Now;
            if (leverResetAt >= 0 && now >= leverResetAt) { leverResetAt = -1; seal.ServerReset(); }

            switch (CurrentPhase)
            {
                case Phase.Open:
                    if (AnyInZone()) { Set(phaseNode, (int)Phase.Sealed); Set(leakNode, now); Dbg.Puzzle("Sarnıç kapandı"); }
                    break;
                case Phase.Sealed:
                    if ((now - Get(leakNode)) * Dt >= LeakSeconds) { SetLevel(Level + 1); Set(leakNode, now); }
                    if (Level >= DrownLevel)
                    {
                        if (Get(breathNode) == 0) Set(breathNode, now);
                        else if ((now - Get(breathNode)) * Dt >= BreathSeconds) Die();
                    }
                    else if (Get(breathNode) != 0) Set(breathNode, 0);
                    break;
                case Phase.Dying:
                    if ((now - Get(timerNode)) * Dt >= DyingSeconds) NewAttempt(true);
                    break;
            }
        }

        void Die()
        {
            Set(phaseNode, (int)Phase.Dying);
            Set(timerNode, Now);
            M.ServerAddDeath();
            Dbg.Puzzle("Sarnıç: boğuldu");
        }

        void OnValve(PuzzleNode node)
        {
            if (!IsServer || PuzzleState != PuzzleState.Active || CurrentPhase == Phase.Dying) return;
            int idx = Array.IndexOf(valves, node as CisternValve);
            if (idx < 0) return;
            int e = Make(Seed).Effect(idx);
            SetLevel(Level + e);
            Dbg.Puzzle("Sarnıç: vana " + idx + " (" + (e > 0 ? "+" : "") + e + ") → seviye " + Level);
        }

        void OnSeal(PuzzleNode lever)
        {
            if (!IsServer || PuzzleState != PuzzleState.Active || lever.Value == 0) return;
            if (CurrentPhase == Phase.Sealed && Level == Make(Seed).target)
            {
                ServerSolve();
                return;
            }
            leverResetAt = Now + Ticks(LeverResetSeconds);
            if (CurrentPhase == Phase.Dying) return;
            SetLevel(Level + SurgeLevels);
            Dbg.Puzzle("Sarnıç: yanlış mühür, su yükseldi → " + Level);
        }
    }
}
