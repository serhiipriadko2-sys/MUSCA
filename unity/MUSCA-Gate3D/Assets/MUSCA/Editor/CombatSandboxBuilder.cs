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

        [MenuItem("MUSCA/Combat/Build Sandbox v0.1")]
        public static void Build()
        {
            RequireAsset(SourceScene);
            RequireAsset(SentinelAsset);

            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TargetScene, false))
            {
                throw new InvalidOperationException("Could not create combat sandbox scene copy.");
            }

            GameObject player = FindRoot(scene, "Player");
            GameObject functionEnvironment = FindRoot(scene, "Function_Environment");
            SetColliders(functionEnvironment, true);

            FirstPersonController movement = player.GetComponent<FirstPersonController>();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (movement == null || camera == null)
            {
                throw new InvalidOperationException("Combat sandbox requires the existing player controller and camera.");
            }

            PlayerMeleeCombat combat = player.GetComponent<PlayerMeleeCombat>();
            if (combat == null) combat = player.AddComponent<PlayerMeleeCombat>();
            combat.Configure(camera, 34f);

            GameObject oldSandbox = GameObject.Find("CombatSandbox_v01");
            if (oldSandbox != null) UnityEngine.Object.DestroyImmediate(oldSandbox);

            GameObject sandbox = new GameObject("CombatSandbox_v01");
            GameObject sentinel = InstantiateModel(SentinelAsset, "Sentinel_v01");
            sentinel.transform.SetParent(sandbox.transform, false);
            sentinel.transform.SetPositionAndRotation(new Vector3(0f, 0f, 4.6f), Quaternion.Euler(0f, 0f, 0f));
            RemapMaterials(sentinel, LoadMaterialMap());
            AlignFeetToWorld(sentinel, 0f);

            CapsuleCollider capsule = sentinel.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = sentinel.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.88f, 0f);
            capsule.height = 1.76f;
            capsule.radius = 0.42f;

            CombatDamageReceiver receiver = sentinel.GetComponent<CombatDamageReceiver>();
            if (receiver == null) receiver = sentinel.AddComponent<CombatDamageReceiver>();
            receiver.Configure(100f, 1.75f);

            GameObject hudRoot = new GameObject("CombatSandboxHUD");
            hudRoot.transform.SetParent(sandbox.transform, false);
            CombatSandboxHud hud = hudRoot.AddComponent<CombatSandboxHud>();
            hud.Configure(receiver, combat);

            GateHud gateHud = UnityEngine.Object.FindAnyObjectByType<GateHud>();
            if (gateHud != null) gateHud.enabled = false;

            CreateArenaMarkers(sandbox.transform);

            EditorSceneManager.SaveScene(scene, TargetScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = sentinel;
            Debug.Log($"MUSCA_COMBAT_SANDBOX_BUILT scene={TargetScene}");
        }

        private static void CreateArenaMarkers(Transform parent)
        {
            Material cyan = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/CyanSoft.mat");
            Material amber = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/AccentOrange.mat");
            CreateMarker("CombatBoundary_Left", parent, new Vector3(-2.6f, 0.03f, 4.6f), new Vector3(0.05f, 0.04f, 5.2f), cyan);
            CreateMarker("CombatBoundary_Right", parent, new Vector3(2.6f, 0.03f, 4.6f), new Vector3(0.05f, 0.04f, 5.2f), cyan);
            CreateMarker("CombatFocus", parent, new Vector3(0f, 0.035f, 4.6f), new Vector3(1.1f, 0.045f, 1.1f), amber);
        }

        private static void CreateMarker(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = scale;
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            throw new InvalidOperationException($"Missing root: {name}");
        }

        private static void SetColliders(GameObject root, bool enabled)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = enabled;
            }
        }

        private static GameObject InstantiateModel(string path, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException($"Model could not be loaded: {path}");
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

        private static void Add(Dictionary<string, Material> map, string source, string target)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{target}.mat");
            if (material == null) throw new InvalidOperationException($"Missing material: {target}");
            map[source] = material;
        }

        private static void RemapMaterials(GameObject root, Dictionary<string, Material> map)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source != null && map.TryGetValue(source.name, out Material replacement))
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
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 position = visual.transform.position;
            position.y += floorY - bounds.min.y;
            visual.transform.position = position;
        }
    }
}
