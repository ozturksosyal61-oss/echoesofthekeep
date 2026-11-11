using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>
    /// Mahzenin yerel tarafı: yerel oyuncu mahzendeyken ortam ışığı/sis karartılır (yalnızca bu cihazda), kibrit ışığı kameraya bağlanır,
    /// "Kibrit yak" düğmesi, korku karartması, ölüm geçişi ve deneme mesajları. Ağ durumu CryptPuzzle'dan okunur.
    /// </summary>
    public class CryptHud : MonoBehaviour
    {
        [SerializeField] Button matchButton;
        [SerializeField] Text matchLabel;
        [SerializeField] Text status;
        [SerializeField] Text message;
        [SerializeField] Image dread;
        [SerializeField] Image fade;
        [SerializeField] Material flameMat;

        static readonly Color DarkAmbient = new Color(0.004f, 0.004f, 0.006f);
        const float DarkFog = 0.22f;

        Color ambient0, fog0; float density0, refl0;
        UnityEngine.Rendering.SphericalHarmonicsL2 probe0;
        bool darkApplied;
        const float DarkDelay = 3f;
        float zoneSince = -1f;
        readonly System.Collections.Generic.List<Light> killed = new System.Collections.Generic.List<Light>();
        Light matchLight;
        Transform matchRoot;
        int seenAttempt = -1;
        PuzzleState seenState;
        float messageUntil, fadeOut;

        void Start()
        {
            ambient0 = RenderSettings.ambientLight; fog0 = RenderSettings.fogColor; density0 = RenderSettings.fogDensity;
            refl0 = RenderSettings.reflectionIntensity; probe0 = RenderSettings.ambientProbe;
            matchButton.onClick.AddListener(() => { if (PlayerAvatar.Local != null) PlayerAvatar.Local.RequestAbility(CryptPuzzle.AbilityMatch); });

            matchRoot = new GameObject("MatchLight").transform;
            matchLight = matchRoot.gameObject.AddComponent<Light>();
            matchLight.type = LightType.Point;
            matchLight.color = new Color(1f, 0.62f, 0.3f);
            matchLight.range = 7f;
            matchLight.shadows = LightShadows.None;
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(flame.GetComponent<Collider>());
            flame.transform.SetParent(matchRoot, false);
            flame.transform.localScale = new Vector3(0.025f, 0.05f, 0.025f);
            var fr = flame.GetComponent<Renderer>();
            fr.sharedMaterial = flameMat;
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            matchRoot.gameObject.SetActive(false);

            dread.color = Color.clear; fade.color = Color.clear; message.text = ""; status.text = "";
        }

        void OnDestroy() => SetDark(false);

        void SetDark(bool on)
        {
            if (on == darkApplied) return;
            darkApplied = on;
            RenderSettings.ambientLight = on ? DarkAmbient : ambient0;
            RenderSettings.fogColor = on ? Color.black : fog0;
            RenderSettings.fogDensity = on ? DarkFog : density0;
            RenderSettings.reflectionIntensity = on ? 0f : refl0; // metal yüzeyler gökyüzü yansımasıyla karanlıkta parlamasın
            var sh = new UnityEngine.Rendering.SphericalHarmonicsL2();
            sh.AddAmbientLight(DarkAmbient);
            RenderSettings.ambientProbe = on ? sh : probe0;
            if (on)
            {
                killed.Clear();
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.enabled && l != matchLight && l.type != LightType.Directional) { l.enabled = false; killed.Add(l); }
            }
            else
            {
                foreach (var l in killed) if (l != null) l.enabled = true;
                killed.Clear();
            }
        }

        void Say(string text, float seconds) { message.text = text; messageUntil = Time.time + seconds; }

        void Update()
        {
            var c = CryptPuzzle.Instance;
            var me = PlayerAvatar.Local;
            if (c == null || PuzzleManager.Instance == null)
            {
                matchButton.gameObject.SetActive(false);
                return;
            }

            var st = c.PuzzleState;
            var phase = c.CurrentPhase;
            bool inZone = me != null && c.InZone(me.transform.position) && st != PuzzleState.Solved;
            // Odaya girince 3 sn sonra komple kararır (tüm ışıklar söner; yalnızca kibrit kalır)
            if (!inZone) zoneSince = -1f; else if (zoneSince < 0f) zoneSince = Time.time;
            SetDark(inZone && Time.time - zoneSince >= DarkDelay);

            // Deneme / çözüm mesajları (iki tarafta)
            int attempt = c.Attempt;
            if (seenAttempt >= 0 && attempt > seenAttempt)
            {
                Say("Karanlık yuttu. Mahzen yeniden düzenlendi.\nDeneme " + (attempt + 1), 4.5f);
                fadeOut = 1f;
            }
            seenAttempt = attempt;
            if (st != seenState)
            {
                if (st == PuzzleState.Active && seenState == PuzzleState.Locked) Say(inZone ? "" : "Zindanın derinliklerinde bir kapı aralandı...", 4f);
                if (st == PuzzleState.Solved) Say("Mahzen sırrını verdi.", 4f);
                seenState = st;
            }
            if (Time.time > messageUntil) message.text = "";

            // Kibrit ışığı
            bool lit = inZone && c.Lit;
            var cam = Camera.main;
            if (lit && cam != null)
            {
                if (matchRoot.parent != cam.transform)
                {
                    matchRoot.SetParent(cam.transform, false);
                    matchRoot.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
                }
                float left = c.LitLeft;
                float flick = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 9f, 0.3f);
                matchLight.intensity = 2.6f * flick * Mathf.Clamp01(left / 1.5f);
            }
            if (matchRoot.gameObject.activeSelf != lit) matchRoot.gameObject.SetActive(lit);

            // Düğme + durum
            bool canLight = inZone && st == PuzzleState.Active && phase == CryptPuzzle.Phase.Sealed && !c.Lit && c.Matches > 0;
            if (matchButton.gameObject.activeSelf != canLight) matchButton.gameObject.SetActive(canLight);
            if (canLight) matchLabel.text = "KİBRİT YAK  (" + c.Matches + ")";
            status.text = !inZone || st != PuzzleState.Active ? "" :
                          c.Lit ? "Kibrit yanıyor: " + Mathf.CeilToInt(c.LitLeft) + " sn   ·   kalan " + c.Matches :
                          phase == CryptPuzzle.Phase.Sealed ? (c.Matches > 0 ? "Karanlık. Kibrit: " + c.Matches : "Kibrit kalmadı") : "";

            // Korku + ölüm geçişi
            float d = inZone ? c.Dread01 : 0f;
            dread.color = new Color(0f, 0f, 0f, d * d * 0.9f);
            if (inZone && phase == CryptPuzzle.Phase.Sealed && d > 0.55f && Time.time > messageUntil) message.text = "Karanlıkta bir şey kıpırdıyor...";
            if (!inZone && phase == CryptPuzzle.Phase.Dying && Time.time > messageUntil) message.text = "Aşağıdan bir çığlık yankılanıyor...";

            float f = inZone && phase == CryptPuzzle.Phase.Dying ? c.Dying01 : 0f;
            fadeOut = Mathf.MoveTowards(fadeOut, 0f, Time.deltaTime / 1.5f);
            // fadeOut yalnızca mahzendeki (ölüp dönen) oyuncu için: diğer oyuncu karartma görmez
            float fo = me != null && Layout.RoomOf(me.transform.position) == Layout.RoomOf(c.transform.position) ? fadeOut : 0f;
            fade.color = new Color(0f, 0f, 0f, Mathf.Max(f, fo));
        }
    }
}
