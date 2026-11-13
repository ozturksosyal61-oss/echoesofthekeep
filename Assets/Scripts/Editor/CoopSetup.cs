using System.IO;
using Fusion;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoopGame.EditorTools
{
    /// <summary>Prefab, sahne, oyuncu ayarı ve Android build kurulumu. Menü: CoopGame/Setup All. Batch: -executeMethod CoopGame.EditorTools.CoopSetup.SetupAll</summary>
    public static partial class CoopSetup
    {
        const string AvatarPath = "Assets/Prefabs/PlayerAvatar.prefab";
        const string RunnerPath = "Assets/Resources/NetworkRunner.prefab";
        const string RoomStatePath = "Assets/Prefabs/RoomState.prefab";
        const string LobbyPath = "Assets/Scenes/Lobby.unity";
        const string GamePath = "Assets/Scenes/Game.unity";
        const string ApkPath = "Builds/Android/CoopGame-dev.apk";

        static bool quickSetup;

        /// <summary>Hızlı kontrol: ışık pişirme yok; baked ışıklar geçici olarak gerçek zamanlı (yalnızca editörde bakmak için, build'e çıkmadan önce Setup All).</summary>
        [MenuItem("CoopGame/Setup Hızlı (bake yok, editör testi)")]
        public static void SetupQuick()
        {
            quickSetup = true;
            try { SetupAll(); }
            finally { quickSetup = false; }
        }

        [MenuItem("CoopGame/Setup All")]
        public static void SetupAll()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); // dışarıdan indirilen model/dokular içe aktarılsın

            SetupUrp(); // önce pipeline: materyaller URP/Lit ile üretilsin
            PrepareArt(); // dokulu malzemeler, gökyüzü, post-fx (CoopSetup.Art.cs)
            PrepareKit(); // Art kit: malzeme kütüphanesi, dekor prefabları, ışık profili (CoopSetup.Kit.cs)
            PrepareUi();  // fontlar + prosedürel sprite'lar (CoopSetup.UI.cs)

            var avatar = CreateAvatarPrefab();
            CreateRunnerPrefab();
            CreateRoomStatePrefab();
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
            avatar = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

            CreateLobbyScene();
            CreateGameScene(avatar);
            if (quickSetup)
            {
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (l.lightmapBakeType == LightmapBakeType.Baked) { l.lightmapBakeType = LightmapBakeType.Realtime; l.shadows = LightShadows.None; }
                Lightmapping.Clear();
                EditorSceneManager.SaveOpenScenes();
                Debug.LogWarning("[CoopSetup] HIZLI kurulum: bake yok, ışıklar gerçek zamanlı. Telefona build'den önce Setup All yap.");
            }
            else if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noBake") < 0)
            {
                KitBake(); // Game sahnesi açık ve kayıtlı: kit odalarının ışığı pişer
                // Occlusion varsayılan KAPALI: hesaplanan veri Lobi Holü'nde kamerayı yanlışlıkla tamamen kesti (2026-11-13). -occlusion ile açılır.
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-occlusion") >= 0) KitOcclusion(); else StaticOcclusionCulling.Clear();
                EditorSceneManager.SaveOpenScenes();
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LobbyPath, true),
                new EditorBuildSettingsScene(GamePath, true),
            };
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[CoopSetup] Setup tamam.");
        }

        // ---------- Prefablar ----------

        static GameObject CreateAvatarPrefab()
        {
            var root = new GameObject("PlayerAvatar");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;
            root.AddComponent<NetworkObject>();
            var ncc = root.AddComponent<NetworkCharacterController>();
            ncc.maxSpeed = 5f; ncc.acceleration = 40f; ncc.braking = 30f; ncc.rotationSpeed = 12f;
            root.AddComponent<PlayerAvatar>();
            BuildModel(root.transform);
            root.AddComponent<VoiceNetworkObject>();

            var speakerGo = new GameObject("Speaker");
            speakerGo.transform.SetParent(root.transform, false);
            var src = speakerGo.AddComponent<AudioSource>();
            src.spatialBlend = 0f; // 2B: konuşma her yerde eşit duyulsun
            src.playOnAwake = false;
            speakerGo.AddComponent<Speaker>();
            speakerGo.AddComponent<VoiceGain>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, AvatarPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateRoomStatePrefab()
        {
            var go = new GameObject("RoomState");
            go.AddComponent<NetworkObject>();
            go.AddComponent<RoomState>();
            go.AddComponent<PuzzleManager>(); // bulmaca durumu aynı ağ nesnesinde (tek SpawnAsync)
            PrefabUtility.SaveAsPrefabAsset(go, RoomStatePath);
            Object.DestroyImmediate(go);
        }

        // ---------- URP (mobil) ----------

        const string UrpAssetPath = "Assets/Settings/URP-Mobile.asset";
        const string UrpRendererPath = "Assets/Settings/URP-Mobile-Renderer.asset";

        static void SetInt(SerializedObject so, string name, int v)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogError("[CoopSetup] URP alanı yok: " + name); else p.intValue = v;
        }
        static void SetFlt(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogError("[CoopSetup] URP alanı yok: " + name); else p.floatValue = v;
        }
        static void SetB(SerializedObject so, string name, bool v)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogError("[CoopSetup] URP alanı yok: " + name); else p.boolValue = v;
        }

        /// <summary>Mobil URP asset + renderer: tek ana ışık (gölgeli, kısa mesafe), az sayıda per-pixel ek ışık, MSAA 2x, HDR/derinlik/opak doku kapalı.</summary>
        static void SetupUrp()
        {
            Directory.CreateDirectory("Assets/Settings");
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (asset == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, UrpRendererPath);
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, UrpAssetPath);
            }

            var so = new SerializedObject(asset);
            SetB(so, "m_SupportsHDR", PostFx);            // bloom için HDR
            SetInt(so, "m_MSAA", 2);                          // MsaaQuality._2x
            SetFlt(so, "m_RenderScale", 1f);
            SetInt(so, "m_MainLightRenderingMode", 1);        // PerPixel (tek ana ışık)
            SetB(so, "m_MainLightShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", 1024);
            SetFlt(so, "m_ShadowDistance", 25f);              // kısa gölge mesafesi
            SetInt(so, "m_ShadowCascadeCount", 1);
            SetB(so, "m_SoftShadowsSupported", false);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1); // PerPixel: oda/lamba noktasal ışıkları için
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 4);
            SetB(so, "m_AdditionalLightShadowsSupported", false);
            SetB(so, "m_RequireDepthTexture", false);
            SetB(so, "m_RequireOpaqueTexture", false);
            SetB(so, "m_UseSRPBatcher", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            // Graphics + tüm kalite seviyeleri
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[CoopSetup] URP atandı: " + UrpAssetPath + " (kalite seviyesi: " + QualitySettings.names.Length + ")");
        }

        /// <summary>Malzeme klasörü: Art kit kurulurken Assets/Art/Kit/Materials olur.</summary>
        static string matDir = "Assets/Materials";

        /// <summary>Malzemeyi üretir ya da var olanı sıfırlar (GUID sabit kalır: prefab/sahne referansları ve git farkı bozulmaz).</summary>
        static Material MakeMat(string name, Color color, string shaderName = "Universal Render Pipeline/Lit")
        {
            Directory.CreateDirectory(matDir);
            string path = matDir + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception(shaderName + " shader bulunamadı (URP paketi yüklü mü?)");
            var fresh = new Material(shader) { color = color };
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { AssetDatabase.CreateAsset(fresh, path); return fresh; }
            m.shader = shader;
            m.CopyPropertiesFromMaterial(fresh);
            m.shaderKeywords = fresh.shaderKeywords;
            m.renderQueue = -1;
            m.globalIlluminationFlags = fresh.globalIlluminationFlags;
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            b.transform.localScale = scale;
            b.GetComponent<Renderer>().sharedMaterial = mat;
            b.isStatic = true;
            return b;
        }

        // ---------- Avatar modeli + yürüme animasyonu (primitiflerden, Animator ile) ----------

        static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // çarpışmayı yalnızca CharacterController yapar
            go.GetComponent<Renderer>().sharedMaterial = avatarMat;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go.transform;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        static Material avatarMat;

        static void BuildModel(Transform root)
        {
            if (BuildCharacterModels(root)) return; // Mixamo X/O (CoopSetup.Characters.cs); yoksa primitif gövde
            avatarMat = MakeMat("AvatarMat", Color.white); // URP/Lit; çalışma anında rol rengine boyanır
            var model = new GameObject("Model").transform;
            model.SetParent(root, false);
            Part(model, "Body", PrimitiveType.Cube, new Vector3(0, 1.15f, 0), new Vector3(0.6f, 0.7f, 0.35f));
            Part(model, "Head", PrimitiveType.Sphere, new Vector3(0, 1.75f, 0), new Vector3(0.4f, 0.4f, 0.4f));
            Part(model, "Nose", PrimitiveType.Cube, new Vector3(0, 1.75f, 0.22f), new Vector3(0.1f, 0.1f, 0.14f)); // yön işareti
            foreach (var side in new[] { ("LegL", -0.15f), ("LegR", 0.15f) })
            {
                var piv = Pivot(model, side.Item1, new Vector3(side.Item2, 0.8f, 0));
                Part(piv, "Mesh", PrimitiveType.Cube, new Vector3(0, -0.4f, 0), new Vector3(0.2f, 0.8f, 0.2f));
            }
            foreach (var side in new[] { ("ArmL", -0.4f), ("ArmR", 0.4f) })
            {
                var piv = Pivot(model, side.Item1, new Vector3(side.Item2, 1.45f, 0));
                Part(piv, "Mesh", PrimitiveType.Cube, new Vector3(0, -0.3f, 0), new Vector3(0.15f, 0.6f, 0.15f));
            }

            var controller = BuildAnimator();
            var animator = model.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
        }

        static AnimationClip MakeClip(string name, params (string path, float[] values)[] curves)
        {
            var clip = new AnimationClip { name = name };
            const float len = 0.8f;
            foreach (var c in curves)
            {
                var curve = new AnimationCurve();
                for (int i = 0; i < c.values.Length; i++)
                    curve.AddKey(len * i / (c.values.Length - 1), c.values[i]);
                for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                clip.SetCurve(c.path, typeof(Transform), "localEulerAnglesRaw.x", curve);
            }
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            return clip;
        }

        static UnityEditor.Animations.AnimatorController BuildAnimator()
        {
            const string path = "Assets/Prefabs/Avatar.controller";
            var idle = MakeClip("Idle",
                ("ArmL", new[] { 0f, 3f, 0f, -3f, 0f }), ("ArmR", new[] { 0f, -3f, 0f, 3f, 0f }));
            var walk = MakeClip("Walk",
                ("LegL", new[] { 0f, 35f, 0f, -35f, 0f }), ("LegR", new[] { 0f, -35f, 0f, 35f, 0f }),
                ("ArmL", new[] { 0f, -30f, 0f, 30f, 0f }), ("ArmR", new[] { 0f, 30f, 0f, -30f, 0f }));
            AssetDatabase.CreateAsset(idle, "Assets/Prefabs/Idle.anim");
            AssetDatabase.CreateAsset(walk, "Assets/Prefabs/Walk.anim");

            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
            sm.defaultState = sIdle;
            var t1 = sIdle.AddTransition(sWalk);
            t1.hasExitTime = false; t1.duration = 0.1f; t1.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.3f, "Speed");
            var t2 = sWalk.AddTransition(sIdle);
            t2.hasExitTime = false; t2.duration = 0.1f; t2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "Speed");
            return ctrl;
        }

        static void CreateRunnerPrefab()
        {
            var go = new GameObject("NetworkRunner");
            go.AddComponent<NetworkRunner>();
            go.AddComponent<NetworkSceneManagerDefault>();

            var recGo = new GameObject("Recorder");
            recGo.transform.SetParent(go.transform, false);
            var rec = recGo.AddComponent<Recorder>();
            rec.TransmitEnabled = true;

            var vc = go.AddComponent<FusionVoiceClient>(); // Fusion oturumuna girince Voice odasına da girer
            vc.UseFusionAppSettings = true;                 // Voice AppId: PhotonAppSettings.AppIdVoice
            vc.UseFusionAuthValues = true;
            var so = new SerializedObject(vc);
            // false: true olursa FusionVoiceClient Recorder'ı katılımda, VoiceNetworkObject da spawn'da ekler ("already added").
            SetBool(so, "usePrimaryRecorder", false);
            SetRef(so, "primaryRecorder", rec);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, RunnerPath);
            Object.DestroyImmediate(go);
        }

        static void SetBool(SerializedObject so, string name, bool v)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogError("[CoopSetup] Alan yok: " + name); else p.boolValue = v;
        }

        static void SetRef(SerializedObject so, string name, Object v)
        {
            var p = so.FindProperty(name);
            if (p == null) Debug.LogError("[CoopSetup] Alan yok: " + name); else p.objectReferenceValue = v;
        }

        // ---------- Oyuncu ayarları ----------

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CoopGame";
            PlayerSettings.productName = "Echoes of the Keep";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.coopgame.p0");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.oztdigital.echoesofthekeep");
            // Yalnızca yatay (Android + iOS ortak ayar): LandscapeLeft/Right, otomatik döndürme; dikey kapalı.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // iOS: mikrofon izni metni (Türkçe)
            PlayerSettings.iOS.microphoneUsageDescription = "Oyun arkadaşınla sesli konuşabilmen için mikrofona erişim gerekiyor.";

            // Android: IL2CPP + ARM64, minSdk 26. RECORD_AUDIO: Assets/Plugins/Android/AndroidManifest.xml
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.renderOutsideSafeArea = false; // içerik çentik/kavisli kenar altına taşmasın
        }

        static string Arg(string key)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
            return null;
        }

        /// <summary>Performans build'leri. Args: -perfName a|b|c  -perfDev 1|0  -perfApi vulkan|gles3. Çıktı: Builds/Android/CoopGame-perf-NAME.apk (taze dosya).</summary>
        public static void BuildPerf()
        {
            string name = Arg("-perfName") ?? "x";
            bool dev = Arg("-perfDev") == "1";
            bool gles = Arg("-perfApi") == "gles3";
            string path = "Builds/Android/CoopGame-perf-" + name + ".apk";

            ConfigurePlayer();
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { gles ? UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 : UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.Delete(path); // ölü alan birikmesin
            EditorUserBuildSettings.buildAppBundle = false;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { LobbyPath, GamePath },
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = path,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true); // proje ayarını geri al
            Debug.Log("[CoopSetup] PerfBuild " + name + " dev=" + dev + " api=" + (gles ? "gles3" : "vulkan") + " -> " + report.summary.result + " " +
                      (report.summary.result == BuildResult.Succeeded ? Path.GetFullPath(path) + " (" + (report.summary.totalSize / 1048576) + " MB)" : ""));
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// iOS Xcode projesi (Mac, batch): Builds/iOS. Args: -bundleId com.x.y  -teamId ABCDE12345. Build numarası zaman damgası
        /// (TestFlight her yüklemede artan numara ister). Arşiv + yükleme: Tools/ios_testflight.sh.
        /// </summary>
        [MenuItem("CoopGame/Build iOS (Xcode projesi)")]
        public static void BuildIOS()
        {
            ConfigurePlayer();
            var bundle = Arg("-bundleId"); var team = Arg("-teamId");
            if (!string.IsNullOrEmpty(bundle)) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundle);
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.buildNumber = System.DateTime.UtcNow.ToString("yyMMddHHmm");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            const string outDir = "Builds/iOS";
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { LobbyPath, GamePath },
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                locationPathName = outDir,
                options = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-dev") >= 0 ? BuildOptions.Development : BuildOptions.None, // -dev: test araçları açık
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[CoopSetup] iOS build: " + report.summary.result + " -> " + Path.GetFullPath(outDir) + " build " + PlayerSettings.iOS.buildNumber);
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        [MenuItem("CoopGame/Build Android (Development)")]
        public static void BuildAndroid()
        {
            ConfigurePlayer();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { LobbyPath, GamePath },
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = ApkPath,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[CoopSetup] Build sonucu: " + report.summary.result + " -> " + Path.GetFullPath(ApkPath));
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
