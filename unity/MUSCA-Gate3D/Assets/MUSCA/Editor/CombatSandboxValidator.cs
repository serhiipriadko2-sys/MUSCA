using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MUSCA.Gate3D.Editor
{
    public static class CombatSandboxValidator
    {
        private const string ScenePath = "Assets/MUSCA/Scenes/CombatSandbox_v01.unity";

        [Serializable]
        private sealed class Receipt
        {
            public string status;
            public string unityVersion;
            public string scene;
            public int missingScripts;
            public int sentinelRenderers;
            public bool playerCombat;
            public bool playerVitals;
            public bool playerDodge;
            public bool playerLockOn;
            public bool jumpBindingCorrect;
            public bool dodgeBindingCorrect;
            public bool lockBindingCorrect;
            public bool lockMiddleMouseEnabled;
            public bool playerProxyRigPresent;
            public bool sentinelHealth;
            public bool sentinelBrain;
            public bool sentinelPresentation;
            public bool kaelPrediction;
            public bool crownPresentation;
            public bool telegraphPresent;
            public bool predictionTelegraphPresent;
            public bool sentinelProxyRigPresent;
            public bool sentinelCollider;
            public bool sentinelColliderWorldSizeValid;
            public bool sentinelOverlapsPlayer;
            public Vector3 sentinelColliderWorldSize;
            public bool sentinelVisualUpright;
            public Vector3 sentinelVisualWorldSize;
            public bool companionPresent;
            public bool arenaPresent;
            public bool collisionFloorEnabled;
            public bool legacyEnvironmentsHidden;
            public bool gateRuntimeDisabled;
            public bool gateHudDisabled;
            public bool buildSettingsPreserved;
            public string[] findings;
        }

        public static void Validate()
        {
            var findings = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = FindRoot(scene, "Player", findings);
            GameObject sandbox = FindRoot(scene, "CombatSandbox_v01", findings);
            GameObject companion = FindRoot(scene, "MUSCA Companion", findings);
            GameObject formEnvironment = FindRoot(scene, "FormV03_Environment", findings);
            GameObject functionEnvironment = FindRoot(scene, "Function_Environment", findings);

            FirstPersonController movement = player != null ? player.GetComponent<FirstPersonController>() : null;
            PlayerMeleeCombat combat = player != null ? player.GetComponent<PlayerMeleeCombat>() : null;
            PlayerCombatVitals vitals = player != null ? player.GetComponent<PlayerCombatVitals>() : null;
            PlayerDodgeController dodge = player != null ? player.GetComponent<PlayerDodgeController>() : null;
            PlayerLockOn lockOn = player != null ? player.GetComponent<PlayerLockOn>() : null;
            CharacterController playerCharacter = player != null ? player.GetComponent<CharacterController>() : null;
            if (combat == null) findings.Add("player melee combat missing");
            if (vitals == null) findings.Add("player combat vitals missing");
            if (dodge == null) findings.Add("player dodge missing");
            if (lockOn == null) findings.Add("player lock-on missing");
            if (playerCharacter == null) findings.Add("player CharacterController missing");

            bool jumpBindingCorrect = KeyBindingMatches(
                movement, "jumpKey", KeyCode.Space);
            bool dodgeBindingCorrect = KeyBindingMatches(
                dodge, "dodgeKey", KeyCode.LeftShift);
            bool lockBindingCorrect = KeyBindingMatches(
                lockOn, "keyboardToggle", KeyCode.Q);
            bool lockMiddleMouseEnabled = BoolBindingMatches(
                lockOn, "middleMouseToggle", true);
            if (!jumpBindingCorrect) findings.Add("jump binding is not SPACE");
            if (!dodgeBindingCorrect) findings.Add("dodge binding is not LEFT SHIFT");
            if (!lockBindingCorrect) findings.Add("lock-on keyboard binding is not Q");
            if (!lockMiddleMouseEnabled) findings.Add("lock-on middle mouse binding is disabled");

            Transform playerVisual = player != null
                ? FindDeep(player.transform, "FormV03_Researcher_Visual")
                : null;
            Transform playerRig = playerVisual != null
                ? FindDeep(playerVisual, "P04_ProxyRig")
                : null;
            bool playerProxyRigPresent =
                playerRig != null &&
                FindDeep(playerRig, "P04_Shoulder_L") != null &&
                FindDeep(playerRig, "P04_Shoulder_R") != null &&
                FindDeep(playerRig, "P04_Elbow_L") != null &&
                FindDeep(playerRig, "P04_Elbow_R") != null &&
                FindDeep(playerRig, "P04_Hip_L") != null &&
                FindDeep(playerRig, "P04_Hip_R") != null &&
                FindDeep(playerRig, "P04_Knee_L") != null &&
                FindDeep(playerRig, "P04_Knee_R") != null;
            if (!playerProxyRigPresent)
                findings.Add("player proxy pivot rig incomplete");

            Transform sentinelTransform = sandbox != null ? sandbox.transform.Find("Sentinel_v01") : null;
            Transform sentinelVisual = sentinelTransform != null ? sentinelTransform.Find("SentinelVisual") : null;
            CombatDamageReceiver health = sentinelTransform != null ? sentinelTransform.GetComponent<CombatDamageReceiver>() : null;
            SentinelCombatBrain brain = sentinelTransform != null ? sentinelTransform.GetComponent<SentinelCombatBrain>() : null;
            SentinelPresentation presentation = sentinelTransform != null ? sentinelTransform.GetComponent<SentinelPresentation>() : null;
            KaelPredictionProbe prediction = sentinelTransform != null ? sentinelTransform.GetComponent<KaelPredictionProbe>() : null;
            KaelCrownPresentation crownPresentation = sentinelTransform != null ?
                sentinelTransform.GetComponentInChildren<KaelCrownPresentation>(true) : null;
            Transform telegraph = sandbox != null
                ? sandbox.transform.Find("SentinelTelegraph")
                : null;
            Transform predictionTelegraph = sandbox != null
                ? sandbox.transform.Find("KaelPredictionTelegraph")
                : null;
            Transform sentinelRig = sentinelVisual != null
                ? FindDeep(sentinelVisual, "SV04_ProxyRig")
                : null;
            bool sentinelProxyRigPresent =
                sentinelRig != null &&
                FindDeep(sentinelRig, "SV04_Shoulder_L") != null &&
                FindDeep(sentinelRig, "SV04_Shoulder_R") != null &&
                FindDeep(sentinelRig, "SV04_Elbow_L") != null &&
                FindDeep(sentinelRig, "SV04_Elbow_R") != null &&
                FindDeep(sentinelRig, "SV04_Hip_L") != null &&
                FindDeep(sentinelRig, "SV04_Hip_R") != null &&
                FindDeep(sentinelRig, "SV04_Knee_L") != null &&
                FindDeep(sentinelRig, "SV04_Knee_R") != null;
            CapsuleCollider collider = sentinelTransform != null ? sentinelTransform.GetComponent<CapsuleCollider>() : null;
            int rendererCount = sentinelTransform != null ?
                sentinelTransform.GetComponentsInChildren<Renderer>(true).Length : 0;

            if (sentinelTransform == null) findings.Add("sentinel logic root missing");
            if (sentinelVisual == null) findings.Add("sentinel visual child missing");
            if (health == null) findings.Add("sentinel health missing");
            if (brain == null) findings.Add("sentinel combat brain missing");
            if (presentation == null) findings.Add("sentinel presentation missing");
            if (prediction == null) findings.Add("Kael prediction probe missing");
            if (crownPresentation == null) findings.Add("Kael crown presentation missing");
            if (telegraph == null) findings.Add("sentinel telegraph marker missing");
            if (predictionTelegraph == null)
                findings.Add("Kael prediction telegraph marker missing");
            if (!sentinelProxyRigPresent)
                findings.Add("sentinel proxy pivot rig incomplete");
            if (collider == null) findings.Add("sentinel collider missing");
            if (rendererCount == 0) findings.Add("sentinel renderers missing");

            Vector3 sentinelColliderWorldSize = collider != null ? collider.bounds.size : Vector3.zero;
            bool sentinelColliderWorldSizeValid = collider != null &&
                sentinelColliderWorldSize.x >= 1.00f && sentinelColliderWorldSize.x <= 1.30f &&
                sentinelColliderWorldSize.y >= 2.50f && sentinelColliderWorldSize.y <= 2.82f &&
                sentinelColliderWorldSize.z >= 1.00f && sentinelColliderWorldSize.z <= 1.30f;
            if (!sentinelColliderWorldSizeValid)
                findings.Add($"sentinel collider world size invalid: {sentinelColliderWorldSize}");

            bool sentinelOverlapsPlayer = collider != null && playerCharacter != null &&
                collider.bounds.Intersects(playerCharacter.bounds);
            if (sentinelOverlapsPlayer)
                findings.Add("sentinel collider overlaps player at authored spawn");

            Vector3 sentinelVisualWorldSize = ComputeRendererSize(sentinelVisual);
            bool sentinelVisualUpright =
                sentinelVisualWorldSize.y >= 2.45f &&
                sentinelVisualWorldSize.y > sentinelVisualWorldSize.x * 1.45f &&
                sentinelVisualWorldSize.y > sentinelVisualWorldSize.z * 1.45f;
            if (!sentinelVisualUpright)
                findings.Add($"sentinel visual is not upright: {sentinelVisualWorldSize}");

            if (companion == null || companion.GetComponent<MuscaCompanion>() == null)
                findings.Add("MUSCA companion missing");

            Transform arena = sandbox != null ? sandbox.transform.Find("FirstThresholdArena_v03") : null;
            bool arenaPresent = arena != null;
            if (!arenaPresent) findings.Add("First Threshold arena missing");

            Transform arenaFloor = arena != null ? arena.Find("ArenaFloor") : null;
            BoxCollider floorCollider = arenaFloor != null ? arenaFloor.GetComponent<BoxCollider>() : null;
            bool collisionFloorEnabled =
                floorCollider != null && floorCollider.enabled && arenaFloor.gameObject.activeInHierarchy;
            if (!collisionFloorEnabled)
                findings.Add("First Threshold arena floor collider missing or disabled");

            bool legacyEnvironmentsHidden =
                formEnvironment != null && !formEnvironment.activeSelf &&
                functionEnvironment != null && !functionEnvironment.activeSelf;
            if (!legacyEnvironmentsHidden)
                findings.Add("legacy GateLab environments should be hidden in combat scene");

            GateRuntime gateRuntime = UnityEngine.Object.FindAnyObjectByType<GateRuntime>(
                FindObjectsInactive.Include);
            bool gateRuntimeDisabled = gateRuntime != null && !gateRuntime.enabled;
            if (!gateRuntimeDisabled)
                findings.Add("GateRuntime should be disabled in combat scene");

            GateHud gateHud = UnityEngine.Object.FindAnyObjectByType<GateHud>(
                FindObjectsInactive.Include);
            bool gateHudDisabled = gateHud != null && !gateHud.enabled;
            if (!gateHudDisabled)
                findings.Add("Gate HUD should be disabled in combat scene");

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts +=
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                }
            }
            if (missingScripts != 0) findings.Add($"missing scripts: {missingScripts}");

            bool buildSettingsPreserved = EditorBuildSettings.scenes.Length == 1 &&
                EditorBuildSettings.scenes[0].enabled &&
                EditorBuildSettings.scenes[0].path ==
                    "Assets/MUSCA/Scenes/GateLab_FormV03_Playable.unity";
            if (!buildSettingsPreserved)
                findings.Add("project build settings drifted from existing Form v0.3 scene");

            var receipt = new Receipt
            {
                status = findings.Count == 0 ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                scene = ScenePath,
                missingScripts = missingScripts,
                sentinelRenderers = rendererCount,
                playerCombat = combat != null,
                playerVitals = vitals != null,
                playerDodge = dodge != null,
                playerLockOn = lockOn != null,
                jumpBindingCorrect = jumpBindingCorrect,
                dodgeBindingCorrect = dodgeBindingCorrect,
                lockBindingCorrect = lockBindingCorrect,
                lockMiddleMouseEnabled = lockMiddleMouseEnabled,
                playerProxyRigPresent = playerProxyRigPresent,
                sentinelHealth = health != null,
                sentinelBrain = brain != null,
                sentinelPresentation = presentation != null,
                kaelPrediction = prediction != null,
                crownPresentation = crownPresentation != null,
                telegraphPresent = telegraph != null,
                predictionTelegraphPresent = predictionTelegraph != null,
                sentinelProxyRigPresent = sentinelProxyRigPresent,
                sentinelCollider = collider != null,
                sentinelColliderWorldSizeValid = sentinelColliderWorldSizeValid,
                sentinelOverlapsPlayer = sentinelOverlapsPlayer,
                sentinelColliderWorldSize = sentinelColliderWorldSize,
                sentinelVisualUpright = sentinelVisualUpright,
                sentinelVisualWorldSize = sentinelVisualWorldSize,
                companionPresent =
                    companion != null && companion.GetComponent<MuscaCompanion>() != null,
                arenaPresent = arenaPresent,
                collisionFloorEnabled = collisionFloorEnabled,
                legacyEnvironmentsHidden = legacyEnvironmentsHidden,
                gateRuntimeDisabled = gateRuntimeDisabled,
                gateHudDisabled = gateHudDisabled,
                buildSettingsPreserved = buildSettingsPreserved,
                findings = findings.ToArray()
            };

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string output = Path.Combine(
                projectRoot, "Docs", "AI", "CombatSandboxValidation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
            File.WriteAllText(output, JsonUtility.ToJson(receipt, true));
            Debug.Log(
                $"MUSCA_COMBAT_SANDBOX_VALIDATION status={receipt.status} receipt={output}");
            if (findings.Count != 0)
                throw new InvalidOperationException(string.Join("; ", findings));
        }

        private static bool KeyBindingMatches(
            UnityEngine.Object target, string fieldName, KeyCode expected)
        {
            if (target == null) return false;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            return property != null && property.intValue == (int)expected;
        }

        private static bool BoolBindingMatches(
            UnityEngine.Object target, string fieldName, bool expected)
        {
            if (target == null) return false;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            return property != null && property.boolValue == expected;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }

        private static Vector3 ComputeRendererSize(Transform root)
        {
            if (root == null) return Vector3.zero;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return Vector3.zero;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.size;
        }

        private static GameObject FindRoot(
            Scene scene, string name, List<string> findings)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            findings.Add($"root missing: {name}");
            return null;
        }
    }
}
