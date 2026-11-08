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
    public static class CoopSetup
    {
        const string AvatarPath = "Assets/Prefabs/PlayerAvatar.prefab";
        const string RunnerPath = "Assets/Resources/NetworkRunner.prefab";
        const string RoomStatePath = "Assets/Prefabs/RoomState.prefab";
        const string LobbyPath = "Assets/Scenes/Lobby.unity";
        const string GamePath = "Assets/Scenes/Game.unity";
        const string ApkPath = "Builds/Android/CoopGame-dev.apk";

        [MenuItem("CoopGame/Setup All")]
        public static void SetupAll()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Scenes");

            SetupUrp(); // önce pipeline: materyaller URP/Lit ile üretilsin

            var avatar = CreateAvatarPrefab();
            CreateRunnerPrefab();
            CreateRoomStatePrefab();
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
            avatar = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

            CreateLobbyScene();
            CreateGameScene(avatar);

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
            PrefabUtility.SaveAsPrefabAsset(go, RoomStatePath);
            Object.DestroyImmediate(go);
        }

        // ---------- Dünya: iki kapalı oda (primitifler) ----------

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
            SetB(so, "m_SupportsHDR", false);
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

        static Material MakeMat(string name, Color color)
        {
            Directory.CreateDirectory("Assets/Materials");
            string path = "Assets/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.Exception("URP/Lit shader bulunamadı (URP paketi yüklü mü?)");
            var m = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(m, path);
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

        /// <summary>İç ölçü 16x16, yükseklik 4. Zemin üstü y=0. Duvar kalınlığı 0.5. withAlcove: arka duvarda kapılı küçük oda.</summary>
        static Transform BuildRoom(string name, Vector3 center, Material floorMat, Material wallMat, bool withAlcove)
        {
            var root = new GameObject(name).transform;
            root.position = center;
            const float h = 4f;
            Box("Floor", root, new Vector3(0, -0.25f, 0), new Vector3(17, 0.5f, 17), floorMat);
            Box("Ceiling", root, new Vector3(0, h + 0.25f, 0), new Vector3(17, 0.5f, 17), wallMat);
            Box("WallS", root, new Vector3(0, h / 2, -8.25f), new Vector3(17, h, 0.5f), wallMat);
            Box("WallE", root, new Vector3(8.25f, h / 2, 0), new Vector3(0.5f, h, 17), wallMat);
            Box("WallW", root, new Vector3(-8.25f, h / 2, 0), new Vector3(0.5f, h, 17), wallMat);

            var roomLight = new GameObject("RoomLight").AddComponent<Light>();
            roomLight.transform.SetParent(root, false);
            roomLight.transform.localPosition = new Vector3(0, 3.5f, 0);
            roomLight.type = LightType.Point; roomLight.range = 22f; roomLight.intensity = 3f;

            if (!withAlcove)
            {
                Box("WallN", root, new Vector3(0, h / 2, 8.25f), new Vector3(17, h, 0.5f), wallMat);
                return root;
            }

            // Kısmi duvar + 3x3 kapı boşluğu (kapı paneli RoomVisuals tarafından açılır/kapanır) + arkada alkov
            Box("WallN_L", root, new Vector3(-5f, h / 2, 8.25f), new Vector3(7f, h, 0.5f), wallMat);
            Box("WallN_R", root, new Vector3(5f, h / 2, 8.25f), new Vector3(7f, h, 0.5f), wallMat);
            Box("WallN_Lintel", root, new Vector3(0, 3.5f, 8.25f), new Vector3(3f, 1f, 0.5f), wallMat);
            Box("AlcoveFloor", root, new Vector3(0, -0.25f, 10.625f), new Vector3(17, 0.5f, 4.25f), floorMat);
            Box("AlcoveCeiling", root, new Vector3(0, h + 0.25f, 10.625f), new Vector3(17, 0.5f, 4.25f), wallMat);
            Box("AlcoveWallN", root, new Vector3(0, h / 2, 12.75f), new Vector3(17, h, 0.5f), wallMat);
            Box("AlcoveWallE", root, new Vector3(8.25f, h / 2, 10.625f), new Vector3(0.5f, h, 4.25f), wallMat);
            Box("AlcoveWallW", root, new Vector3(-8.25f, h / 2, 10.625f), new Vector3(0.5f, h, 4.25f), wallMat);
            var alcoveLight = new GameObject("AlcoveLight").AddComponent<Light>();
            alcoveLight.transform.SetParent(root, false);
            alcoveLight.transform.localPosition = new Vector3(0, 3.5f, 10.6f);
            alcoveLight.type = LightType.Point; alcoveLight.range = 12f; alcoveLight.intensity = 3f;
            return root;
        }

        static GameObject MakeButtonProp(string name, Transform parent, Vector3 pos, Color color, InteractId id, string label)
        {
            var b = Box(name, parent, pos, new Vector3(0.7f, 1.2f, 0.4f), MakeMat(name + "Mat", color));
            b.isStatic = false;
            var i = b.AddComponent<Interactable>();
            i.Id = id; i.Label = label;
            return b;
        }

        static void BuildWorld()
        {
            var orangeFloor = MakeMat("FloorOrange", new Color(0.95f, 0.5f, 0.12f));
            var blueFloor = MakeMat("FloorBlue", new Color(0.15f, 0.4f, 0.9f));
            var wallA = MakeMat("WallA", new Color(0.55f, 0.45f, 0.38f));
            var wallB = MakeMat("WallB", new Color(0.35f, 0.42f, 0.5f));

            var roomA = BuildRoom("RoomA_Mekanizma", Layout.RoomA, orangeFloor, wallA, false);
            var roomB = BuildRoom("RoomB_Sarnic", Layout.RoomB, blueFloor, wallB, true);

            // Oda A: lamba (tavanda asılı) + kapıyı kontrol eden buton
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Lamp";
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.transform.SetParent(roomA, false);
            lamp.transform.localPosition = new Vector3(0, 3.1f, 0);
            lamp.transform.localScale = Vector3.one * 0.7f;
            lamp.GetComponent<Renderer>().sharedMaterial = MakeMat("LampMat", new Color(0.2f, 0.2f, 0.2f));
            var lampLight = new GameObject("LampLight").AddComponent<Light>();
            lampLight.transform.SetParent(lamp.transform, false);
            lampLight.type = LightType.Point; lampLight.range = 18f; lampLight.intensity = 8f;
            lampLight.color = new Color(1f, 0.9f, 0.6f);
            lampLight.enabled = false;
            MakeButtonProp("DoorButton", roomA, new Vector3(-5f, 0.6f, -6f), new Color(0.85f, 0.15f, 0.15f), InteractId.DoorButton, "Kapıyı aç/kapat");

            // Oda B: lambayı kontrol eden buton + alkov kapısı
            MakeButtonProp("LampButton", roomB, new Vector3(5f, 0.6f, -6f), new Color(0.15f, 0.8f, 0.3f), InteractId.LampButton, "Lambayı aç/kapat");
            var door = Box("Door", roomB, new Vector3(0, 1.5f, 8.25f), new Vector3(3f, 3f, 0.3f), MakeMat("DoorMat", new Color(0.4f, 0.25f, 0.1f)));
            door.isStatic = false;

            var vis = new GameObject("RoomVisuals").AddComponent<RoomVisuals>();
            var so = new SerializedObject(vis);
            SetRef(so, "lampLight", lampLight);
            SetRef(so, "lampRenderer", lamp.GetComponent<Renderer>());
            SetRef(so, "door", door.transform);
            SetRef(so, "doorCollider", door.GetComponent<Collider>());
            so.ApplyModifiedPropertiesWithoutUndo();
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

        // ---------- Sahneler ----------

        static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(new Vector3(0, 1, -10), Vector3.zero, new Color(0.08f, 0.1f, 0.16f));
            cam.name = "Main Camera";

            var root = MakeCanvas();
            var canvas = MakeSafeArea(root);
            MakeText(canvas, "Title", "CoopGame", 80, new Vector2(0, 410), new Vector2(900, 110), TextAnchor.MiddleCenter);
            var input = MakeInput(canvas, "CodeInput", new Vector2(0, 240), new Vector2(560, 120));
            var join = MakeButton(canvas, "JoinButton", "Katıl", new Vector2(0, 90), new Vector2(560, 110));
            var create = MakeButton(canvas, "CreateButton", "Oda Kur", new Vector2(0, -50), new Vector2(560, 110));
            var status = MakeText(canvas, "Status", "", 40, new Vector2(0, -230), new Vector2(1500, 220), TextAnchor.UpperCenter);

            var lobby = new GameObject("LobbyUI").AddComponent<LobbyUI>();
            var so = new SerializedObject(lobby);
            SetRef(so, "codeInput", input);
            SetRef(so, "createButton", create);
            SetRef(so, "joinButton", join);
            SetRef(so, "status", status);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, LobbyPath);
        }

        static void CreateGameScene(GameObject avatarPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Üçüncü şahıs takip kamerası (yerel avatarı bulur).
            var cam = MakeCamera(new Vector3(0, 4, -9), new Vector3(0, 0.5f, 0), new Color(0.55f, 0.75f, 0.95f));
            cam.name = "Main Camera";
            cam.gameObject.AddComponent<FollowCamera>();
            new GameObject("InputRelay").AddComponent<InputRelay>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
            light.intensity = 1f;
            light.shadows = LightShadows.Hard; // tek ana ışık, sert gölge (mobil için hafif)

            BuildWorld();

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            var sso = new SerializedObject(session);
            SetRef(sso, "avatarPrefab", avatarPrefab);
            SetRef(sso, "roomStatePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(RoomStatePath));
            sso.ApplyModifiedPropertiesWithoutUndo();

            var root = MakeCanvas();
            var canvas = MakeSafeArea(root);
            var TL = new Vector2(0f, 1f); var TC = new Vector2(0.5f, 1f); var TR = new Vector2(1f, 1f);
            var role = MakeText(canvas, "RoleText", "Rol: -", 80, Vector2.zero, new Vector2(700, 110), TextAnchor.MiddleCenter);
            Corner(role, TC, TC, new Vector2(0, -40));
            var info = MakeText(canvas, "InfoText", "", 34, Vector2.zero, new Vector2(1300, 90), TextAnchor.UpperCenter);
            Corner(info, TC, TC, new Vector2(0, -150));
            var roomLabel = MakeText(canvas, "RoomText", "", 38, Vector2.zero, new Vector2(900, 110), TextAnchor.UpperCenter);
            Corner(roomLabel, TC, TC, new Vector2(0, -230));
            roomLabel.raycastTarget = false;

            var dots = new Image[2];
            var labels = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                float y = -110 - i * 65;
                var dot = MakeImage(canvas, "SpeakDot" + (i == 0 ? "A" : "B"), Vector2.zero, new Vector2(44, 44));
                Corner(dot, TL, TL, new Vector2(60, y - 8));
                dots[i] = dot;
                labels[i] = MakeText(canvas, "SpeakLabel" + (i == 0 ? "A" : "B"), i == 0 ? "A" : "B", 38, Vector2.zero, new Vector2(380, 60), TextAnchor.MiddleLeft);
                Corner(labels[i], TL, TL, new Vector2(120, y));
            }
            // Bakış alanı: ekranın sağ yarısı (arkada), sanal joystick sol altta, Çık butonu en üstte.
            var lookGo = new GameObject("LookPad", typeof(RectTransform));
            var lrt = lookGo.GetComponent<RectTransform>();
            lrt.SetParent(root, false); // safe area dışına da dokunuşu yakalasın
            lrt.anchorMin = new Vector2(0.5f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            lookGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            lookGo.AddComponent<LookPad>();
            lookGo.transform.SetAsFirstSibling();

            var stickBg = MakeImage(canvas, "Joystick", Vector2.zero, new Vector2(340, 340));
            Corner(stickBg, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(240, 240)); // sol alt, 70 px kenar boşluğu
            stickBg.color = new Color(1, 1, 1, 0.15f);
            var handle = MakeImage(stickBg.rectTransform, "Handle", Vector2.zero, new Vector2(140, 140));
            handle.color = new Color(1, 1, 1, 0.45f);
            handle.raycastTarget = false;
            var js = stickBg.gameObject.AddComponent<VirtualJoystick>();
            var jso = new SerializedObject(js);
            SetRef(jso, "handle", handle.rectTransform);
            jso.ApplyModifiedPropertiesWithoutUndo();

            var perfText = MakeText(canvas, "PerfText", "FPS -", 34, Vector2.zero, new Vector2(480, 50), TextAnchor.MiddleLeft);
            Corner(perfText, TL, TL, new Vector2(60, -40));
            perfText.raycastTarget = false;
            var perf = new GameObject("PerfHud").AddComponent<PerfHud>();
            var pso = new SerializedObject(perf);
            SetRef(pso, "label", perfText);
            pso.ApplyModifiedPropertiesWithoutUndo();

            var leave = MakeButton(canvas, "LeaveButton", "Çık", Vector2.zero, new Vector2(200, 80));
            Corner(leave.targetGraphic, TR, TR, new Vector2(-60, -40));
            var camBtn = MakeButton(canvas, "CameraButton", "Kamera", Vector2.zero, new Vector2(200, 80));
            Corner(camBtn.targetGraphic, TR, TR, new Vector2(-60, -135));
            // Etkileşim düğmesi: yalnızca bir butona yaklaşınca görünür (sağ alt).
            var interactBtn = MakeButton(canvas, "InteractButton", "Etkileş", Vector2.zero, new Vector2(420, 130));
            Corner(interactBtn.targetGraphic, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-80, 90));
            var prompt = new GameObject("InteractPrompt").AddComponent<InteractPrompt>();
            var pro = new SerializedObject(prompt);
            SetRef(pro, "button", interactBtn);
            pro.ApplyModifiedPropertiesWithoutUndo();

            var camToggle = new GameObject("CameraToggle").AddComponent<CameraToggle>();
            var cto = new SerializedObject(camToggle);
            SetRef(cto, "button", camBtn);
            cto.ApplyModifiedPropertiesWithoutUndo();

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            var so = new SerializedObject(hud);
            SetRef(so, "roleText", role);
            SetRef(so, "infoText", info);
            SetRef(so, "roomText", roomLabel);
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
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // yatayda yükseklik sabit (1080): geniş ekranda üst HUD kırpılmaz
            go.AddComponent<GraphicRaycaster>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Tüm HUD/Lobby UI'ının altına konduğu panel; anchor'ları Screen.safeArea'ya göre çalışma anında güncellenir.</summary>
        static RectTransform MakeSafeArea(RectTransform canvas)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<SafeAreaPanel>();
            return rt;
        }

        /// <summary>Öğeyi bir köşeye/kenara bağlar (pivot sabitlenir): safe area daralınca öğe içeride kalır.</summary>
        static void Corner(Graphic g, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
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
