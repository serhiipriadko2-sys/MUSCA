using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MUSCA.Gate3D.Editor
{
    public static class FormV03GameplayIntegrator
    {
        private const string SourceScene = "Assets/MUSCA/Scenes/GateLab.unity";
        private const string TargetScene = "Assets/MUSCA/Scenes/GateLab_FormV03_Playable.unity";
        private const string ArtRoot = "Assets/MUSCA/Art/FormV03";
        private const string MaterialRoot = "Assets/MUSCA/Art/FormV03/UnityMaterials";

        [MenuItem("MUSCA/Form V03/Integrate Gameplay")]
        public static void Integrate()
        {
            RequireAsset(SourceScene);
            RequireAsset($"{ArtRoot}/GateLab_Form_v0.3.fbx");
            RequireAsset($"{ArtRoot}/MUSCA_FormProxy_v0.31.fbx");
            RequireAsset($"{ArtRoot}/Researcher_FormProxy_v0.31.fbx");

            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TargetScene, false))
                throw new InvalidOperationException("Could not create Form v0.3 playable scene copy.");

            GameObject functionEnvironment = FindRoot(scene, "Environment");
            GameObject player = FindRoot(scene, "Player");
            GameObject companion = FindRoot(scene, "MUSCA Companion");
            functionEnvironment.name = "Function_Environment";

            SetRenderers(functionEnvironment, false);
            SetColliders(functionEnvironment, false);
            SetRenderers(companion, false);

            GameObject formEnvironment = InstantiateModel($"{ArtRoot}/GateLab_Form_v0.3.fbx", "FormV03_Environment");
            formEnvironment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            Dictionary<string, Material> materials = LoadMaterialMap();
            RemapMaterials(formEnvironment, materials);
            AddFormColliders(formEnvironment);

            Transform functionGate = functionEnvironment.transform.Find("Gate");
            Transform functionLeft = RequireChild(functionGate, "LeftPanel");
            Transform functionRight = RequireChild(functionGate, "RightPanel");
            Transform visualLeft = FindDeep(formEnvironment.transform, "GateDoor_L");
            Transform visualRight = FindDeep(formEnvironment.transform, "GateDoor_R");

            GameObject bridgeObject = new GameObject("FormV03_GateBridge");
            FormV03GateVisualBridge bridge = bridgeObject.AddComponent<FormV03GateVisualBridge>();
            bridge.Configure(functionLeft, functionRight, visualLeft, visualRight);

            FirstPersonController playerController = player.GetComponent<FirstPersonController>();
            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            if (playerController == null || playerCamera == null)
                throw new InvalidOperationException("Player controller/camera missing.");
            playerController.ConfigureCamera(playerCamera, new Vector3(0.20f, 1.62f, -2.60f), 64f);
            playerController.SetCameraCollision(true, 0.22f, 0.08f);
            GateHud hud = UnityEngine.Object.FindFirstObjectByType<GateHud>();
            if (hud != null) hud.SetCompactMode(true);

            GameObject researcherVisual = InstantiateModel($"{ArtRoot}/Researcher_FormProxy_v0.31.fbx", "FormV03_Researcher_Visual");
            researcherVisual.transform.SetParent(player.transform, false);
            researcherVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            RemapMaterials(researcherVisual, materials);
            AlignFeetToParent(researcherVisual, player.transform);
            CharacterController character = player.GetComponent<CharacterController>();
            ResearcherPresentation presentation = player.AddComponent<ResearcherPresentation>();
            presentation.Configure(character, researcherVisual.transform,
                FindDeep(researcherVisual.transform, "P03_UpperArm_-1"), FindDeep(researcherVisual.transform, "P03_UpperArm_1"),
                FindDeep(researcherVisual.transform, "P03_Thigh_-1"), FindDeep(researcherVisual.transform, "P03_Thigh_1"));

            GameObject muscaVisual = InstantiateModel($"{ArtRoot}/MUSCA_FormProxy_v0.31.fbx", "FormV03_MUSCA_Visual");
            muscaVisual.transform.SetParent(companion.transform, false);
            muscaVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            muscaVisual.transform.localScale = Vector3.one * 1.28f;
            RemapMaterials(muscaVisual, materials);
            RecenterOnParent(muscaVisual, companion.transform);
            MuscaCompanion companionMotion = companion.GetComponent<MuscaCompanion>();
            if (companionMotion != null)
            {
                companionMotion.SetFollowTarget(player.transform);
                companionMotion.SetOffset(new Vector3(0.72f, 1.50f, 0.05f));
                companionMotion.ConfigureVisualWings(
                    FindDeep(muscaVisual.transform, "M03_WingUpper_-1"), FindDeep(muscaVisual.transform, "M03_WingUpper_1"),
                    FindDeep(muscaVisual.transform, "M03_WingLower_-1"), FindDeep(muscaVisual.transform, "M03_WingLower_1"));
                GateRuntime gateRuntime = UnityEngine.Object.FindFirstObjectByType<GateRuntime>();
                MuscaBehaviorDriver behaviorDriver = companion.GetComponent<MuscaBehaviorDriver>();
                if (behaviorDriver == null) behaviorDriver = companion.AddComponent<MuscaBehaviorDriver>();
                behaviorDriver.Configure(gateRuntime, companionMotion);
            }

            CreateGameplayLights();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.30f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.11f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.028f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.018f, 0.035f, 0.045f);
            RenderSettings.fogDensity = 0.006f;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TargetScene, true) };
            EditorSceneManager.SaveScene(scene, TargetScene);
            Selection.activeGameObject = player;
            AssetDatabase.SaveAssets();
            Debug.Log($"MUSCA_FORM_V03_GAMEPLAY_INTEGRATED scene={TargetScene}");
        }

        private static void RequireAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                throw new InvalidOperationException($"Missing required asset: {path}");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            throw new InvalidOperationException($"Missing root: {name}");
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            if (child == null) throw new InvalidOperationException($"Missing child: {name}");
            return child;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            throw new InvalidOperationException($"Missing imported object: {name}");
        }

        private static GameObject InstantiateModel(string path, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            return instance;
        }

        private static void SetRenderers(GameObject root, bool enabled)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = enabled;
        }

        private static void SetColliders(GameObject root, bool enabled)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = enabled;
        }

        private static void AddFormColliders(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!NeedsCollider(t.name)) continue;
                MeshFilter filter = t.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                BoxCollider box = t.GetComponent<BoxCollider>();
                if (box == null) box = t.gameObject.AddComponent<BoxCollider>();
                box.center = filter.sharedMesh.bounds.center;
                box.size = filter.sharedMesh.bounds.size;
            }
        }

        private static bool NeedsCollider(string name)
        {
            return name == "Floor" || name == "Wall_W" || name == "Wall_E" ||
                   name.StartsWith("NorthWall_") || name.StartsWith("SouthWall_") ||
                   name == "GateDoor_L" || name == "GateDoor_R" ||
                   name == "AmberStation_Base" || name == "CobaltStation_Base" ||
                   name == "SectorBFloor" || name == "SectorBWallL" || name == "SectorBWallR" ||
                   name.StartsWith("Planter_") || name.StartsWith("RailPost_") || name.StartsWith("RailBar_") ||
                   name == "V03_ScannerBase" || name == "V03_SampleCanister";
        }

        private static Dictionary<string, Material> LoadMaterialMap()
        {
            var map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            Add(map, "M_DarkMetal", "DarkMetal"); Add(map, "V03_DarkMetal", "DarkMetal");
            Add(map, "M_Panel", "Panel"); Add(map, "V03_Panel", "Panel");
            Add(map, "M_Floor", "Floor"); Add(map, "M_Cyan", "Cyan"); Add(map, "V03_Cyan", "Cyan");
            Add(map, "V03_CyanSoft", "CyanSoft"); Add(map, "V03_WhiteLight", "WhiteLight");
            Add(map, "M_Amber", "Amber"); Add(map, "V03_Amber", "Amber");
            Add(map, "M_Cobalt", "Cobalt"); Add(map, "V03_Cobalt", "Cobalt");
            Add(map, "M_Plant", "Plant"); Add(map, "M_AccentOrange", "AccentOrange");
            Add(map, "V03_Ceramic", "MuscaCeramic"); Add(map, "V03_Glass", "MuscaGlass");
            Add(map, "V03_Armor", "ResearcherArmor"); Add(map, "V03_Suit", "ResearcherSuit");
            Add(map, "V03_Skin", "ResearcherSkin"); Add(map, "V03_Hair", "ResearcherHair");
            Add(map, "V03_Orange", "ResearcherOrange");
            return map;
        }

        private static void Add(Dictionary<string, Material> map, string source, string target)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{target}.mat");
            if (material == null) throw new InvalidOperationException($"Missing Unity material: {target}");
            map[source] = material;
        }

        private static void RemapMaterials(GameObject root, Dictionary<string, Material> map)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++)
                    if (source[i] != null && map.TryGetValue(source[i].name, out Material replacement)) source[i] = replacement;
                renderer.sharedMaterials = source;
            }
        }

        private static void RecenterOnParent(GameObject visual, Transform parent)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
            visual.transform.localPosition -= localCenter;
        }

        private static void AlignFeetToParent(GameObject visual, Transform parent)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            visual.transform.position += parent.position - anchor;
        }

        private static void CreateGameplayLights()
        {
            GameObject root = new GameObject("FormV03_GameplayLights");
            float[] zPositions = { 9f, 4f, -1f, -6f, -10.5f };
            foreach (float z in zPositions)
            {
                GameObject go = new GameObject($"NeutralFill_{z:0.0}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(0f, 3.6f, z);
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 7f;
                light.intensity = 1.6f;
                light.color = new Color(0.78f, 0.86f, 0.90f);
                light.shadows = LightShadows.None;
            }
        }
    }
}
