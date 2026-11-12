using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 5 "Katakomb labirenti" (host mantığı). O karanlık 8x8 labirentte fenerle ilerler; her hücrenin köşesinde
    /// rün + zar noktası (Runes; 36 çift, hücreler arasında tekrar edebilir — yerini komşularla birlikte tarif etmek gerekir).
    /// Tuzaklar çözüm yolundan sapan neredeyse her kolun ilk hücresinde: X'in yönlendirmesi olmadan ilerlemek ölümcül. X harita odasındaki makette duvarları, hücre işaretlerini, tuzakları ve çıkışı görür.
    /// O girişten içeri adım atınca yağ sayacı başlar. Tuzak hücre ya da yağın bitmesi → kısa karanlık, girişe dönüş, yeni tohum
    /// (duvarlar, işaretler, tuzaklar karışır). Çıkış hücresine varınca çözülür. Bu düğümün değeri = tohum.
    /// Hücre (x,z): origin'den +x (doğu) ve +z (kuzey). Giriş (0,0) güney duvarında; çıkış kuzey sırasında, her tohumda farklı sütun (diğer kuzey açıklıkları duvarla kapanır).
    /// </summary>
    public class LabyrinthPuzzle : PuzzleNode
    {
        public const int N = 8, ExtraTraps = 6, ExtraOpenings = 6;
        public const float Cell = 2.4f, OilSeconds = 170f, DyingSeconds = 2.2f;
        public enum Phase { Waiting = 0, Running = 1, Dying = 2 }

        [SerializeField] int phaseNode = 45, attemptNode = 47;
        [SerializeField] Transform origin;   // labirentin güneybatı köşesi (dünya)
        [SerializeField] Transform respawn;  // giriş odası
        [SerializeField] Vector3 anteCenter, anteSize; // giriş odası bölgesi (dünya): ölünce buradakiler de geri alınır

        public static LabyrinthPuzzle Instance { get; private set; }
        public int Seed => Value;
        public Phase CurrentPhase => (Phase)Get(phaseNode);
        public int Attempt => Get(attemptNode);
        public Transform Origin => origin;
        static int Get(int node) => M != null ? M.GetValue(node) : 0;
        static int Now => M != null && M.Runner != null ? (int)M.Runner.Tick : 0;
        static float Dt => M != null && M.Runner != null ? M.Runner.DeltaTime : 1f / 60f;
        float Elapsed => (Now - (M != null ? M.GetTick(phaseNode) : 0)) * Dt;

        /// <summary>Kalan yağ 0..1 (labirente girilmeden 1).</summary>
        public float Oil01 => CurrentPhase == Phase.Running ? 1f - Mathf.Clamp01(Elapsed / OilSeconds) : CurrentPhase == Phase.Dying ? 0f : 1f;
        public float Dying01 => CurrentPhase == Phase.Dying ? Mathf.Clamp01(Elapsed / DyingSeconds) : 0f;

        // ---------- Tohumdan labirent ----------
        public class Maze
        {
            public bool[,] wallN = new bool[N, N]; // hücrenin kuzey kenarı (z < N-1)
            public bool[,] wallE = new bool[N, N]; // hücrenin doğu kenarı (x < N-1)
            public bool[,] trap = new bool[N, N];
            public int[,] glyph = new int[N, N], numeral = new int[N, N]; // glyph = rün 0..5, numeral = zar 1..6
            public Vector2Int entry = new Vector2Int(0, 0), exit = new Vector2Int(N - 1, N - 1); // exit.x tohumdan

            public bool Open(Vector2Int a, Vector2Int b)
            {
                if (b.x == a.x + 1) return !wallE[a.x, a.y];
                if (b.x == a.x - 1) return !wallE[b.x, b.y];
                if (b.y == a.y + 1) return !wallN[a.x, a.y];
                if (b.y == a.y - 1) return !wallN[b.x, b.y];
                return false;
            }
        }

        static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        static bool In(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < N && c.y < N;

        public static Maze Make(int seed)
        {
            var r = new System.Random(seed);
            var m = new Maze();
            m.exit = new Vector2Int(2 + r.Next(N - 2), N - 1); // girişin hemen üstü değil
            for (int x = 0; x < N; x++) for (int z = 0; z < N; z++) { m.wallN[x, z] = z < N - 1; m.wallE[x, z] = x < N - 1; }
            void Carve(Vector2Int a, Vector2Int b)
            {
                if (b.x > a.x) m.wallE[a.x, a.y] = false; else if (b.x < a.x) m.wallE[b.x, b.y] = false;
                else if (b.y > a.y) m.wallN[a.x, a.y] = false; else m.wallN[b.x, b.y] = false;
            }
            // Derinlik öncelikli (tek çözümlü), sonra birkaç ek açıklık (döngü: ezber zorlaşır)
            var seen = new bool[N, N];
            var stack = new Stack<Vector2Int>();
            stack.Push(m.entry); seen[0, 0] = true;
            while (stack.Count > 0)
            {
                var c = stack.Peek();
                var opts = new List<Vector2Int>();
                foreach (var d in Dirs) { var n = c + d; if (In(n) && !seen[n.x, n.y]) opts.Add(n); }
                if (opts.Count == 0) { stack.Pop(); continue; }
                var nx = opts[r.Next(opts.Count)];
                Carve(c, nx); seen[nx.x, nx.y] = true; stack.Push(nx);
            }
            for (int k = 0, guard = 0; k < ExtraOpenings && guard < 200; guard++)
            {
                int x = r.Next(N), z = r.Next(N);
                if (r.Next(2) == 0 && x < N - 1 && m.wallE[x, z]) { m.wallE[x, z] = false; k++; }
                else if (z < N - 1 && m.wallN[x, z]) { m.wallN[x, z] = false; k++; }
            }
            // En kısa yol (BFS): tuzaklar yolun dışına
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>(); q.Enqueue(m.entry); prev[m.entry] = m.entry;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in Dirs) { var n = c + d; if (In(n) && !prev.ContainsKey(n) && m.Open(c, n)) { prev[n] = c; q.Enqueue(n); } }
            }
            var path = new HashSet<Vector2Int>(); var pathList = new List<Vector2Int>(); // liste: sıra her makinede aynı
            for (var c = m.exit; ; c = prev[c]) { path.Add(c); pathList.Add(c); if (c == m.entry) break; }
            // Sapak tuzakları: yoldaki her hücrenin yol dışı açık komşusu tuzak (yönlendirmesiz ilerlemek ölümcül); sonra rastgele ek tuzaklar
            var free = new List<Vector2Int>();
            foreach (var c in pathList)
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (In(n) && !path.Contains(n) && m.Open(c, n) && !m.trap[n.x, n.y]) m.trap[n.x, n.y] = true;
                }
            for (int x = 0; x < N; x++) for (int z = 0; z < N; z++) { var c = new Vector2Int(x, z); if (!path.Contains(c) && !m.trap[x, z]) free.Add(c); }
            for (int k = 0; k < ExtraTraps && free.Count > 0; k++) { int i = r.Next(free.Count); m.trap[free[i].x, free[i].y] = true; free.RemoveAt(i); }
            // Rün + zar: 36 çift karıştırılıp sırayla dağıtılır (64 hücre: bazı çiftler iki kez görülür)
            var pairs = new List<int>();
            for (int i = 0; i < Runes.Count * 6; i++) pairs.Add(i);
            for (int i = pairs.Count - 1; i > 0; i--) { int j = r.Next(i + 1); (pairs[i], pairs[j]) = (pairs[j], pairs[i]); }
            for (int x = 0; x < N; x++) for (int z = 0; z < N; z++)
            {
                int p = pairs[(x * N + z) % pairs.Count];
                m.glyph[x, z] = p % Runes.Count;
                m.numeral[x, z] = p / Runes.Count + 1;
            }
            return m;
        }

        /// <summary>Dünya konumu labirent içindeyse hücresi.</summary>
        public bool CellOf(Vector3 world, out Vector2Int cell)
        {
            cell = default;
            if (origin == null) return false;
            var l = world - origin.position;
            int x = Mathf.FloorToInt(l.x / Cell), z = Mathf.FloorToInt(l.z / Cell);
            if (x < 0 || z < 0 || x >= N || z >= N || l.y < -1f || l.y > 4f) return false;
            cell = new Vector2Int(x, z);
            return true;
        }

        public bool InAnte(Vector3 p) => new Bounds(anteCenter, anteSize).Contains(p + Vector3.up * 0.5f);

        // ---------- Host ----------
        protected override void OnEnable() { base.OnEnable(); Instance = this; }
        protected override void OnDisable() { base.OnDisable(); if (Instance == this) Instance = null; }

        void Set(int node, int v, bool stamp = false) => M.ServerSetValue(node, v, stamp);

        public override void ServerFixedUpdate()
        {
            if (PuzzleState != PuzzleState.Active) return;
            if (Seed == 0) { ServerSetValue(Random.Range(1, int.MaxValue)); Set(phaseNode, (int)Phase.Waiting, true); return; }
            var phase = CurrentPhase;
            if (phase == Phase.Dying)
            {
                if (Elapsed < DyingSeconds) return;
                foreach (var a in PlayerAvatar.All)
                    if (a != null && respawn != null && (CellOf(a.transform.position, out _) || InAnte(a.transform.position)))
                        a.GetComponent<NetworkCharacterController>().Teleport(respawn.position, respawn.rotation);
                ServerSetValue(Random.Range(1, int.MaxValue));
                Set(attemptNode, Attempt + 1);
                Set(phaseNode, (int)Phase.Waiting, true);
                Dbg.Puzzle("Labirent yeniden karıştı (deneme " + (Attempt + 1) + ")");
                return;
            }
            var maze = Make(Seed);
            foreach (var a in PlayerAvatar.All)
            {
                if (a == null || !CellOf(a.transform.position, out var c)) continue;
                if (phase == Phase.Waiting) { Set(phaseNode, (int)Phase.Running, true); phase = Phase.Running; Dbg.Puzzle("Labirent: fener yandı"); }
                if (c == maze.exit) { Dbg.Puzzle("Labirent çözüldü"); ServerSolve(); return; }
                if (maze.trap[c.x, c.y]) { Dbg.Puzzle("Labirent: tuzak " + c); Set(phaseNode, (int)Phase.Dying, true); return; }
            }
            if (phase == Phase.Running && Elapsed >= OilSeconds) { Dbg.Puzzle("Labirent: yağ bitti"); Set(phaseNode, (int)Phase.Dying, true); }
        }
    }
}
