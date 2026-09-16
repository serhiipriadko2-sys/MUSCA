using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MUSCA.Gate3D.Editor
{
    public static class GateSceneValidator
    {
        private const string ScenePath = "Assets/MUSCA/Scenes/GateLab.unity";

        [Serializable]
        private sealed class Receipt
        {
            public string status;
            public string unityVersion;
            public string scene;
            public int rootObjects;
            public int missingScripts;
            public float cameraFov;
            public float playerEyeHeight;
            public float amberX;
            public float cobaltX;
            public float stationZ;
            public int buildSceneCount;
            public string[] findings;
        }

        public static void Validate()
        {
            var findings = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject environment = FindRoot(scene, "Environment", findings);
            GameObject player = FindRoot(scene, "Player", findings);
            FindRoot(scene, "GateRuntime", findings);
            FindRoot(scene, "GateHUD", findings);

            Camera camera = player != null ? player.GetComponentInChildren<Camera>() : null;
            CharacterController controller = player != null ? player.GetComponent<CharacterController>() : null;
            if (camera == null) findings.Add("player camera missing");
            if (controller == null) findings.Add("character controller missing");
            if (camera != null && Mathf.Abs(camera.fieldOfView - 72f) > 0.01f) findings.Add("camera FOV drift");
            if (camera != null && Mathf.Abs(camera.transform.localPosition.y - 1.68f) > 0.01f) findings.Add("eye height drift");

            Transform amber = environment != null ? environment.transform.Find("AmberStation") : null;
            Transform cobalt = environment != null ? environment.transform.Find("CobaltStation") : null;
            Transform gate = environment != null ? environment.transform.Find("Gate") : null;
            Transform corridor = environment != null ? environment.transform.Find("DestinationCorridor") : null;
            if (amber == null) findings.Add("amber station missing");
            if (cobalt == null) findings.Add("cobalt station missing");
            if (gate == null) findings.Add("gate missing");
            if (corridor == null) findings.Add("destination corridor missing");

            if (amber != null && cobalt != null)
            {
                if (Mathf.Abs(amber.position.x + cobalt.position.x) > 0.001f) findings.Add("station mirror symmetry drift");
                if (Mathf.Abs(amber.position.z - cobalt.position.z) > 0.001f) findings.Add("station z alignment drift");
            }

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }
            if (missingScripts != 0) findings.Add($"missing scripts: {missingScripts}");

            int buildSceneCount = EditorBuildSettings.scenes.Length;
            if (buildSceneCount != 1 || EditorBuildSettings.scenes[0].path != ScenePath || !EditorBuildSettings.scenes[0].enabled)
            {
                findings.Add("build settings do not contain exactly the enabled GateLab scene");
            }

            var receipt = new Receipt
            {
                status = findings.Count == 0 ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                scene = ScenePath,
                rootObjects = scene.rootCount,
                missingScripts = missingScripts,
                cameraFov = camera != null ? camera.fieldOfView : -1f,
                playerEyeHeight = camera != null ? camera.transform.localPosition.y : -1f,
                amberX = amber != null ? amber.position.x : float.NaN,
                cobaltX = cobalt != null ? cobalt.position.x : float.NaN,
                stationZ = amber != null ? amber.position.z : float.NaN,
                buildSceneCount = buildSceneCount,
                findings = findings.ToArray()
            };

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string output = Path.Combine(projectRoot, "Docs", "AI", "UnitySceneValidation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
            File.WriteAllText(output, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_SCENE_VALIDATION status={receipt.status} receipt={output}");
            if (findings.Count != 0)
            {
                throw new InvalidOperationException(string.Join("; ", findings));
            }
        }

        private static GameObject FindRoot(Scene scene, string name, List<string> findings)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }
            findings.Add($"root missing: {name}");
            return null;
        }
    }
}
