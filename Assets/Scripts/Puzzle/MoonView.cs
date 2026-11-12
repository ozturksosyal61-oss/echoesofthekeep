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
        [SerializeField] Transform mouth;                // çatıda kuyu ağzı merkezi (mercekler çevresinde)
        [SerializeField] float lensRadius = 0.95f;
        [SerializeField] Transform wellTop;              // kuyu odasında ışığın girdiği nokta (şaft tepesi)
        [SerializeField] Transform spot;                 // zemin lekesi (disk)
        [SerializeField] Light spotLight;
        [SerializeField] float moonDistance = 40f, moonHeight = 21f, moonSize = 5.5f; // kamera uzak düzlemi 140 m + sis: yakın ve büyük

        Transform[] beams;
        int shownSeed = -1;

        void Start()
        {
            beams = new Transform[5];
            for (int i = 0; i < beams.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(go.GetComponent<Collider>());
                go.name = "MoonBeam" + i;
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = beamMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                beams[i] = go.transform;
            }
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
            Beam(0, moonPos, Mp(0), 0.35f);
            if (hits == 1) { if (a1Out != Vector3.zero) Beam(1, Mp(0), Mp(0) + roof.TransformDirection(a1Out) * 9f, 0.18f); return; }
            Beam(1, Mp(0), Mp(1), 0.22f);
            if (hits == 2) { if (a2Out != Vector3.zero) Beam(2, Mp(1), Mp(1) + roof.TransformDirection(a2Out) * 9f, 0.18f); return; }
            Beam(2, Mp(1), Mp(2), 0.22f);
            if (lens < 0 || mouth == null) return;
            var lensPos = mouth.position + roof.TransformDirection(MoonPuzzle.Dir(mir[2].NormalAz)) * lensRadius;
            Beam(3, Mp(2), lensPos, 0.16f);
            // kuyu odası: şaft tepesinden döşemeye
            int tile = s.lensToTile[lens];
            if (wellTop != null && tile < tiles.Length && tiles[tile] != null)
            {
                var p = tiles[tile].transform.position;
                Beam(4, wellTop.position, p, 0.55f);
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
