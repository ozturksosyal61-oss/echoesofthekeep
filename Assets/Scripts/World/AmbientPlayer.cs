using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopGame
{
    /// <summary>
    /// Ortam sesleri: açılışta kodla sentezlenir (dosya/lisans yok). Bölgeye göre katmanlar çapraz geçer:
    /// Lobi: hafif rüzgâr + ocak çıtırtısı + ahşap gıcırtısı. X (kule): güçlü rüzgâr + saat tıkırtısı + dişli tıngırtısı.
    /// O (yer altı): derin uğultu + su damlaları. Final: rüzgâr + uğultu + çıtırtı. Ses: Ayarlar'daki müzik düzeyi; konuşurken kısılır.
    /// </summary>
    public class AmbientPlayer : MonoBehaviour
    {
        const int Rate = 22050;
        const float Base = 0.55f, Fade = 0.5f; // Fade: saniyede değişen hacim
        static AmbientPlayer inst;

        AudioSource wind, rumble, fire, shots;
        AudioClip[] drips, ticks, clanks, creaks;
        float tWind, tRumble, tFire, rDrip, rTick, rClank, rCreak; // hedef düzeyler / olay sıklığı (1/sn)
        float nextDrip, nextTick, nextClank, nextCreak, nextVol, vol = 1f, duck = 1f, master;
        bool tock;
        System.Random rng = new System.Random(7);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("AmbientPlayer");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AmbientPlayer>();
        }

        void Awake()
        {
            wind = Loop(Wind(8f));
            rumble = Loop(Rumble(6f));
            fire = Loop(Fire(5f));
            shots = gameObject.AddComponent<AudioSource>();
            shots.playOnAwake = false; shots.spatialBlend = 0f; shots.ignoreListenerPause = true;
            drips = new[] { Drip(1300f), Drip(1650f), Drip(1050f) };
            ticks = new[] { Tick(3200f), Tick(2500f) };
            clanks = new[] { Clank(180f), Clank(240f) };
            creaks = new[] { Creak(90f), Creak(120f) };
        }

        AudioSource Loop(AudioClip c)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = c; s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.ignoreListenerPause = true;
            s.Play();
            return s;
        }

        void Update()
        {
            bool inGame = SceneManager.GetActiveScene().name == Session.GameScene && PlayerAvatar.Local != null && !Reconnect.Active;
            int area = inGame ? PlayerAvatar.Local.Room : -1;
            switch (area)
            {
                case Layout.AreaHall:  Set(0.35f, 0.10f, 0.55f, 0f, 0f, 0f, 1f / 25f); break;
                case Layout.AreaX:     Set(0.75f, 0.05f, 0.00f, 0f, 1f, 1f / 9f, 1f / 18f); break;
                case Layout.AreaO:     Set(0.10f, 0.70f, 0.00f, 1f / 2.6f, 0f, 0f, 0f); break;
                case Layout.AreaFinal: Set(0.45f, 0.40f, 0.35f, 0f, 0f, 0f, 1f / 30f); break;
                default:               Set(0f, 0f, 0f, 0f, 0f, 0f, 0f); break;
            }
            if (Time.unscaledTime >= nextVol) { nextVol = Time.unscaledTime + 0.5f; vol = Settings.Music; }
            duck = Mathf.MoveTowards(duck, VoiceGuard.TalkActive ? 0.55f : 1f, Time.unscaledDeltaTime);
            master = Base * vol * duck;
            float step = Time.unscaledDeltaTime * Fade;
            wind.volume = Mathf.MoveTowards(wind.volume, tWind * master, step);
            rumble.volume = Mathf.MoveTowards(rumble.volume, tRumble * master, step);
            fire.volume = Mathf.MoveTowards(fire.volume, tFire * master, step);
            if (master <= 0.001f) return;

            float now = Time.unscaledTime;
            if (rDrip > 0f && now >= nextDrip) { nextDrip = now + Exp(rDrip); Shot(drips, 0.25f + R() * 0.35f, R() * 1.6f - 0.8f, 0.85f + R() * 0.3f); }
            if (rTick > 0f && now >= nextTick)
            {   // tik-tak, saniyede bir (uzak saat)
                nextTick = now + 1f; tock = !tock;
                Shot(ticks, 0.12f, tock ? 0.25f : -0.25f, 1f, tock ? 1 : 0);
            }
            if (rClank > 0f && now >= nextClank) { nextClank = now + Exp(rClank); Shot(clanks, 0.22f + R() * 0.15f, R() - 0.5f, 0.85f + R() * 0.3f); }
            if (rCreak > 0f && now >= nextCreak) { nextCreak = now + Exp(rCreak); Shot(creaks, 0.18f + R() * 0.12f, R() * 1.2f - 0.6f, 0.9f + R() * 0.2f); }
        }

        void Set(float w, float r, float f, float drip, float tick, float clank, float creak)
        {
            float now = Time.unscaledTime;
            // Yeni bölgeye girince olaylar hemen boşalmasın: ilk olay biraz sonra
            if (drip > 0f && rDrip <= 0f) nextDrip = now + 1f;
            if (tick > 0f && rTick <= 0f) nextTick = now + 0.5f;
            if (clank > 0f && rClank <= 0f) nextClank = now + Exp(clank);
            if (creak > 0f && rCreak <= 0f) nextCreak = now + Exp(creak);
            tWind = w; tRumble = r; tFire = f; rDrip = drip; rTick = tick; rClank = clank; rCreak = creak;
        }

        void Shot(AudioClip[] set, float v, float pan, float pitch, int idx = -1)
        {
            shots.panStereo = Mathf.Clamp(pan, -1f, 1f);
            shots.pitch = pitch;
            shots.PlayOneShot(set[idx >= 0 ? idx : rng.Next(set.Length)], v * master / Base);
        }

        float R() => (float)rng.NextDouble();
        float Exp(float rate) => Mathf.Clamp(-Mathf.Log(Mathf.Max(0.001f, R())) / rate, 0.4f / rate, 3f / rate);

        // ---------- sentez ----------
        static float N(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        /// <summary>Uçları çapraz geçişli döngü: son kısım başa karıştırılır, dikiş duyulmaz.</summary>
        static AudioClip Make(string name, float[] raw, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * Rate), x = raw.Length - n;
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = raw[i];
            for (int i = 0; i < x; i++) { float k = (float)i / x; d[i] = raw[n + i] * (1f - k) + raw[i] * k; }
            Normalize(d, 0.9f);
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip One(string name, float[] d)
        {
            Normalize(d, 0.9f);
            var c = AudioClip.Create(name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0f; foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m > 0f) for (int i = 0; i < d.Length; i++) d[i] *= peak / m;
        }

        static AudioClip Wind(float sec)
        {
            var r = new System.Random(1);
            int n = Mathf.RoundToInt((sec + 1f) * Rate);
            var d = new float[n];
            float lp = 0f, lp2 = 0f, bp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                // yavaş esinti: iki LFO; kesim frekansı esintiyle açılır (uğuldama)
                float g = 0.55f + 0.3f * Mathf.Sin(t * 0.83f) + 0.15f * Mathf.Sin(t * 2.1f + 1.3f);
                float a = 0.004f + 0.02f * g;
                lp += (N(r) - lp) * a;
                lp2 += (lp - lp2) * a;
                bp += (lp - lp2 - bp) * 0.05f;
                d[i] = (lp2 * 0.7f + bp * 2.5f) * g;
            }
            return Make("amb_wind", d, sec);
        }

        static AudioClip Rumble(float sec)
        {
            var r = new System.Random(2);
            int n = Mathf.RoundToInt((sec + 1f) * Rate);
            var d = new float[n];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                lp += (N(r) - lp) * 0.012f;
                lp2 += (lp - lp2) * 0.012f;
                d[i] = lp2 * (0.8f + 0.2f * Mathf.Sin(t * 0.5f)) + 0.08f * Mathf.Sin(t * 2f * Mathf.PI * 46f) * (0.6f + 0.4f * Mathf.Sin(t * 0.37f));
            }
            return Make("amb_rumble", d, sec);
        }

        static AudioClip Fire(float sec)
        {
            var r = new System.Random(3);
            int n = Mathf.RoundToInt((sec + 1f) * Rate);
            var d = new float[n];
            float hiss = 0f, env = 0f, pop = 0f, hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                hiss += (N(r) - hiss) * 0.08f;
                if (r.NextDouble() < 18.0 / Rate) env = 0.4f + (float)r.NextDouble() * 0.6f; // çıtırtı
                env *= 0.9965f;
                float s = N(r) * env;
                hp = 0.92f * (hp + s - prev); prev = s; // yüksek geçiren: keskin çıt
                pop += (N(r) * 0.4f - pop) * 0.002f; // alevin düşük gürlemesi
                d[i] = hiss * 0.12f + hp * 0.8f + pop * 2f;
            }
            return Make("amb_fire", d, sec);
        }

        static AudioClip Drip(float f0)
        {
            int n = Rate * 1; var d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = f0 * (1f + 1.4f * Mathf.Clamp01(t / 0.035f)); // yükselen "plink"
                ph += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 55f);
            }
            // taş yankısı: azalan gecikmeli kopyalar
            int[] taps = { 2900, 5300, 8100, 11900 }; float[] gain = { 0.45f, 0.3f, 0.2f, 0.12f };
            for (int k = 0; k < taps.Length; k++)
                for (int i = n - 1; i >= taps[k]; i--) d[i] += d[i - taps[k]] * gain[k] * 0.6f;
            return One("amb_drip", d);
        }

        static AudioClip Tick(float f0)
        {
            var r = new System.Random((int)f0);
            int n = Rate / 8; var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.7f + N(r) * 0.3f) * Mathf.Exp(-t * 160f);
            }
            return One("amb_tick", d);
        }

        static AudioClip Clank(float f0)
        {
            var r = new System.Random((int)f0);
            int n = Rate * 2; var d = new float[n];
            float[] ratio = { 1f, 2.76f, 5.4f, 8.93f }; float[] amp = { 1f, 0.6f, 0.35f, 0.2f }; float[] dec = { 2.5f, 3.5f, 5f, 7f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, s = N(r) * Mathf.Exp(-t * 90f) * 0.5f;
                for (int k = 0; k < ratio.Length; k++) s += Mathf.Sin(2f * Mathf.PI * f0 * ratio[k] * t) * amp[k] * Mathf.Exp(-t * dec[k]);
                d[i] = s * 0.5f;
            }
            return One("amb_clank", d);
        }

        static AudioClip Creak(float f0)
        {
            var r = new System.Random((int)f0 + 9);
            float len = 0.9f; int n = Mathf.RoundToInt(Rate * len); var d = new float[n];
            float ph = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, k = t / len;
                float f = f0 * (1f + 0.35f * Mathf.Sin(k * Mathf.PI * 1.5f)) * (1f + 0.04f * N(r)); // takılan sürtünme
                ph += f / Rate;
                float saw = (ph % 1f) * 2f - 1f;
                lp += (saw - lp) * 0.25f;
                d[i] = lp * Mathf.Sin(k * Mathf.PI); // yumuşak giriş/çıkış
            }
            return One("amb_creak", d);
        }
    }
}
