using System;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 2 "Kibrit Mahzeni" (host mantığı). B karanlık mahzene girince kapı kapanır; 3 kibriti var (her biri MatchSeconds yanar).
    /// Lahitlerde sembol + Roma rakamı, kulede (A) yazıtta 3 rakam: A rakamları söyler, B ilgili lahitlerin sembollerini anlatır, A kadranları çevirip kolu çeker.
    /// Kurallar: karanlıkta DreadSeconds kalan B ölür; yanlış cevap yanan kibriti söndürür ve bir kibrit yakar (kibrit yoksa ölüm).
    /// Ölünce deneme baştan: yeni tohum (lahitler/yazıt/kadranlar karışır), 3 kibrit, B mahzen girişine döner.
    /// Bu düğümün değeri = tohum (0: henüz yok). Diğer durumlar ek düğümlerde (aşağıdaki id'ler).
    /// </summary>
    public class CryptPuzzle : PuzzleNode
    {
        public const int MatchCount = 3, Dials = 3, Coffins = 6;
        public const float MatchSeconds = 10f, DreadSeconds = 15f, DyingSeconds = 2.5f, LeverResetSeconds = 1.2f;
        public const int AbilityMatch = 1;

        public enum Phase { Open = 0, Sealed = 1, Dying = 2 }

        [SerializeField] int matchesNode = 11, litNode = 12, phaseNode = 13, attemptNode = 14, timerNode = 19;
        [SerializeField] SymbolDial[] dials = new SymbolDial[0];
        [SerializeField] SyncedLever confirm;
        [SerializeField] Vector3 zoneCenter, zoneSize; // dünya koordinatı
        [SerializeField] Transform respawn;

        public static CryptPuzzle Instance { get; private set; }

        int leverResetAt = -1;

        public int Seed => Value;
        public int Matches => Get(matchesNode);
        public int LitUntil => Get(litNode);
        public Phase CurrentPhase => (Phase)Get(phaseNode);
        public int Attempt => Get(attemptNode);
        public int PhaseNodeId => phaseNode;
        public int MatchesNodeId => matchesNode;
        public int AttemptNodeId => attemptNode;

        static int Get(int node) => M != null ? M.GetValue(node) : 0;
        static int Now => M != null && M.Runner != null ? (int)M.Runner.Tick : 0;
        static float Dt => M != null && M.Runner != null ? M.Runner.DeltaTime : 1f / 60f;
        static int Ticks(float sec) => Mathf.CeilToInt(sec / Dt);

        public bool Lit => LitUntil > Now;
        public float LitLeft => Lit ? (LitUntil - Now) * Dt : 0f;
        /// <summary>Karanlık korkusu 0..1 (kapalı mahzende ışıksız geçen süre). Dying'de 1.</summary>
        public float Dread01 =>
            CurrentPhase == Phase.Dying ? 1f :
            CurrentPhase == Phase.Sealed && !Lit ? Mathf.Clamp01((Now - Get(timerNode)) * Dt / DreadSeconds) : 0f;
        public float Dying01 => CurrentPhase == Phase.Dying ? Mathf.Clamp01((Now - Get(timerNode)) * Dt / DyingSeconds) : 0f;

        public Bounds Zone => new Bounds(zoneCenter, zoneSize);
        public bool InZone(Vector3 p) => Zone.Contains(p + Vector3.up * 0.5f);

        // ---------- Tohumdan yerleşim (iki tarafta aynı) ----------

        public struct Setup
        {
            public int[] coffinSymbol;   // lahit → sembol
            public int[] coffinNumeral;  // lahit → rakam (1..6)
            public int[] inscription;    // A'nın yazıtı: kadran k için rakam
            public int[][] dialOrder;    // kadran k, konum v → sembol
            public int[] solution;       // kadran k → doğru sembol
        }

        static int[] Shuffled(System.Random r, int n, int offset = 0)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i + offset;
            for (int i = n - 1; i > 0; i--) { int j = r.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        public static Setup Make(int seed)
        {
            var r = new System.Random(seed);
            var s = new Setup
            {
                coffinSymbol = Shuffled(r, Coffins),
                coffinNumeral = Shuffled(r, Coffins, 1),
                dialOrder = new int[Dials][],
                solution = new int[Dials],
            };
            var nums = Shuffled(r, Coffins, 1);
            s.inscription = new[] { nums[0], nums[1], nums[2] };
            for (int k = 0; k < Dials; k++)
            {
                s.dialOrder[k] = Shuffled(r, Glyphs.Count);
                s.solution[k] = s.coffinSymbol[Array.IndexOf(s.coffinNumeral, s.inscription[k])];
            }
            return s;
        }

        // ---------- Yaşam döngüsü ----------

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            PlayerAvatar.ServerAbility += OnAbility;
            if (confirm != null) confirm.ServerActivated += OnConfirm;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this) Instance = null;
            PlayerAvatar.ServerAbility -= OnAbility;
            if (confirm != null) confirm.ServerActivated -= OnConfirm;
        }

        void Set(int node, int v) => M.ServerSetValue(node, v);

        bool AnyInZone()
        {
            foreach (var a in PlayerAvatar.All) if (InZone(a.transform.position)) return true;
            return false;
        }

        void NewAttempt(bool died)
        {
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            ServerSetValue(seed);
            Set(matchesNode, MatchCount);
            Set(litNode, 0);
            Set(phaseNode, (int)Phase.Open);
            Set(timerNode, 0);
            if (died) Set(attemptNode, Attempt + 1);
            foreach (var d in dials) d.ServerRandomize();
            if (confirm != null) confirm.ServerReset();
            leverResetAt = -1;
            if (died && respawn != null)
                foreach (var a in PlayerAvatar.All)
                    if (InZone(a.transform.position)) a.GetComponent<NetworkCharacterController>().Teleport(respawn.position);
            Dbg.Puzzle("Mahzen: yeni deneme (öldü=" + died + ") deneme=" + Attempt + " çözüm=" + string.Join(",", Make(seed).solution));
        }

        public override void ServerFixedUpdate()
        {
            if (PuzzleState == PuzzleState.Solved)
            {
                if (CurrentPhase != Phase.Open) Set(phaseNode, (int)Phase.Open);
                return;
            }
            if (PuzzleState != PuzzleState.Active) return;
            if (Seed == 0) { NewAttempt(false); return; }

            int now = Now;
            if (leverResetAt >= 0 && now >= leverResetAt) { leverResetAt = -1; confirm.ServerReset(); }

            switch (CurrentPhase)
            {
                case Phase.Open:
                    if (AnyInZone()) { Set(phaseNode, (int)Phase.Sealed); Set(timerNode, now); Dbg.Puzzle("Mahzen kapandı"); }
                    break;
                case Phase.Sealed:
                    if (Lit) break;
                    if (LitUntil != 0) { Set(litNode, 0); Set(timerNode, now); break; } // kibrit söndü: korku yeniden başlar
                    if ((now - Get(timerNode)) * Dt >= DreadSeconds) Die("karanlık");
                    break;
                case Phase.Dying:
                    if ((now - Get(timerNode)) * Dt >= DyingSeconds) NewAttempt(true);
                    break;
            }
        }

        void Die(string why)
        {
            Set(phaseNode, (int)Phase.Dying);
            Set(litNode, 0);
            Set(timerNode, Now);
            Dbg.Puzzle("Mahzen: ölüm (" + why + ")");
        }

        void OnAbility(PlayerAvatar who, int ability)
        {
            if (ability != AbilityMatch || !IsServer || PuzzleState != PuzzleState.Active) return;
            if (CurrentPhase != Phase.Sealed || Lit || Matches <= 0 || !InZone(who.transform.position)) return;
            Set(matchesNode, Matches - 1);
            Set(litNode, Now + Ticks(MatchSeconds));
            Dbg.Puzzle("Kibrit yakıldı, kalan " + Matches);
        }

        void OnConfirm(PuzzleNode lever)
        {
            if (!IsServer || PuzzleState != PuzzleState.Active || lever.Value == 0) return;
            var sol = Make(Seed).solution;
            bool ok = true;
            for (int k = 0; k < dials.Length && k < sol.Length; k++) ok &= dials[k].Symbol(Seed) == sol[k];
            if (ok)
            {
                Set(litNode, 0);
                ServerSolve();
                return;
            }
            leverResetAt = Now + Ticks(LeverResetSeconds);
            Dbg.Puzzle("Mahzen: yanlış cevap");
            if (CurrentPhase == Phase.Dying) return;
            if (Matches <= 0 && CurrentPhase == Phase.Sealed && !Lit) { Die("yanlış cevap, kibrit yok"); return; }
            Set(matchesNode, Mathf.Max(0, Matches - 1));
            if (Lit) { Set(litNode, 0); Set(timerNode, Now); }
        }
    }
}
