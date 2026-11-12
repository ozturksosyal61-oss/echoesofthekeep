using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 5 görselleri (her iki tarafta, tohumdan deterministik): O'nun labirentindeki iç duvarlar (çarpışmalı) ve hücre levhaları,
    /// X'in maketindeki küçük duvarlar, hücre işaretleri, tuzak (kırmızı) iğneleri, giriş/çıkış işaretleri.
    /// </summary>
    public class LabyrinthView : MonoBehaviour
    {
        const float WallH = 3.6f, WallT = 0.4f;
        [SerializeField] LabyrinthPuzzle puzzle;
        [SerializeField] Transform mazeRoot;   // labirent güneybatı köşesi (iç duvarlar buraya)
        [SerializeField] Transform modelRoot;  // maket güneybatı köşesi; ölçek = maket / labirent
        [SerializeField] float modelScale = 1f / 12f;
        [SerializeField] Material wallMat, plaqueMat, glyphMat, modelWallMat, modelGlyphMat, trapMat, exitMat;
        int shownSeed = -1;

        void Update()
        {
            int seed = puzzle != null ? puzzle.Seed : 0;
            if (seed == shownSeed) return;
            shownSeed = seed;
            Clear(mazeRoot); Clear(modelRoot);
            if (seed == 0) return;
            var m = LabyrinthPuzzle.Make(seed);
            if (mazeRoot != null) BuildMaze(m);
            if (modelRoot != null) BuildModel(m);
        }

        static void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material mat, bool collide)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!collide) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        void BuildMaze(LabyrinthPuzzle.Maze m)
        {
            const float c = LabyrinthPuzzle.Cell;
            for (int x = 0; x < LabyrinthPuzzle.N; x++)
                for (int z = 0; z < LabyrinthPuzzle.N; z++)
                {
                    if (m.wallN[x, z]) Box(mazeRoot, new Vector3((x + 0.5f) * c, WallH * 0.5f, (z + 1) * c), new Vector3(c - 0.5f, WallH, WallT), wallMat, true);
                    if (m.wallE[x, z]) Box(mazeRoot, new Vector3((x + 1) * c, WallH * 0.5f, (z + 0.5f) * c), new Vector3(WallT, WallH, c - 0.5f), wallMat, true);
                    // Levha: hücrenin güneybatı köşe sütununda, hücre merkezine dönük (yalnız bu hücreden okunur)
                    var p = new GameObject("Plaque").transform;
                    p.SetParent(mazeRoot, false);
                    p.localPosition = new Vector3(x * c + 0.42f, 1.75f, z * c + 0.42f);
                    p.localRotation = Quaternion.LookRotation(new Vector3(-1f, 0, -1f));
                    Box(p, new Vector3(0, 0, 0.03f), new Vector3(0.56f, 0.34f, 0.05f), plaqueMat, false);
                    var g = new GameObject("G").transform; g.SetParent(p, false); g.localPosition = new Vector3(-0.13f, 0, -0.005f);
                    Runes.Rune(g, m.glyph[x, z], glyphMat, 0.24f, 0.02f);
                    var n = new GameObject("N").transform; n.SetParent(p, false); n.localPosition = new Vector3(0.14f, 0, -0.005f);
                    Runes.Pips(n, m.numeral[x, z], glyphMat, 0.2f, 0.02f);
                }
        }

        void BuildModel(LabyrinthPuzzle.Maze m)
        {
            float c = LabyrinthPuzzle.Cell * modelScale, h = 0.07f, t = 0.02f;
            int nn = LabyrinthPuzzle.N;
            // Dış duvar (giriş güneyde x=0, çıkış kuzeyde x=N-1 açık)
            for (int i = 0; i < nn; i++)
            {
                if (i != m.entry.x) Box(modelRoot, new Vector3((i + 0.5f) * c, h * 0.5f, 0), new Vector3(c, h, t), modelWallMat, false);
                if (i != m.exit.x) Box(modelRoot, new Vector3((i + 0.5f) * c, h * 0.5f, nn * c), new Vector3(c, h, t), modelWallMat, false);
                Box(modelRoot, new Vector3(0, h * 0.5f, (i + 0.5f) * c), new Vector3(t, h, c), modelWallMat, false);
                Box(modelRoot, new Vector3(nn * c, h * 0.5f, (i + 0.5f) * c), new Vector3(t, h, c), modelWallMat, false);
            }
            var flat = Quaternion.LookRotation(Vector3.down, Vector3.forward); // yukarı bakan işaret, "üst" kuzeye
            for (int x = 0; x < nn; x++)
                for (int z = 0; z < nn; z++)
                {
                    if (m.wallN[x, z]) Box(modelRoot, new Vector3((x + 0.5f) * c, h * 0.5f, (z + 1) * c), new Vector3(c + t, h, t), modelWallMat, false);
                    if (m.wallE[x, z]) Box(modelRoot, new Vector3((x + 1) * c, h * 0.5f, (z + 0.5f) * c), new Vector3(t, h, c + t), modelWallMat, false);
                    var a = new GameObject("Cell").transform;
                    a.SetParent(modelRoot, false);
                    a.localPosition = new Vector3((x + 0.5f) * c, 0.004f, (z + 0.5f) * c);
                    a.localRotation = flat;
                    var g = new GameObject("G").transform; g.SetParent(a, false); g.localPosition = new Vector3(-c * 0.22f, c * 0.12f, 0);
                    Runes.Rune(g, m.glyph[x, z], modelGlyphMat, c * 0.36f, 0.004f);
                    var n = new GameObject("N").transform; n.SetParent(a, false); n.localPosition = new Vector3(c * 0.22f, c * 0.12f, 0);
                    Runes.Pips(n, m.numeral[x, z], modelGlyphMat, c * 0.3f, 0.004f);
                    if (m.trap[x, z])
                    {
                        var pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Destroy(pin.GetComponent<Collider>());
                        pin.transform.SetParent(modelRoot, false);
                        pin.transform.localPosition = new Vector3((x + 0.5f) * c, 0.02f, (z + 0.25f) * c);
                        pin.transform.localScale = Vector3.one * c * 0.22f;
                        pin.GetComponent<Renderer>().sharedMaterial = trapMat;
                    }
                }
            Box(modelRoot, new Vector3((m.entry.x + 0.5f) * c, 0.01f, -0.06f), new Vector3(c * 0.6f, 0.02f, 0.08f), modelGlyphMat, false);
            Box(modelRoot, new Vector3((m.exit.x + 0.5f) * c, 0.01f, nn * c + 0.06f), new Vector3(c * 0.6f, 0.02f, 0.08f), exitMat, false);
        }
    }
}
