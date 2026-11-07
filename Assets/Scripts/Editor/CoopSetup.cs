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
using UnityEngine.UI;

namespace CoopGame.EditorTools
{
    /// <summary>Prefab, sahne, oyuncu ayarı ve Android build kurulumu. Menü: CoopGame/Setup All. Batch: -executeMethod CoopGame.EditorTools.CoopSetup.SetupAll</summary>
    public static class CoopSetup
    {
        const string AvatarPath = "Assets/Prefabs/PlayerAvatar.prefab";
        const string RunnerPath = "Assets/Resources/NetworkRunner.prefab";
        const string LobbyPath = "Assets/Scenes/Lobby.unity";
        const string GamePath = "Assets/Scenes/Game.unity";
        const string ApkPath = "Builds/Android/CoopGame-dev.apk";

        [MenuItem("CoopGame/Setup All")]
        public static void SetupAll()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Scenes");

            var avatar = CreateAvatarPrefab();
            CreateRunnerPrefab();
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
            avatar = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

            CreateLobbyScene();
            CreateGameScene(avatar.GetComponent<NetworkObject>());

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
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "PlayerAvatar";
            root.AddComponent<NetworkObject>();
            root.AddComponent<PlayerAvatar>();
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
            SetBool(so, "usePrimaryRecorder", true);
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

        // ---------- Sahneler ----------

        static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(new Vector3(0, 1, -10), Vector3.zero, new Color(0.08f, 0.1f, 0.16f));
            cam.name = "Main Camera";

            var canvas = MakeCanvas();
            MakeText(canvas, "Title", "CoopGame", 90, new Vector2(0, 700), new Vector2(900, 140), TextAnchor.MiddleCenter);
            var input = MakeInput(canvas, "CodeInput", new Vector2(0, 250), new Vector2(600, 150));
            var join = MakeButton(canvas, "JoinButton", "Katıl", new Vector2(0, 40), new Vector2(600, 150));
            var create = MakeButton(canvas, "CreateButton", "Oda Kur", new Vector2(0, -150), new Vector2(600, 150));
            var status = MakeText(canvas, "Status", "", 44, new Vector2(0, -420), new Vector2(950, 260), TextAnchor.UpperCenter);

            var lobby = new GameObject("LobbyUI").AddComponent<LobbyUI>();
            var so = new SerializedObject(lobby);
            SetRef(so, "codeInput", input);
            SetRef(so, "createButton", create);
            SetRef(so, "joinButton", join);
            SetRef(so, "status", status);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, LobbyPath);
        }

        static void CreateGameScene(NetworkObject avatarPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Düz kamera: sabit, zemine hafif aşağı bakar.
            var cam = MakeCamera(new Vector3(0, 4, -9), new Vector3(0, 0.5f, 0), new Color(0.55f, 0.75f, 0.95f));
            cam.name = "Main Camera";

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(14, 1, 10);
            floor.GetComponent<Renderer>().sharedMaterial.color = new Color(0.35f, 0.4f, 0.35f);

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            var sso = new SerializedObject(session);
            SetRef(sso, "avatarPrefab", avatarPrefab);
            sso.ApplyModifiedPropertiesWithoutUndo();

            var canvas = MakeCanvas();
            var role = MakeText(canvas, "RoleText", "Rol: -", 110, new Vector2(0, 780), new Vector2(900, 160), TextAnchor.MiddleCenter);
            var info = MakeText(canvas, "InfoText", "", 40, new Vector2(0, 640), new Vector2(1000, 130), TextAnchor.UpperCenter);

            var dots = new Image[2];
            var labels = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                float y = -700 - i * 90;
                var dot = MakeImage(canvas, "SpeakDot" + (i == 0 ? "A" : "B"), new Vector2(-380, y), new Vector2(50, 50));
                dots[i] = dot;
                labels[i] = MakeText(canvas, "SpeakLabel" + (i == 0 ? "A" : "B"), i == 0 ? "A" : "B", 44, new Vector2(-150, y), new Vector2(380, 70), TextAnchor.MiddleLeft);
            }
            var leave = MakeButton(canvas, "LeaveButton", "Çık", new Vector2(330, -770), new Vector2(300, 110));

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            var so = new SerializedObject(hud);
            SetRef(so, "roleText", role);
            SetRef(so, "infoText", info);
            SetRef(so, "leaveButton", leave);
            var dp = so.FindProperty("speakDots"); dp.arraySize = 2;
            var lp = so.FindProperty("speakLabels"); lp.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                dp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
                lp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GamePath);
        }

        // ---------- UI yardımcıları (legacy uGUI) ----------

        static Camera MakeCamera(Vector3 pos, Vector3 lookAt, Color bg)
        {
            var go = new GameObject("Camera");
            var cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.transform.position = pos;
            go.transform.LookAt(lookAt);
            go.AddComponent<AudioListener>();
            return cam;
        }

        static RectTransform MakeCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            return go.GetComponent<RectTransform>();
        }

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static RectTransform Place(GameObject go, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Text MakeText(RectTransform parent, string name, string text, int size, Vector2 pos, Vector2 dim, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var t = go.AddComponent<Text>();
            t.font = UiFont; t.text = text; t.fontSize = size; t.alignment = anchor; t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.2f);
            return img;
        }

        static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = MakeText(go.GetComponent<RectTransform>(), "Label", label, 56, Vector2.zero, dim, TextAnchor.MiddleCenter);
            t.raycastTarget = false;
            return btn;
        }

        static InputField MakeInput(RectTransform parent, string name, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, pos, dim);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.95f);
            var field = go.AddComponent<InputField>();
            var text = MakeText(go.GetComponent<RectTransform>(), "Text", "", 80, Vector2.zero, dim - new Vector2(30, 0), TextAnchor.MiddleCenter);
            text.color = Color.black;
            var ph = MakeText(go.GetComponent<RectTransform>(), "Placeholder", "0000", 80, Vector2.zero, dim - new Vector2(30, 0), TextAnchor.MiddleCenter);
            ph.color = new Color(0, 0, 0, 0.3f);
            field.targetGraphic = img;
            field.textComponent = text;
            field.placeholder = ph;
            return field;
        }

        // ---------- Oyuncu ayarları ----------

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CoopGame";
            PlayerSettings.productName = "CoopGame";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.coopgame.p0");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.coopgame.p0");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // iOS: mikrofon izni metni (Türkçe)
            PlayerSettings.iOS.microphoneUsageDescription = "Oyun arkadaşınla sesli konuşabilmen için mikrofona erişim gerekiyor.";

            // Android: IL2CPP + ARM64, minSdk 26. RECORD_AUDIO: Assets/Plugins/Android/AndroidManifest.xml
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
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
