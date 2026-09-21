using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MUSCA.Gate3D.Editor
{
    public static class CombatSandboxBuilder
    {
        private const string SourceScene = "Assets/MUSCA/Scenes/GateLab_FormV03_Playable.unity";
        private const string TargetScene = "Assets/MUSCA/Scenes/CombatSandbox_v01.unity";
        private const string SentinelAsset = "Assets/MUSCA/Art/CombatV01/Sentinel_FormProxy_v0.1.fbx";
        private const string MaterialRoot = "Assets/MUSCA/Art/FormV03/UnityMaterials";

        [MenuItem("MUSCA/Combat/Build Sandbox v0.2")]
        public static void Build()
        {
            RequireAsset(SourceScene);
            RequireAsset(SentinelAsset);

            Scene scene = OpenOrCreateTargetScene();

            GameObject player = FindRoot(scene, "Player");
            GameObject functionEnvironment = FindRoot(scene, "Function_Environment");
            EnsureFunctionFloorCollider(functionEnvironment);

            FirstPersonController movement = player.GetComponent<FirstPersonController>();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (movement == null || camera == null)
            {
                throw new InvalidOperationException(
                    "Combat sandbox requires the existing player controller and camera.");
            }

            PlayerMeleeCombat combat = player.GetComponent<PlayerMeleeCombat>();
            if (combat == null) combat = player.AddComponent<PlayerMeleeCombat>();
            combat.Configure(camera, 34f);

            PlayerCombatVitals vitals = player.GetComponent<PlayerCombatVitals>();
            if (vitals == null) vitals = player.AddComponent<PlayerCombatVitals>();
            vitals.Configure(100f, 100f, 28f, 0.65f, 1.25f);

            PlayerDodgeController dodge = player.GetComponent<PlayerDodgeController>();
            if (dodge == null) dodge = player.AddComponent<PlayerDodgeController>();

            PlayerLockOn lockOn = player.GetComponent<PlayerLockOn>();
            if (lockOn == null) lockOn = player.AddComponent<PlayerLockOn>();

            GameObject sandbox = FindRootOptional(scene, "CombatSandbox_v01");
            if (sandbox == null) sandbox = new GameObject("CombatSandbox_v01");

            GameObject sentinel = GetOrCreateSentinel(sandbox.transform);
            sentinel.transform.SetParent(sandbox.transform, false);
            sentinel.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, 4.6f), Quaternion.identity);
            RemapMaterials(sentinel, LoadMaterialMap());
            AlignFeetToWorld(sentinel, 0f);

            CapsuleCollider capsule = sentinel.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = sentinel.AddComponent<CapsuleCollider>();
            ConfigureWorldCapsule(capsule, 1.76f, 0.42f, 0.88f);

            CombatDamageReceiver receiver = sentinel.GetComponent<CombatDamageReceiver>();
            if (receiver == null) receiver = sentinel.AddComponent<CombatDamageReceiver>();
            receiver.Configure(100f, 1.75f);

            GameObject telegraph = EnsureTelegraphMarker(
                sandbox.transform, sentinel.transform.position);

            KaelPredictionProbe prediction = sentinel.GetComponent<KaelPredictionProbe>();
            if (prediction == null) prediction = sentinel.AddComponent<KaelPredictionProbe>();
            prediction.Configure(dodge, vitals);
            prediction.SetPrototypeActive(false, true);

            SentinelCombatBrain brain = sentinel.GetComponent<SentinelCombatBrain>();
            if (brain == null) brain = sentinel.AddComponent<SentinelCombatBrain>();
            brain.Configure(vitals, telegraph.transform, prediction);

            GameObject hudRoot = EnsureChild(sandbox.transform, "CombatSandboxHUD");
            CombatSandboxHud hud = hudRoot.GetComponent<CombatSandboxHud>();
            if (hud == null) hud = hudRoot.AddComponent<CombatSandboxHud>();
            hud.Configure(receiver, combat, vitals, dodge, lockOn, brain, prediction);

            GateHud gateHud = UnityEngine.Object.FindAnyObjectByType<GateHud>(
                FindObjectsInactive.Include);
            if (gateHud != null) gateHud.enabled = false;

            EnsureArenaMarkers(sandbox.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TargetScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = sentinel;
            Debug.Log($"MUSCA_COMBAT_SANDBOX_BUILT scene={TargetScene}");
        }

        private static Scene OpenOrCreateTargetScene()
        {
            SceneAsset target = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene);
            if (target != null)
            {
                return EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            }

            Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, TargetScene, false))
            {
                throw new InvalidOperationException("Could not create combat sandbox scene copy.");
            }

            return source;
        }

        private static GameObject GetOrCreateSentinel(Transform sandbox)
        {
            Transform existing = sandbox.Find("Sentinel_v01");
            if (existing != null) return existing.gameObject;

            GameObject sentinel = InstantiateModel(SentinelAsset, "Sentinel_v01");
            sentinel.transform.SetParent(sandbox, false);
            return sentinel;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ConfigureWorldCapsule(
            CapsuleCollider capsule, float worldHeight, float worldRadius, float worldCenterY)
        {
            Vector3 scale = capsule.transform.lossyScale;
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
            float scaleRadius = Mathf.Max(
                0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));

            capsule.direction = 1;
            capsule.center = new Vector3(0f, worldCenterY / scaleY, 0f);
            capsule.height = worldHeight / scaleY;
            capsule.radius = worldRadius / scaleRadius;
        }

        private static GameObject EnsureTelegraphMarker(Transform parent, Vector3 position)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/AccentOrange.mat");
            GameObject marker = EnsurePrimitive(
                parent, "SentinelTelegraph", PrimitiveType.Cylinder);
            marker.transform.position = new Vector3(position.x, 0.04f, position.z);
            marker.transform.localScale = new Vector3(1.15f, 0.015f, 1.15f);
            RemoveCollider(marker);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            marker.SetActive(false);
            return marker;
        }

        private static void EnsureArenaMarkers(Transform parent)
        {
            Material cyan = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/CyanSoft.mat");
            Material amber = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/AccentOrange.mat");
            EnsureMarker(
                "CombatBoundary_Left", parent,
                new Vector3(-2.6f, 0.03f, 4.6f),
                new Vector3(0.05f, 0.04f, 5.2f), cyan);
            EnsureMarker(
                "CombatBoundary_Right", parent,
                new Vector3(2.6f, 0.03f, 4.6f),
                new Vector3(0.05f, 0.04f, 5.2f), cyan);
            EnsureMarker(
                "CombatFocus", parent,
                new Vector3(0f, 0.035f, 4.6f),
                new Vector3(1.1f, 0.045f, 1.1f), amber);
        }

        private static void EnsureMarker(
            string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject marker = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            marker.transform.position = position;
            marker.transform.localScale = scale;
            RemoveCollider(marker);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
        }

        private static GameObject EnsurePrimitive(
            Transform parent, string name, PrimitiveType primitiveType)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject item = GameObject.CreatePrimitive(primitiveType);
            item.name = name;
            item.transform.SetParent(parent, false);
            return item;
        }

        private static void RemoveCollider(GameObject item)
        {
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void RequireAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                throw new InvalidOperationException($"Missing required asset: {path}");
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject root = FindRootOptional(scene, name);
            if (root != null) return root;
            throw new InvalidOperationException($"Missing root: {name}");
        }

        private static GameObject FindRootOptional(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private static void EnsureFunctionFloorCollider(GameObject root)
        {
            Transform floor = root.transform.Find("Floor");
            BoxCollider collider = floor != null ? floor.GetComponent<BoxCollider>() : null;
            if (collider == null)
            {
                throw new InvalidOperationException(
                    "Combat sandbox requires Function_Environment/Floor BoxCollider.");
            }
            collider.enabled = true;
        }

        private static GameObject InstantiateModel(string path, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Model could not be loaded: {path}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            return instance;
        }

        private static Dictionary<string, Material> LoadMaterialMap()
        {
            var map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            Add(map, "CombatV01_DarkMetal", "DarkMetal");
            Add(map, "CombatV01_Ceramic", "MuscaCeramic");
            Add(map, "CombatV01_Cyan", "Cyan");
            Add(map, "CombatV01_Amber", "AccentOrange");
            return map;
        }

        private static void Add(
            Dictionary<string, Material> map, string source, string target)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/{target}.mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Missing material: {target}");
            }
            map[source] = material;
        }

        private static void RemapMaterials(
            GameObject root, Dictionary<string, Material> map)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source != null &&
                        map.TryGetValue(source.name, out Material replacement))
                    {
                        materials[i] = replacement;
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static void AlignFeetToWorld(GameObject visual, float floorY)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 position = visual.transform.position;
            position.y += floorY - bounds.min.y;
            visual.transform.position = position;
        }
    }
}
