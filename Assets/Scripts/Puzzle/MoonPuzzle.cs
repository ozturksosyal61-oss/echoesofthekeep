using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 6 "Ay aynaları" (host mantığı). Simge seti: ay evreleri (bu bulmacaya özgü).
    /// X (çatı terası): gökte ay (evresi + yönü her turda değişir), 3 döner ayna. A1 ayı yakalar ve A2'ye, A2 kuyu aynası A3'e yansıtmalı
    /// (16 konum, 22.5°). A3 (8 konum) ışığı kuyu ağzındaki 8 mercekten birine indirir.
    /// O (kuyu odası): zeminde 8 ay evresi döşemesi (dizilim tohumdan); merceklerin hangi döşemeye düştüğü tohumdan karışık.
    /// X gökteki ayın evresini söyler, O ışığın düştüğü evreyi söyler; ışık gökteki evreye HoldSeconds tutulunca tur geçilir (3 tur).
    /// Her turda ay WindowSeconds görünür; süre dolarsa bulut kapatır → aynalar ve tüm eşlemeler karışır, turlar sıfırlanır.
    /// Bu düğümün değeri = tohum.
    /// </summary>
    public class MoonPuzzle : PuzzleNode
    {
        public const int Rounds = 3, Phases = 8, Lenses = 8, MirrorSteps = 16;
        public const float WindowSeconds = 75f, CloudSeconds = 6f, HoldSeconds = 2.5f;
        public static readonly string[] PhaseNames = { "Yeni Ay", "Büyüyen Hilal", "İlk Dördün", "Büyüyen Şişkin Ay", "Dolunay", "Küçülen Şişkin Ay", "Son Dördün", "Küçülen Hilal" };
        public static readonly float[] MoonAzimuths = { -45f, 0f, 45f }; // çatıya göre (kuzey = +z)
        public enum Phase { Window = 0, Cloud = 1 }

        [SerializeField] int roundNode = 51, phaseNode = 52, holdNode = 53;
        [SerializeField] MoonMirror[] mirrors = new MoonMirror[3]; // A1, A2, A3 (kuyu aynası)
        [SerializeField] Transform roof; // çatı kökü: yönler bunun yerelinde

        public static MoonPuzzle Instance { get; private set; }
        public int Seed => Value;
        public int Round => Get(roundNode);
        public Phase CurrentPhase => (Phase)Get(phaseNode);
        public bool Holding => Get(holdNode) != 0;
        public Transform Roof => roof;
        public MoonMirror[] Mirrors => mirrors;
        static int Get(int node) => M != null ? M.GetValue(node) : 0;
        static int Now => M != null && M.Runner != null ? (int)M.Runner.Tick : 0;
        static float Dt => M != null && M.Runner != null ? M.Runner.DeltaTime : 1f / 60f;
        float Elapsed => (Now - (M != null ? M.GetTick(phaseNode) : 0)) * Dt;

        /// <summary>Ayın kalan görünme süresi 0..1 (bulutta 0).</summary>
        public float Window01 => CurrentPhase == Phase.Window ? 1f - Mathf.Clamp01(Elapsed / WindowSeconds) : 0f;
        public float WindowLeft => CurrentPhase == Phase.Window ? Mathf.Max(0f, WindowSeconds - Elapsed) : 0f;
        public float Hold01 => Holding ? Mathf.Clamp01((Now - Get(holdNode)) * Dt / HoldSeconds) : 0f;

        // ---------- Tohumdan kurulum ----------
        public struct Setup
        {
            public int[] moonPhase;   // tur → evre (1..7, yeni ay yok)
            public float[] moonAz;    // tur → ay yönü
            public int[] lensToTile;  // A3 konumu → zemin döşemesi
            public int[] tilePhase;   // döşeme → evre
        }

        public static Setup Make(int seed)
        {
            var r = new System.Random(seed);
            var s = new Setup { moonPhase = new int[Rounds], moonAz = new float[Rounds], lensToTile = Shuffle(r, Lenses), tilePhase = Shuffle(r, Phases) };
            var pool = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7 };
            float lastAz = 999f;
            for (int i = 0; i < Rounds; i++)
            {
                int k = r.Next(pool.Count); s.moonPhase[i] = pool[k]; pool.RemoveAt(k);
                float az; do az = MoonAzimuths[r.Next(MoonAzimuths.Length)]; while (az == lastAz);
                s.moonAz[i] = lastAz = az;
            }
            return s;
        }

        static int[] Shuffle(System.Random r, int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = r.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        // ---------- Işık yolu (her iki taraf aynı sonucu hesaplar) ----------
        public static Vector3 Dir(float azimuth) => Quaternion.Euler(0, azimuth, 0) * Vector3.forward;
        static float Az(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;

        /// <summary>Yansıyan yön (çatı yerelinde, yatay). incoming: ışığın gidiş yönü. Ayna arkası ışığa dönükse false.</summary>
        public static bool Reflect(Vector3 incoming, float normalAz, out Vector3 outDir)
        {
            var n = Dir(normalAz);
            outDir = Vector3.zero;
            if (Vector3.Dot(-incoming, n) <= 0.01f) return false;
            outDir = incoming - 2f * Vector3.Dot(incoming, n) * n;
            outDir.y = 0f; outDir.Normalize();
            return true;
        }

        /// <summary>Işık zinciri: hits = kaç ayna ışık aldı (1..3), lens = kuyuya inen mercek (yoksa -1). Çatı yerelinde.</summary>
        public int Trace(float moonAz, out int hits, out Vector3 a1Out, out Vector3 a2Out)
        {
            hits = 0; a1Out = a2Out = Vector3.zero;
            if (roof == null || mirrors.Length < 3 || mirrors[0] == null || mirrors[1] == null || mirrors[2] == null) return -1;
            Vector3 P(int i) => roof.InverseTransformPoint(mirrors[i].transform.position);
            hits = 1;
            if (!Reflect(-Dir(moonAz), mirrors[0].NormalAz, out a1Out)) return -1;
            var to2 = P(1) - P(0); to2.y = 0;
            if (Vector3.Angle(a1Out, to2) > 2f) return -1;
            hits = 2;
            if (!Reflect(to2.normalized, mirrors[1].NormalAz, out a2Out)) return -1;
            var to3 = P(2) - P(1); to3.y = 0;
            if (Vector3.Angle(a2Out, to3) > 2f) return -1;
            hits = 3;
            return mirrors[2].Step % Lenses;
        }

        // ---------- Host ----------
        protected override void OnEnable() { base.OnEnable(); Instance = this; }
        protected override void OnDisable() { base.OnDisable(); if (Instance == this) Instance = null; }
        void Set(int node, int v, bool stamp = false) => M.ServerSetValue(node, v, stamp);

        void Scramble()
        {
            ServerSetValue(Random.Range(1, int.MaxValue));
            foreach (var m in mirrors) if (m != null) m.ServerScramble();
            Set(roundNode, 0);
            Set(holdNode, 0);
            Set(phaseNode, (int)Phase.Window, true);
        }

        public override void ServerFixedUpdate()
        {
            if (PuzzleState != PuzzleState.Active) return;
            if (Seed == 0) { Scramble(); return; }
            if (CurrentPhase == Phase.Cloud)
            {
                if (Elapsed >= CloudSeconds) { Scramble(); Dbg.Puzzle("Ay aynaları: bulut geçti, her şey karıştı"); }
                return;
            }
            if (Elapsed >= WindowSeconds) { Set(phaseNode, (int)Phase.Cloud, true); Set(holdNode, 0); Dbg.Puzzle("Ay aynaları: bulut"); return; }
            var s = Make(Seed);
            int round = Round;
            int lens = Trace(s.moonAz[round], out _, out _, out _);
            bool good = lens >= 0 && s.tilePhase[s.lensToTile[lens]] == s.moonPhase[round];
            if (!good) { if (Holding) Set(holdNode, 0); return; }
            if (!Holding) { Set(holdNode, Mathf.Max(1, Now)); return; }
            if ((Now - Get(holdNode)) * Dt < HoldSeconds) return;
            Set(holdNode, 0);
            if (round + 1 >= Rounds) { Set(roundNode, Rounds); Dbg.Puzzle("Ay aynaları çözüldü"); ServerSolve(); return; }
            Set(roundNode, round + 1);
            Set(phaseNode, (int)Phase.Window, true); // ay yer değiştirir, süre tazelenir
            Dbg.Puzzle("Ay aynaları: tur " + (round + 1));
        }
    }
}
