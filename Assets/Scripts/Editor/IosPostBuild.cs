#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// iOS Xcode projesi sonrası: Game Center yetkisi (entitlement + GameKit), şifreleme beyanı (yalnız standart TLS/DTLS → muaf),
    /// App Store Connect'te uygulama sürümünde Game Center ayrıca işaretlenmeli.
    /// </summary>
    public static class IosPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string main = proj.GetUnityMainTargetGuid();
            string fw = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(fw, "GameKit.framework", false);
            proj.WriteToFile(projPath);

            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/EchoesOfTheKeep.entitlements", null, main);
            caps.AddGameCenter();
            caps.WriteToFile();

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
