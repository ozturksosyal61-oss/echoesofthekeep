using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Mixamo karakterleri (Assets/ThirdParty/Characters/X|O: {x|o}_Idle.fbx = skin + Idle, {x|o}_walking.fbx = Walk).
    /// Humanoid, dokular çıkarılır (1024, ASTC), clip'ler in-place loop, ortak Character.controller (Speed),
    /// malzemeler palete çekilir, O'ya fener + defter. Avatar prefabında Model/Model_X ve Model/Model_O (PlayerAvatar rolüne göre açar).
    /// </summary>
    public static partial class CoopSetup
    {
        const string CharRoot = "Assets/ThirdParty/Characters";
        const string CharControllerPath = "Assets/Prefabs/Character.controller";
        static readonly string[] CharRoles = { "X", "O" };

        static string CharIdle(string r) => CharRoot + "/" + r + "/" + r.ToLowerInvariant() + "_Idle.fbx";
        static string CharWalk(string r) => CharRoot + "/" + r + "/" + r.ToLowerInvariant() + "_walking.fbx";
        static bool CharactersAvailable => CharRoles.All(r => File.Exists(CharIdle(r)) && File.Exists(CharWalk(r)));

        static void ConfigureClip(ModelImporter mi, string clipName)
        {
            var src = mi.defaultClipAnimations;
            if (src == null || src.Length == 0) { Debug.LogWarning("[CoopSetup] Animasyon yok: " + mi.assetPath); return; }
            var c = src[0];
            c.name = clipName;
            c.loopTime = true; c.loopPose = clipName == "Walk";
            c.lockRootRotation = true; c.keepOriginalOrientation = true;      // kök dönüşü poza göm
            c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;     // in-place
            mi.clipAnimations = new[] { c };
        }

        static Avatar LoadAvatar(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        static AnimationClip LoadClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        /// <summary>İçe aktarma ayarları (yalnızca değişince yeniden içe aktarır).</summary>
        static void PrepareCharacters()
        {
            foreach (var r in CharRoles)
            {
                string idle = CharIdle(r), walk = CharWalk(r), texDir = CharRoot + "/" + r + "/Textures";
                var mi = (ModelImporter)AssetImporter.GetAtPath(idle);
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.skinWeights = ModelImporterSkinWeights.Custom; mi.maxBonesPerVertex = 4; mi.minBoneWeight = 0.01f;
                mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                mi.meshCompression = ModelImporterMeshCompression.Medium;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                ConfigureClip(mi, "Idle");
                mi.SaveAndReimport();
                if (!Directory.Exists(texDir) || Directory.GetFiles(texDir, "*.png").Length + Directory.GetFiles(texDir, "*.jpg").Length == 0)
                {
                    Directory.CreateDirectory(texDir);
                    if (!mi.ExtractTextures(texDir)) Debug.LogWarning("[CoopSetup] Doku çıkarılamadı: " + idle);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                foreach (var tp in Directory.GetFiles(texDir).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg")))
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(tp.Replace('\\', '/'));
                    if (ti == null) continue;
                    ti.maxTextureSize = 1024; ti.mipmapEnabled = true;
                    foreach (var plat in new[] { "Android", "iPhone" })
                    {
                        var ps = ti.GetPlatformTextureSettings(plat);
                        ps.overridden = true; ps.maxTextureSize = 1024; ps.format = TextureImporterFormat.ASTC_6x6;
                        ti.SetPlatformTextureSettings(ps);
                    }
                    ti.SaveAndReimport();
                }
                mi.materialImportMode = ModelImporterMaterialImportMode.None; // kendi malzememiz (palet)
                mi.SaveAndReimport();

                var wi = (ModelImporter)AssetImporter.GetAtPath(walk);
                wi.animationType = ModelImporterAnimationType.Human;
                wi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                wi.sourceAvatar = LoadAvatar(idle);
                wi.materialImportMode = ModelImporterMaterialImportMode.None;
                wi.importBlendShapes = false;
                ConfigureClip(wi, "Walk");
                wi.SaveAndReimport();
                var av = LoadAvatar(idle);
                if (av == null || !av.isHuman || !av.isValid) Debug.LogError("[CoopSetup] Humanoid avatar geçersiz: " + idle);
            }
        }

        /// <summary>Ortak controller: Idle ↔ Walk (Speed). X ve O aynı controller'ı kullanır (clip'ler humanoid, retarget).</summary>
        static AnimatorController BuildCharacterController()
        {
            AssetDatabase.DeleteAsset(CharControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CharControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = LoadClip(CharIdle("X"));
            var sWalk = sm.AddState("Walk"); sWalk.motion = LoadClip(CharWalk("X"));
            sm.defaultState = sIdle;
            var t1 = sIdle.AddTransition(sWalk); t1.hasExitTime = false; t1.duration = 0.15f;
            t1.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");
            var t2 = sWalk.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.2f;
            t2.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
            return ctrl;
        }

        static Material CharMat(string r)
        {
            string texDir = CharRoot + "/" + r + "/Textures";
            var tex = Directory.Exists(texDir)
                ? Directory.GetFiles(texDir).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg")).OrderBy(f => f)
                    .Select(f => AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'))).FirstOrDefault(t => t != null)
                : null;
            // ref karakter_sheet.jpg: X koyu kahve deri + haki kanvas (sıcak), O koyu füme-yeşil palto + soluk mavi şerit (soğuk).
            // Doku çarpanı hafif: dokudaki ayrıntı korunur, doygunluk artmaz.
            // Doku referanstan koyu: X'i sıcak yönde biraz aç (deri kahvesi), O'yu soğuk yeşile çek (çarpan >1 bilerek)
            var tint = r == "X" ? new Color(1.3f, 1.1f, 0.88f) : new Color(0.98f, 1.1f, 1.04f);
            var m = MakeMat("M_Char" + r, tint);
            if (tex != null) m.SetTexture("_BaseMap", tex); else { m.color = r == "X" ? HexC("#8A5A2B") : HexC("#3B4A44"); Debug.LogWarning("[CoopSetup] Karakter dokusu yok: " + r); }
            m.SetFloat("_Smoothness", 0.22f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Color HexC(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        /// <summary>Karakter örneği (unpacked): ölçek 1.75 m, Animator (ortak controller), Bone2 skinning, LOD kesme, malzeme.</summary>
        static GameObject BuildCharacter(string r, Transform parent, AnimatorController ctrl)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CharIdle(r));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Model_" + r;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;

            var mat = CharMat(r);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var b = new Bounds(); bool hasB = false;
            for (int i = 0; i < smrs.Length; i++)
            {
                var s = smrs[i];
                s.sharedMaterials = Enumerable.Repeat(mat, s.sharedMaterials.Length).ToArray();
                s.quality = SkinQuality.Bone2;          // mobil: köşe başı en fazla 2 kemik
                s.skinnedMotionVectors = false;
                s.shadowCastingMode = ShadowCastingMode.On;
                s.lightProbeUsage = LightProbeUsage.BlendProbes;
                s.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                s.updateWhenOffscreen = false;
                // Gerçek boy: pozlanmış ağ (skinned bounds dolgulu olduğundan yanıltıcı)
                var baked = new Mesh(); s.BakeMesh(baked, true);
                foreach (var v in baked.vertices) { var w = s.transform.TransformPoint(v); if (!hasB) { b = new Bounds(w, Vector3.zero); hasB = true; } else b.Encapsulate(w); }
                Object.DestroyImmediate(baked);
            }
            float h = b.size.y;
            if (h > 0.01f && Mathf.Abs(h - 1.75f) > 0.3f) go.transform.localScale = Vector3.one * (1.75f / h);

            var an = go.GetComponent<Animator>(); if (an == null) an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.avatar = LoadAvatar(CharIdle(r));
            an.applyRootMotion = false;
            an.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            if (r == "O") AttachOProps(go, an);

            var lod = go.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(0.02f, go.GetComponentsInChildren<Renderer>(true)) }); // uzakta (ekranın %2'si) çizme
            lod.RecalculateBounds();
            Debug.Log("[CoopSetup] Karakter " + r + ": " + smrs.Sum(s => s.sharedMesh != null ? s.sharedMesh.triangles.Length / 3 : 0) + " üçgen, " +
                      smrs.Sum(s => s.bones.Length) + " kemik, boy " + h.ToString("0.00") + " m");
            return go;
        }

        /// <summary>O (ref karakter_sheet.jpg): sol kalçada kemerden sarkan fırtına feneri (soluk ay mavisi cam, gerçek ışık yok →
        /// ışık bütçesine girmez), sağ kalçada kayışlı deri defter. Ön +Z, karakterin solu -X.</summary>
        static void AttachOProps(GameObject go, Animator an)
        {
            var hips = an.isHuman ? an.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null) hips = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("Hips"));
            if (hips == null) { Debug.LogWarning("[CoopSetup] O kalça kemiği bulunamadı, prop eklenmedi"); return; }
            var root = go.transform;
            var glass = GlowMat("K_MoonLanternGlass", new Color(0.42f, 0.5f, 0.62f), new Color(0.55f, 0.66f, 0.9f) * 0.8f, 0.7f);
            var iron = KitMat("K_Iron", K != null ? K.iron : null);
            var leather = KitMat("K_Leather", K != null ? K.leather : null);
            var brass = KitMat("K_Brass", K != null ? K.brass : null);
            var pages = MakeMat("K_JournalPages", HexC("#CDBE9A"));

            // Fırtına feneri (~0.24 m): alt hazne, küre cam, 4 tel koruma, üst kapak + baca, tepe halkası
            var lantern = new GameObject("PropLantern").transform;
            lantern.SetParent(root, false);
            lantern.localPosition = new Vector3(-0.16f, 0.80f, 0.13f);
            Prim("Tank", lantern, PrimitiveType.Cylinder, new Vector3(0, -0.085f, 0), new Vector3(0.085f, 0.018f, 0.085f), iron);
            Prim("Globe", lantern, PrimitiveType.Sphere, new Vector3(0, -0.01f, 0), new Vector3(0.075f, 0.105f, 0.075f), glass);
            Prim("Cap", lantern, PrimitiveType.Cylinder, new Vector3(0, 0.055f, 0), new Vector3(0.07f, 0.012f, 0.07f), iron);
            Prim("Chimney", lantern, PrimitiveType.Cylinder, new Vector3(0, 0.075f, 0), new Vector3(0.03f, 0.012f, 0.03f), iron);
            for (int i = 0; i < 4; i++)
            {
                float ang = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Prim("Guard", lantern, PrimitiveType.Cube, new Vector3(Mathf.Cos(ang) * 0.043f, -0.012f, Mathf.Sin(ang) * 0.043f), new Vector3(0.005f, 0.13f, 0.005f), iron);
            }
            MeshObj("Ring", lantern, new Vector3(0, 0.1f, 0), RingMesh(0.028f, 0.005f, 0.005f), iron).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Prim("Hook", lantern, PrimitiveType.Cube, new Vector3(0, 0.14f, 0), new Vector3(0.005f, 0.04f, 0.005f), leather);

            // Deri defter (~0.15 x 0.2): kapak, sayfalar, dikey kayış + pirinç toka
            var book = new GameObject("PropJournal").transform;
            book.SetParent(root, false);
            book.localPosition = new Vector3(0.22f, 0.9f, 0.05f);
            book.localRotation = Quaternion.Euler(0, -12f, -6f);
            Slab("Cover", book, Vector3.zero, new Vector3(0.038f, 0.2f, 0.15f), leather, 1f, false);
            Slab("Pages", book, new Vector3(0.002f, 0, 0.004f), new Vector3(0.03f, 0.19f, 0.145f), pages, 1f, false);
            Slab("Strap", book, new Vector3(0.0205f, 0, 0.02f), new Vector3(0.004f, 0.21f, 0.022f), leather, 1f, false);
            Slab("Buckle", book, new Vector3(0.023f, 0.02f, 0.02f), new Vector3(0.004f, 0.025f, 0.028f), brass, 1f, false);
            foreach (var t in new[] { lantern, book })
            {
                foreach (var mr in t.GetComponentsInChildren<MeshRenderer>()) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.lightProbeUsage = LightProbeUsage.BlendProbes; }
                foreach (var tr in t.GetComponentsInChildren<Transform>()) tr.gameObject.isStatic = false;
                t.SetParent(hips, true);
            }
        }

        /// <summary>Kit malzemesi (Setup All'da K hazır; tek başına çalışırken diskten).</summary>
        static Material KitMat(string name, Material live) =>
            live != null ? live : AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Kit/Materials/" + name + ".mat") ?? MakeMat(name, new Color(0.23f, 0.22f, 0.21f));

        /// <summary>Avatar modeli: Mixamo karakterleri varsa Model/Model_X + Model_O, yoksa eski primitif gövde.</summary>
        static bool BuildCharacterModels(Transform avatarRoot)
        {
            if (!CharactersAvailable) { Debug.LogWarning("[CoopSetup] Mixamo karakterleri yok, primitif avatar kullanılıyor"); return false; }
            PrepareCharacters();
            var ctrl = BuildCharacterController();
            string oldDir = matDir; matDir = "Assets/Materials"; // karakter malzemeleri her yoldan aynı yere
            var model = new GameObject("Model").transform;
            model.SetParent(avatarRoot, false);
            BuildCharacter("X", model, ctrl);
            BuildCharacter("O", model, ctrl).SetActive(false);
            matDir = oldDir;
            return true;
        }

        /// <summary>Batch: yalnızca avatar prefabı (karakterler) + görüntüler; sahneleri yeniden üretmez.
        /// -executeMethod CoopGame.EditorTools.CoopSetup.CharactersAndShots -shotDir Docs/Shots/characters</summary>
        public static void CharactersAndShots()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateAvatarPrefab();
            AssetDatabase.SaveAssets();
            CharacterShots();
        }

        // ---------- Karakter testi: Lobi Holü'nde X ve O yan yana (Idle + Walk karesi), ekran görüntüsü ----------
        [MenuItem("CoopGame/Karakter Testi (görüntü)")]
        public static void CharacterShots()
        {
            string dir = Arg("-shotDir") ?? "Docs/Shots/characters";
            Directory.CreateDirectory(dir);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GamePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);
            var h = Layout.Hall;
            var spawned = new System.Collections.Generic.List<GameObject>();
            void Pose(string r, Vector3 p, float yaw, string clip, float t)
            {
                var src = prefab.transform.Find("Model/Model_" + r);
                if (src == null) { Debug.LogError("[CoopSetup] Prefabda Model_" + r + " yok"); return; }
                var g = Object.Instantiate(src.gameObject); g.SetActive(true); spawned.Add(g);
                g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
                var c = AssetDatabase.LoadAllAssetsAtPath(clip == "Walk" ? CharWalk(r) : CharIdle(r)).OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview__"));
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(g, c, t * c.length); AnimationMode.EndSampling();
            }
            var basePos = h + new Vector3(0, 0.05f, -6.5f);
            // Yalnızca çekim için geçici dolgu ışığı (sahneye kaydedilmez)
            var fill = new GameObject("ShotFill").AddComponent<Light>();
            fill.type = LightType.Point; fill.color = new Color(1f, 0.85f, 0.7f); fill.intensity = 2.5f; fill.range = 9f; fill.shadows = LightShadows.None;
            fill.lightmapBakeType = LightmapBakeType.Realtime;
            fill.transform.position = basePos + new Vector3(0.5f, 2.2f, -2.2f);
            Pose("X", basePos + new Vector3(-0.7f, 0, 0), 180f, "Idle", 0.3f);
            Pose("O", basePos + new Vector3(0.7f, 0, 0), 180f, "Idle", 0.3f);
            Shot(dir + "/chars_idle.png", basePos + new Vector3(0, 1.3f, -3.4f), basePos + new Vector3(0, 0.95f, 0), false);
            AnimationMode.StopAnimationMode();
            foreach (var g in spawned) Object.DestroyImmediate(g);
            spawned.Clear();
            Pose("X", basePos + new Vector3(-0.8f, 0, 0), 120f, "Walk", 0.25f);
            Pose("O", basePos + new Vector3(0.8f, 0, 0), 120f, "Walk", 0.25f);
            Shot(dir + "/chars_walk.png", basePos + new Vector3(0, 1.2f, -3.4f), basePos + new Vector3(0, 0.9f, 0), false);
            AnimationMode.StopAnimationMode();
            foreach (var g in spawned) Object.DestroyImmediate(g);
            spawned.Clear();
            // Palto-bacak: O yandan, adımın iki ucu (öne ve arkaya açılmış bacak)
            foreach (var t in new[] { 0.0f, 0.25f, 0.5f })
            {
                Pose("O", basePos, 90f, "Walk", t);
                fill.transform.position = basePos + new Vector3(0.3f, 1.6f, -1.8f);
                Shot(dir + "/o_walk_side_" + (int)(t * 100) + ".png", basePos + new Vector3(0, 0.95f, -2.3f), basePos + new Vector3(0, 0.8f, 0), false);
                AnimationMode.StopAnimationMode();
                foreach (var g in spawned) Object.DestroyImmediate(g);
                spawned.Clear();
            }
            Object.DestroyImmediate(fill.gameObject);
            Debug.Log("[CoopSetup] Karakter görüntüleri -> " + Path.GetFullPath(dir));
        }
    }
}
