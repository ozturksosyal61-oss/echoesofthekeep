using UnityEngine;

namespace CoopGame
{
    /// <summary>
    /// Bulmaca 6 görselleri (iki tarafta da tohumdan/durumdan): gökteki ay (evre dokusu + yön), çatıdaki ışık huzmeleri
    /// (ay → A1 → A2 → A3 → mercek), kuyu zeminindeki 8 ay evresi döşemesi ve kuyuya inen ışık (huzme + zemin lekesi + ışık).
    /// Bulut: ay kararır, huzmeler söner; sürenin son 12 sn'sinde ay titrer.
    /// </summary>
    public class MoonView : MonoBehaviour
    {
        [SerializeField] MoonPuzzle puzzle;
        [SerializeField] Renderer moon;                  // gökte ay kartı (Quad)
        [SerializeField] Material[] skyMats = new Material[8], tileMats = new Material[8];
        [SerializeField] Renderer[] tiles = new Renderer[8]; // kuyu zemini döşemeleri (dizin = döşeme)
        [SerializeField] Material beamMat;
        [SerializeField] Material flareMat;              // ayna/mercek çarpma noktasında parlama (billboard)
        [SerializeField] Transform mouth;                // çatıda kuyu ağzı merkezi (mercekler çevresinde)
        [SerializeField] float lensRadius = 0.95f;
        [SerializeField] Transform wellTop;              // kuyu odasında ışığın girdiği nokta (şaft tepesi)
        [SerializeField] Transform spot;                 // zemin lekesi (disk)
        [SerializeField] Light spotLight;
        [SerializeField] float moonDistance = 40f, moonHeight = 23f, moonSize = 7f; // kamera uzak düzlemi 140 m + sis: yakın ve büyük

        Transform[] beams, flares;
        int shownSeed = -1;
        static Mesh tube;

        /// <summary>Kapaksız tüp, y: -1..1 (silindir ölçek düzeniyle aynı), uv.y boy boyunca 0..1.</summary>
        static Mesh Tube()
        {
            if (tube != null) return tube;
            const int n = 14;
            var v = new Vector3[(n + 1) * 2]; var nr = new Vector3[v.Length]; var uv = new Vector2[v.Length];
            var tri = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * 0.5f + Vector3.down; v[i * 2 + 1] = d * 0.5f + Vector3.up;
                nr[i * 2] = nr[i * 2 + 1] = d;
                uv[i * 2] = new Vector2(i / (float)n, 0); uv[i * 2 + 1] = new Vector2(i / (float)n, 1);
                if (i == n) break;
                int k = i * 6, b = i * 2;
                tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 2; tri[k + 3] = b + 2; tri[k + 4] = b + 1; tri[k + 5] = b + 3;
            }
            tube = new Mesh { name = "MoonBeamTube", vertices = v, normals = nr, uv = uv, triangles = tri };
            tube.bounds = new Bounds(Vector3.zero, new Vector3(1.2f, 2.2f, 1.2f));
            return tube;
        }

        void Start()
        {
            beams = new Transform[5];
            for (int i = 0; i < beams.Length; i++)
            {
                var go = new GameObject("MoonBeam" + i);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = Tube();
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = beamMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                go.SetActive(false);
                beams[i] = go.transform;
            }
            flares = new Transform[5];
            for (int i = 0; i < flares.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "MoonFlare" + i;
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = flareMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                go.SetActive(false);
                flares[i] = go.transform;
            }
        }

        void Flare(int i, Vector3 p, float size)
        {
            if (flareMat == null) return;
            var t = flares[i];
            t.gameObject.SetActive(true);
            t.position = p;
            t.localScale = Vector3.one * size;
        }

        void Beam(int i, Vector3 a, Vector3 b, float w)
        {
            var t = beams[i];
            var d = b - a;
            if (d.sqrMagnitude < 0.01f) { t.gameObject.SetActive(false); return; }
            t.gameObject.SetActive(true);
            t.position = (a + b) * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            t.localScale = new Vector3(w, d.magnitude * 0.5f, w);
        }

