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
    public static class FormV03PreviewBuilder
    {
        private const string ScenePath = "Assets/MUSCA/Scenes/GateLab_FormV03_Preview.unity";
        private const string ArtRoot = "Assets/MUSCA/Art/FormV03";
        private const string MaterialRoot = "Assets/MUSCA/Art/FormV03/UnityMaterials";

        [MenuItem("MUSCA/Form V03/Build Preview")]
        public static void BuildPreview()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GateLab_FormV03_Preview";

            GameObject environment = InstantiateModel($"{ArtRoot}/GateLab_Form_v0.3.fbx", "FormV03_Environment");
            GameObject researcher = InstantiateModel($"{ArtRoot}/Researcher_FormProxy_v0.3.fbx", "FormV03_Researcher");
            GameObject musca = InstantiateModel($"{ArtRoot}/MUSCA_FormProxy_v0.3.fbx", "FormV03_MUSCA");

            foreach (GameObject root in new[] { environment, researcher, musca })
            {
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            }
            // Match the approved gameplay composition: MUSCA hovers on the researcher's screen-left side.
            musca.transform.position = new Vector3(1.05f, 0f, 0f);

            Dictionary<string, Material> materials = BuildMaterials();
            RemapMaterials(environment, materials);
            RemapMaterials(researcher, materials);
            RemapMaterials(musca, materials);

            Camera camera = CreateCamera(new Vector3(0f, 1.68f, 8.6f), new Vector3(0f, 1.42f, -7.8f));
            camera.fieldOfView = 58f;

            CreateDirectional(new Vector3(48f, -25f, 0f), 1.25f, new Color(0.78f, 0.86f, 0.90f));
            CreatePoint("GateFill", new Vector3(0f, 2.4f, -11.3f), 15f, 11f, new Color(0.10f, 0.62f, 1f));
            CreatePoint("AmberFill", new Vector3(-4.1f, 2.0f, -6.8f), 8f, 7f, new Color(1f, 0.28f, 0.03f));
            CreatePoint("CobaltFill", new Vector3(4.1f, 2.0f, -6.8f), 8f, 7f, new Color(0.05f, 0.30f, 1f));
            CreatePoint("PlayerRim", new Vector3(0f, 2.35f, 6.7f), 5.5f, 4.5f, new Color(0.32f, 0.72f, 1f));
            foreach (float z in new[] { 7f, 3f, -1f, -5f, -9f })
            {
                CreatePoint($"NeutralFill_{z:0}", new Vector3(0f, 3.75f, z), 1.8f, 7.5f, new Color(0.72f, 0.80f, 0.84f));
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.39f, 0.41f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.17f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.045f, 0.055f, 0.065f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.018f, 0.035f, 0.045f);
            RenderSettings.fogDensity = 0.006f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = researcher;
            SceneView.lastActiveSceneView?.FrameSelected();
            AssetDatabase.SaveAssets();
            Debug.Log($"MUSCA_FORM_V03_PREVIEW_BUILT scene={ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MUSCA", "Art", "FormV03", "UnityMaterials"));
            AssetDatabase.Refresh();
        }

        private static GameObject InstantiateModel(string path, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException($"Missing model: {path}");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            return instance;
        }
        private static Dictionary<string, Material> BuildMaterials()
        {
            var map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            map["M_DarkMetal"] = MaterialAsset("DarkMetal", new Color(0.045f,0.060f,0.070f), Color.black, 0.82f, 0.68f);
            map["V03_DarkMetal"] = map["M_DarkMetal"];
            map["M_Panel"] = MaterialAsset("Panel", new Color(0.13f,0.17f,0.19f), Color.black, 0.58f, 0.55f);
            map["V03_Panel"] = map["M_Panel"];
            map["M_Floor"] = MaterialAsset("Floor", new Color(0.075f,0.090f,0.100f), Color.black, 0.42f, 0.52f);
            map["M_Cyan"] = Emissive("Cyan", new Color(0.04f,0.33f,0.45f), new Color(0.05f,1.6f,2.5f));
            map["V03_Cyan"] = map["M_Cyan"];
            map["V03_CyanSoft"] = Emissive("CyanSoft", new Color(0.04f,0.18f,0.23f), new Color(0.02f,0.55f,0.85f));
            map["V03_WhiteLight"] = Emissive("WhiteLight", new Color(0.65f,0.70f,0.70f), new Color(1.4f,1.65f,1.7f));
            map["M_Amber"] = Emissive("Amber", new Color(0.34f,0.10f,0.01f), new Color(2.4f,0.50f,0.03f));
            map["V03_Amber"] = map["M_Amber"];
            map["M_Cobalt"] = Emissive("Cobalt", new Color(0.02f,0.08f,0.35f), new Color(0.08f,0.55f,2.8f));
            map["V03_Cobalt"] = map["M_Cobalt"];
            map["M_Plant"] = MaterialAsset("Plant", new Color(0.045f,0.22f,0.09f), Color.black, 0f, 0.25f);
            map["M_AccentOrange"] = Emissive("AccentOrange", new Color(0.25f,0.055f,0.006f), new Color(0.55f,0.09f,0.005f));
            map["V03_Armor"] = MaterialAsset("ResearcherArmor", new Color(0.035f,0.045f,0.050f), Color.black, 0.72f, 0.68f);
            map["V03_Suit"] = MaterialAsset("ResearcherSuit", new Color(0.47f,0.50f,0.50f), Color.black, 0.10f, 0.34f);
            map["V03_Skin"] = MaterialAsset("ResearcherSkin", new Color(0.46f,0.23f,0.16f), Color.black, 0f, 0.30f);
            map["V03_Hair"] = MaterialAsset("ResearcherHair", new Color(0.025f,0.014f,0.012f), Color.black, 0f, 0.15f);
            map["V03_Orange"] = Emissive("ResearcherOrange", new Color(0.38f,0.075f,0.008f), new Color(0.55f,0.08f,0.005f));
            map["V03_Ceramic"] = MaterialAsset("MuscaCeramic", new Color(0.62f,0.66f,0.66f), Color.black, 0.20f, 0.68f);
            map["V03_Glass"] = GlassAsset();
            return map;
        }

        private static void RemapMaterials(GameObject root, Dictionary<string, Material> map)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] != null && map.TryGetValue(source[i].name, out Material replacement))
                    {
                        source[i] = replacement;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = source;
            }
        }
        private static Material Emissive(string name, Color color, Color emission)
        {
            return MaterialAsset(name, color, emission, 0.15f, 0.52f);
        }

        private static Material MaterialAsset(string name, Color color, Color emission, float metallic, float smoothness)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Material GlassAsset()
        {
            string path = $"{MaterialRoot}/MuscaGlass.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = "MuscaGlass" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = new Color(0.10f, 0.40f, 0.52f, 0.42f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.SetFloat("_Metallic", 0.12f);
            material.SetFloat("_Glossiness", 0.82f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.02f,0.30f,0.55f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Camera CreateCamera(Vector3 position, Vector3 target)
        {
            GameObject go = new GameObject("FormV03_Camera");
            go.tag = "MainCamera";
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);
            Camera camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.014f, 0.020f);
            return camera;
        }

        private static void CreateDirectional(Vector3 euler, float intensity, Color color)
        {
            GameObject go = new GameObject("FormV03_Directional");
            go.transform.rotation = Quaternion.Euler(euler);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;
        }

        private static void CreatePoint(string name, Vector3 position, float intensity, float range, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            light.shadows = LightShadows.Soft;
        }
    }
}
