using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 7 "Çan ve kemik org" (host mantığı). Simge seti: nota işaretleri (Notes).
    /// Aşama 0 (çanlar): O'nun kriptasındaki org kitabesinde 4 nota yazılı; X'in 4 çanının her birinde bir nota kazılı.
    ///   O okur, X çanları o sırayla çalar. Yanlış çan: dizi başa döner.
    /// Aşama 1 (org): çanlar bitince X'in kadranı açılır ve 4 nota gösterir. X okur, O orgun 6 tuşundan (her tuşta nota) ilk 3'ünü çalar.
    ///   Yanlış tuş: kadran yeni dizi gösterir (karışır), aşama 1 baştan.
    /// Aşama 2 (son nota): X büyük çanı, O kadranın 4. notasını AYNI ANDA çalar (sesli 3-2-1). Fark > SyncWindow ya da yanlış tuş:
    ///   kadran karışır, aşama 1'e döner. Çözülünce iki odada kafes asansör finale iner.
    /// Düğümler: 60 tohum, 61 aşama, 62 ilerleme, 63 kadran tohumu, 59 hata sayacı, 64-67 çanlar, 68 büyük çan, 69 çan vuruş tick'i,
    /// 70-75 org tuşları, 76 tuş vuruş tick'i (+1: tuş indeksi kodlu).
    /// </summary>
    public class BellPuzzle : PuzzleNode
    {
        public const int Bells = 4, Keys = 6, Inscription = 4, DialLen = 4;
        public const float SyncWindow = 0.8f;
        public enum Stage { Bells = 0, Organ = 1, Final = 2, Done = 3 }

        [SerializeField] int stageNode = 61, progressNode = 62, dialNode = 63, errorNode = 59, bellStrikeNode = 69, keyStrikeNode = 76;

        public static BellPuzzle Instance { get; private set; }
        public int Seed => Value;
        public int DialSeed => Get(dialNode);
        public Stage CurrentStage => (Stage)Get(stageNode);
        public int Progress => Get(progressNode);
        public int Errors => Get(errorNode);
        static int Get(int node) => M != null ? M.GetValue(node) : 0;
        static int Now => M != null && M.Runner != null ? (int)M.Runner.Tick : 0;
        static float Dt => M != null && M.Runner != null ? M.Runner.DeltaTime : 1f / 60f;

        public struct Setup
        {
            public int[] bellNote;   // çan → nota
            public int[] order;      // kitabe: çalınacak çan sırası (çan indeksi)
            public int[] keyNote;    // org tuşu → nota
            public int[] dial;       // kadran: 4 nota (ilk 3 org, 4. son nota)
            public int InscriptionNote(int k) => bellNote[order[k]];
            public int KeyFor(int note) => System.Array.IndexOf(keyNote, note);
        }

        public static Setup Make(int seed, int dialSeed)
        {
            var r = new System.Random(seed);
            var s = new Setup();
            var six = Shuffle(r, Notes.Count);
            s.bellNote = new int[Bells];
            for (int i = 0; i < Bells; i++) s.bellNote[i] = six[i];
            s.order = Shuffle(r, Bells);
            s.keyNote = Shuffle(r, Keys);
            var rd = new System.Random(dialSeed);
            var d = Shuffle(rd, Notes.Count);
            s.dial = new int[DialLen];
            for (int i = 0; i < DialLen; i++) s.dial[i] = d[i];
            return s;
        }

        static int[] Shuffle(System.Random r, int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = r.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        protected override void OnEnable() { base.OnEnable(); Instance = this; }
        protected override void OnDisable() { base.OnDisable(); if (Instance == this) Instance = null; }
        void Set(int node, int v, bool stamp = false) => M.ServerSetValue(node, v, stamp);

        static bool Solo => PlayerAvatar.All.Count <= 1; // editörde tek kişi: son adımda büyük çan yeter

        void Error(string why)
        {
            Set(errorNode, Errors + 1);
            Dbg.Puzzle("Çan/org hata: " + why);
        }

        void ReshuffleDial()
        {
            int d; do d = Random.Range(1, int.MaxValue); while (d == DialSeed);
            Set(dialNode, d);
            Set(stageNode, (int)Stage.Organ);
            Set(progressNode, 0);
            Set(bellStrikeNode, 0); Set(keyStrikeNode, 0);
        }

        /// <summary>Host: çan (0..3) çalındı.</summary>
        public void ServerBell(int bell)
        {
            if (PuzzleState != PuzzleState.Active || Seed == 0 || CurrentStage != Stage.Bells) return;
            var s = Make(Seed, DialSeed);
            int p = Progress;
            if (s.order[p] != bell) { Set(progressNode, 0); Error("yanlış çan"); return; }
            if (p + 1 < Inscription) { Set(progressNode, p + 1); return; }
            Set(stageNode, (int)Stage.Organ); Set(progressNode, 0);
            Dbg.Puzzle("Çanlar tamam → kadran açıldı");
        }

        /// <summary>Host: büyük çan vuruldu.</summary>
        public void ServerGreatBell()
        {
            if (PuzzleState != PuzzleState.Active || CurrentStage != Stage.Final) return;
            if (Get(bellStrikeNode) == 0) Set(bellStrikeNode, Mathf.Max(1, Now));
            if (Solo) { Solve(); return; }
            Check();
        }

        /// <summary>Host: org tuşu (0..5) çalındı.</summary>
        public void ServerKey(int key)
        {
            if (PuzzleState != PuzzleState.Active || Seed == 0) return;
            var st = CurrentStage;
            if (st != Stage.Organ && st != Stage.Final) return;
            var s = Make(Seed, DialSeed);
            int note = s.keyNote[key];
            if (st == Stage.Organ)
            {
                int p = Progress;
                if (note != s.dial[p]) { ReshuffleDial(); Error("yanlış tuş"); return; }
                if (p + 1 < DialLen - 1) { Set(progressNode, p + 1); return; }
                Set(stageNode, (int)Stage.Final); Set(progressNode, DialLen - 1);
                Set(bellStrikeNode, 0); Set(keyStrikeNode, 0);
                Dbg.Puzzle("Org: 3 nota tamam → son nota bekleniyor");
                return;
            }
            if (note != s.dial[DialLen - 1]) { ReshuffleDial(); Error("son nota yanlış"); return; }
            if (Get(keyStrikeNode) == 0) Set(keyStrikeNode, Mathf.Max(1, Now));
            if (Solo) { Solve(); return; }
            Check();
        }

        void Check()
        {
            int b = Get(bellStrikeNode), k = Get(keyStrikeNode);
            if (b == 0 || k == 0) return;
            if (Mathf.Abs(b - k) * Dt <= SyncWindow) Solve();
            else { ReshuffleDial(); Error("eşzamanlı değil"); }
        }

        void Solve()
        {
            Set(stageNode, (int)Stage.Done);
            Dbg.Puzzle("Çan ve kemik org çözüldü");
            ServerSolve();
        }

        public override void ServerFixedUpdate()
        {
            if (PuzzleState != PuzzleState.Active) return;
            if (Seed == 0)
            {
                ServerSetValue(Random.Range(1, int.MaxValue));
                Set(dialNode, Random.Range(1, int.MaxValue));
                Set(stageNode, 0); Set(progressNode, 0);
                return;
            }
            if (CurrentStage != Stage.Final) return;
            int b = Get(bellStrikeNode), k = Get(keyStrikeNode);
            int first = b != 0 && k != 0 ? 0 : Mathf.Max(b, k);
            if (first != 0 && (Now - first) * Dt > SyncWindow) { ReshuffleDial(); Error("eşzamanlı değil (biri geç kaldı)"); }
        }
    }
}