        void Update()
        {
            if (puzzle == null || beams == null) return;
            int seed = puzzle.Seed;
            var st = puzzle.PuzzleState;
            bool on = st == PuzzleState.Active && seed != 0;
            if (seed != shownSeed)
            {
                shownSeed = seed;
                if (seed != 0)
                {
                    var s0 = MoonPuzzle.Make(seed);
                    for (int i = 0; i < tiles.Length && i < s0.tilePhase.Length; i++)
                        if (tiles[i] != null) tiles[i].sharedMaterial = tileMats[s0.tilePhase[i]];
                }
            }
            for (int i = 0; i < beams.Length; i++) beams[i].gameObject.SetActive(false);
            for (int i = 0; i < flares.Length; i++) flares[i].gameObject.SetActive(false);
            if (spot != null) spot.gameObject.SetActive(false);
            if (spotLight != null) spotLight.enabled = false;

            var roof = puzzle.Roof;
            if (!on || roof == null)
            {
                // çözülünce ay sabit dolunay olarak kalır
                if (moon != null) { moon.enabled = st == PuzzleState.Solved; if (st == PuzzleState.Solved) PlaceMoon(roof, 0f, 4); }
                return;
            }
            var s = MoonPuzzle.Make(seed);
            int round = Mathf.Clamp(puzzle.Round, 0, MoonPuzzle.Rounds - 1);
            float az = s.moonAz[round];
            bool cloud = puzzle.CurrentPhase == MoonPuzzle.Phase.Cloud;
            float left = puzzle.WindowLeft;
            bool flicker = !cloud && left < 12f && Mathf.PerlinNoise(Time.time * 2.2f, 0.3f) < Mathf.Lerp(0.75f, 0.25f, left / 12f);
            if (moon != null) { moon.enabled = !cloud; PlaceMoon(roof, az, s.moonPhase[round]); }
            if (cloud || flicker) return;

            var mir = puzzle.Mirrors;
            int lens = puzzle.Trace(az, out int hits, out var a1Out, out var a2Out);
            Vector3 W(Vector3 local) => roof.TransformPoint(local);
            Vector3 Mp(int i) => mir[i].transform.position + Vector3.up * 1.6f;
            var moonPos = roof.TransformPoint(MoonPuzzle.Dir(az) * 34f + Vector3.up * 18f);
            Beam(0, moonPos, Mp(0), 0.9f);
            Flare(0, Mp(0), 1.3f);
            if (hits == 1) { if (a1Out != Vector3.zero) Beam(1, Mp(0), Mp(0) + roof.TransformDirection(a1Out) * 9f, 0.45f); return; }
            Beam(1, Mp(0), Mp(1), 0.55f);
            Flare(1, Mp(1), 1.1f);
            if (hits == 2) { if (a2Out != Vector3.zero) Beam(2, Mp(1), Mp(1) + roof.TransformDirection(a2Out) * 9f, 0.45f); return; }
            Beam(2, Mp(1), Mp(2), 0.55f);
            Flare(2, Mp(2), 1.1f);
            if (lens < 0 || mouth == null) return;
            var lensPos = mouth.position + roof.TransformDirection(MoonPuzzle.Dir(mir[2].NormalAz)) * lensRadius;
            Beam(3, Mp(2), lensPos, 0.4f);
            Flare(3, lensPos, 0.7f);
            // kuyu odası: şaft tepesinden döşemeye
            int tile = s.lensToTile[lens];
            if (wellTop != null && tile < tiles.Length && tiles[tile] != null)
            {
                var p = tiles[tile].transform.position;
                Beam(4, wellTop.position, p, 1.2f);
                Flare(4, p + Vector3.up * 0.1f, 1.6f);
                if (spot != null) { spot.gameObject.SetActive(true); spot.position = p + Vector3.up * 0.015f; }
                if (spotLight != null)
                {
                    spotLight.enabled = true;
                    spotLight.transform.position = p + Vector3.up * 1.2f;
                    spotLight.intensity = 2.2f + (puzzle.Holding ? 1.5f * puzzle.Hold01 : 0f);
                }
            }
        }

        void PlaceMoon(Transform roof, float az, int phase)
        {
            if (moon == null || roof == null) return;
            var center = roof.position + Vector3.up * 2f;
            var pos = roof.TransformPoint(MoonPuzzle.Dir(az) * moonDistance + Vector3.up * moonHeight);
            moon.transform.position = pos;
            moon.transform.rotation = Quaternion.LookRotation(pos - center, Vector3.up);
            moon.transform.localScale = Vector3.one * moonSize;
            if (phase >= 0 && phase < skyMats.Length && moon.sharedMaterial != skyMats[phase]) moon.sharedMaterial = skyMats[phase];
        }
    }
}
