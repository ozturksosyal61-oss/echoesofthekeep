using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace CoopGame
{
    /// <summary>Küçük FPS ve ping (RTT) göstergesi. Client: host'a RTT. Host: en yüksek client RTT'si.</summary>
    public class PerfHud : MonoBehaviour
    {
        [SerializeField] Text label;

        float acc;
        int frames;
        float fpsSum, nextLog;
        int fpsN;

        void Update()
        {
            frames++;
            acc += Time.unscaledDeltaTime;
            if (acc < 0.5f) return;
            float fps = frames / acc;
            frames = 0; acc = 0f;

            string ping = "-";
            var r = Session.Runner;
            if (r != null && r.IsRunning)
            {
                double best = -1;
                foreach (var p in r.ActivePlayers)
                {
                    if (p == r.LocalPlayer) continue;
                    best = System.Math.Max(best, r.GetPlayerRtt(p));
                }
                if (!r.IsServer) best = r.GetPlayerRtt(r.LocalPlayer);
                if (best >= 0) ping = Mathf.RoundToInt((float)(best * 1000.0)) + " ms";
                else if (r.IsServer) ping = "host";
            }
            string rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? "URP" : "BIRP";
            label.text = "FPS " + Mathf.RoundToInt(fps) + "   Ping " + ping + "   " + rp;

            fpsSum += fps; fpsN++;
            if (Time.unscaledTime >= nextLog)
            {
                nextLog = Time.unscaledTime + 5f;
                Debug.Log("[Perf] " + rp + " FPS ort=" + (fpsSum / fpsN).ToString("F1") + " anlık=" + fps.ToString("F0") + " ping=" + ping);
                fpsSum = 0f; fpsN = 0;
            }
        }
    }
}
