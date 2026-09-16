using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MUSCA.Gate3D.Editor
{
    public static class GateSceneBuilder
    {
        private const string ScenePath = "Assets/MUSCA/Scenes/GateLab.unity";
        private const string MaterialRoot = "Assets/MUSCA/Materials";

        public static void BuildScene()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GateLab";

            Material shell = MaterialAsset("Mat_Shell", new Color32(18, 30, 38, 255), Color.black, 0.55f, 0.32f);
            Material floor = MaterialAsset("Mat_Floor", new Color32(28, 42, 50, 255), Color.black, 0.35f, 0.18f);
            Material gate = MaterialAsset("Mat_Gate", new Color32(38, 56, 68, 255), new Color(0.02f, 0.12f, 0.17f), 0.62f, 0.45f);
            Material cyan = MaterialAsset("Mat_Cyan", new Color32(20, 104, 135, 255), new Color(0.08f, 0.9f, 1.4f), 0.2f, 0.2f);
            Material amber = MaterialAsset("Mat_Amber", new Color32(255, 139, 22, 255), new Color(1.6f, 0.48f, 0.02f), 0.08f, 0.3f);
            Material cobalt = MaterialAsset("Mat_Cobalt", new Color32(34, 135, 255, 255), new Color(0.12f, 0.5f, 1.8f), 0.12f, 0.35f);
            Material companion = MaterialAsset("Mat_Companion", new Color32(20, 42, 52, 255), new Color(0.04f, 0.65f, 0.85f), 0.6f, 0.35f);

            Transform environment = new GameObject("Environment").transform;
            BuildShell(environment, shell, floor, gate, cyan);

            ReagentStation amberStation = BuildStation(environment, Reagent.Amber, new Vector3(-4.1f, 0f, -7.2f), amber, new Color(1f, 0.42f, 0.02f));
            ReagentStation cobaltStation = BuildStation(environment, Reagent.Cobalt, new Vector3(4.1f, 0f, -7.2f), cobalt, new Color(0.12f, 0.5f, 1f));

            Transform gateRoot = environment.Find("Gate");
            Transform leftPanel = gateRoot.Find("LeftPanel");
            Transform rightPanel = gateRoot.Find("RightPanel");
            Transform gateAnchor = gateRoot.Find("DiscoveryAnchor");
            Light gateLight = gateRoot.Find("ResponseLight").GetComponent<Light>();

            FirstPersonController player = BuildPlayer();
            GateRuntime runtime = new GameObject("GateRuntime").AddComponent<GateRuntime>();
            runtime.Configure(player, gateAnchor, leftPanel, rightPanel, gateLight, amberStation, cobaltStation);

            GateHud hud = new GameObject("GateHUD").AddComponent<GateHud>();
            hud.Configure(runtime);
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            BuildCompanion(playerCamera != null ? playerCamera.transform : player.transform, companion, cyan);

            RuntimeQaCapture capture = new GameObject("RuntimeQaCapture").AddComponent<RuntimeQaCapture>();
            capture.Configure(player, runtime);

            BuildLighting(environment);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.27f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.04f, 0.08f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.01f, 0.02f, 0.025f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.06f, 0.085f);
            RenderSettings.fogDensity = 0.018f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MUSCA_SCENE_BUILT path={ScenePath}");
        }

        private static void BuildShell(Transform parent, Material shell, Material floor, Material gate, Material cyan)
        {
            CreateCube("Floor", parent, new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 30f), floor, true);
            CreateCube("LeftWall", parent, new Vector3(-7.75f, 2.6f, 0f), new Vector3(0.5f, 5.2f, 30f), shell, true);
            CreateCube("RightWall", parent, new Vector3(7.75f, 2.6f, 0f), new Vector3(0.5f, 5.2f, 30f), shell, true);
            CreateCube("Ceiling", parent, new Vector3(0f, 5.05f, 0f), new Vector3(16f, 0.3f, 30f), shell, true);

            for (float z = 12f; z >= -12f; z -= 3f)
            {
                CreateCube($"FloorJoint_{z:0}", parent, new Vector3(0f, 0.015f, z), new Vector3(15f, 0.03f, 0.05f), shell, false);
                CreateCube($"CeilingBeam_{z:0}", parent, new Vector3(0f, 4.72f, z), new Vector3(15.2f, 0.22f, 0.34f), shell, false);
            }

            for (float z = 11f; z >= -10f; z -= 2.6f)
            {
                CreateCube($"LightStripL_{z:0.0}", parent, new Vector3(-7.45f, 2.5f, z), new Vector3(0.05f, 1.4f, 0.12f), cyan, false);
                CreateCube($"LightStripR_{z:0.0}", parent, new Vector3(7.45f, 2.5f, z), new Vector3(0.05f, 1.4f, 0.12f), cyan, false);
            }

            for (float z = 9.5f; z >= -9f; z -= 1.45f)
            {
                CreateCube($"Route_{z:0.0}", parent, new Vector3(0f, 0.035f, z), new Vector3(0.75f, 0.035f, 0.34f), cyan, false);
            }

            BuildGate(parent, shell, gate, cyan);
            BuildDestinationCorridor(parent, shell, floor, cyan);
        }

        private static void BuildGate(Transform parent, Material shell, Material gate, Material cyan)
        {
            Transform root = new GameObject("Gate").transform;
            root.SetParent(parent, false);

            CreateCube("BackLeft", root, new Vector3(-5.35f, 2.6f, -13.55f), new Vector3(5.3f, 5.2f, 0.5f), shell, true);
            CreateCube("BackRight", root, new Vector3(5.35f, 2.6f, -13.55f), new Vector3(5.3f, 5.2f, 0.5f), shell, true);
            CreateCube("Header", root, new Vector3(0f, 4.38f, -13.55f), new Vector3(5.4f, 1.25f, 0.7f), gate, true);
            CreateCube("FrameLeft", root, new Vector3(-2.72f, 2f, -13.52f), new Vector3(0.72f, 4f, 0.75f), gate, true);
            CreateCube("FrameRight", root, new Vector3(2.72f, 2f, -13.52f), new Vector3(0.72f, 4f, 0.75f), gate, true);
            CreateCube("LeftPanel", root, new Vector3(-1.13f, 1.76f, -13.25f), new Vector3(2.25f, 3.5f, 0.34f), gate, true);
            CreateCube("RightPanel", root, new Vector3(1.13f, 1.76f, -13.25f), new Vector3(2.25f, 3.5f, 0.34f), gate, true);
            CreateCube("GateGlow", root, new Vector3(0f, 3.72f, -13.02f), new Vector3(0.16f, 1.2f, 0.12f), cyan, false);

            GameObject anchor = new GameObject("DiscoveryAnchor");
            anchor.transform.SetParent(root, false);
            anchor.transform.localPosition = new Vector3(0f, 1.68f, -11.6f);

            GameObject lightObject = new GameObject("ResponseLight");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 3.3f, -11.2f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 16f;
            light.intensity = 7f;
            light.color = new Color(0.25f, 0.85f, 1f);

            CreateWorldText("GateLabel", root, "ШЛЮЗ A-1", new Vector3(0f, 4.32f, -13.01f), 0.04f, new Color(0.83f, 0.96f, 1f));
        }

        private static void BuildDestinationCorridor(Transform parent, Material shell, Material floor, Material cyan)
        {
            Transform corridor = new GameObject("DestinationCorridor").transform;
            corridor.SetParent(parent, false);
            CreateCube("CorridorFloor", corridor, new Vector3(0f, -0.1f, -16f), new Vector3(4.5f, 0.2f, 5.3f), floor, true);
            CreateCube("CorridorLeft", corridor, new Vector3(-2.35f, 2.2f, -16f), new Vector3(0.3f, 4.4f, 5.3f), shell, true);
            CreateCube("CorridorRight", corridor, new Vector3(2.35f, 2.2f, -16f), new Vector3(0.3f, 4.4f, 5.3f), shell, true);
            CreateCube("CorridorCeiling", corridor, new Vector3(0f, 4.35f, -16f), new Vector3(4.7f, 0.25f, 5.3f), shell, false);
            CreateCube("FarWall", corridor, new Vector3(0f, 2.2f, -18.65f), new Vector3(4.7f, 4.4f, 0.25f), shell, true);
            CreateCube("DestinationLight", corridor, new Vector3(0f, 3.65f, -18.3f), new Vector3(2.5f, 0.1f, 0.08f), cyan, false);
            CreateWorldText("SectorBLabel", corridor, "СЕКТОР B", new Vector3(0f, 2.4f, -18.5f), 0.04f, new Color(0.3f, 0.9f, 1f));
        }

        private static ReagentStation BuildStation(Transform parent, Reagent reagent, Vector3 position, Material glow, Color lightColor)
        {
            Transform root = new GameObject(reagent == Reagent.Amber ? "AmberStation" : "CobaltStation").transform;
            root.SetParent(parent, false);
            root.position = position;

            Material baseMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/Mat_Gate.mat");
            CreateCube("Base", root, new Vector3(0f, 0.4f, 0f), new Vector3(2.15f, 0.8f, 1.55f), baseMaterial, true);

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "ReagentColumn";
            cylinder.transform.SetParent(root, false);
            cylinder.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            cylinder.transform.localScale = new Vector3(0.96f, 1.15f, 0.96f);
            cylinder.GetComponent<Renderer>().sharedMaterial = glow;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());

            GameObject lightObject = new GameObject("StationLight");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.4f, 0.2f);
            Light stationLight = lightObject.AddComponent<Light>();
            stationLight.type = LightType.Point;
            stationLight.range = 8f;
            stationLight.intensity = 3f;
            stationLight.color = lightColor;

            string code = reagent == Reagent.Amber ? "AMBER · 9" : "COBALT · 4";
            Color textColor = reagent == Reagent.Amber ? new Color(1f, 0.68f, 0.23f) : new Color(0.32f, 0.72f, 1f);
            CreateWorldText("StationLabel", root, code, new Vector3(0f, 3.15f, 0.25f), 0.028f, textColor);

            ReagentStation station = root.gameObject.AddComponent<ReagentStation>();
            Color idle = lightColor * 0.65f;
            Color active = lightColor * 2.2f;
            station.Configure(reagent, cylinder.GetComponent<Renderer>(), stationLight, idle, active);
            return station;
        }

        private static FirstPersonController BuildPlayer()
        {
            GameObject root = new GameObject("Player");
            root.transform.SetPositionAndRotation(new Vector3(0f, 0f, 12f), Quaternion.Euler(0f, 180f, 0f));
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.68f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.025f, 0.035f);
            cameraObject.AddComponent<AudioListener>();

            return root.AddComponent<FirstPersonController>();
        }

        private static void BuildCompanion(Transform target, Material bodyMaterial, Material wingMaterial)
        {
            Transform root = new GameObject("MUSCA Companion").transform;
            root.position = new Vector3(-1.1f, 1.45f, 10.8f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root, false);
            body.transform.localScale = Vector3.one * 0.44f;
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(root, false);
            eye.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            eye.transform.localScale = Vector3.one * 0.16f;
            eye.GetComponent<Renderer>().sharedMaterial = wingMaterial;
            UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());

            Transform leftWing = CreateCube("LeftWing", root, new Vector3(-0.28f, 0.03f, 0.03f), new Vector3(0.48f, 0.025f, 0.22f), wingMaterial, false).transform;
            Transform rightWing = CreateCube("RightWing", root, new Vector3(0.28f, 0.03f, 0.03f), new Vector3(0.48f, 0.025f, 0.22f), wingMaterial, false).transform;

            GameObject lightObject = new GameObject("CompanionLight");
            lightObject.transform.SetParent(root, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 2.2f;
            light.color = new Color(0.25f, 0.9f, 1f);

            MuscaCompanion companion = root.gameObject.AddComponent<MuscaCompanion>();
            companion.Configure(target, leftWing, rightWing);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject sun = new GameObject("DirectionalLight");
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = new Color(0.78f, 0.9f, 1f);
            light.shadows = LightShadows.Soft;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static void CreateWorldText(string name, Transform parent, string text, Vector3 position, float characterSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(-1f, 1f, 1f);
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 48;
            mesh.characterSize = characterSize;
            mesh.color = color;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                mesh.font = font;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }
        }

        private static Material MaterialAsset(string name, Color color, Color emission, float metallic, float smoothness)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Built-in Standard shader was not found.");
                }
                material = new Material(shader) { name = name };
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

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MUSCA", "Scenes"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MUSCA", "Materials"));
            AssetDatabase.Refresh();
        }

        public static void BuildWindowsPlayer()
        {
            BuildScene();
            string output = ArgValue("--musca-build-output=");
            if (string.IsNullOrEmpty(output))
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
                output = Path.Combine(projectRoot, "Builds", "Windows", "MUSCA-Gate3D.exe");
            }
            output = Path.GetFullPath(output.Trim('"'));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;
            Debug.Log($"MUSCA_BUILD result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} bytes={summary.totalSize} path={output}");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"MUSCA Windows build failed: {summary.result}");
            }
        }

        private static string ArgValue(string prefix)
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return argument.Substring(prefix.Length);
                }
            }
            return string.Empty;
        }
    }
}
