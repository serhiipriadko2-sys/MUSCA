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
            public bool sentinelHealth;
            public bool sentinelBrain;
            public bool kaelPrediction;
            public bool telegraphPresent;
            public bool sentinelCollider;
            public bool sentinelColliderWorldSizeValid;
            public bool sentinelOverlapsPlayer;
            public Vector3 sentinelColliderWorldSize;
            public bool companionPresent;
            public bool formEnvironmentPresent;
            public bool collisionFloorEnabled;
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

            Transform sentinelTransform = sandbox != null ? sandbox.transform.Find("Sentinel_v01") : null;
            CombatDamageReceiver health = sentinelTransform != null ? sentinelTransform.GetComponent<CombatDamageReceiver>() : null;
            SentinelCombatBrain brain = sentinelTransform != null ? sentinelTransform.GetComponent<SentinelCombatBrain>() : null;
            KaelPredictionProbe prediction = sentinelTransform != null ? sentinelTransform.GetComponent<KaelPredictionProbe>() : null;
            Transform telegraph = sandbox != null ? sandbox.transform.Find("SentinelTelegraph") : null;
            CapsuleCollider collider = sentinelTransform != null ? sentinelTransform.GetComponent<CapsuleCollider>() : null;
            int rendererCount = sentinelTransform != null ? sentinelTransform.GetComponentsInChildren<Renderer>(true).Length : 0;
            if (sentinelTransform == null) findings.Add("sentinel missing");
            if (health == null) findings.Add("sentinel health missing");
            if (brain == null) findings.Add("sentinel combat brain missing");
            if (prediction == null) findings.Add("Kael prediction probe missing");
            if (telegraph == null) findings.Add("sentinel telegraph marker missing");
            if (collider == null) findings.Add("sentinel collider missing");
            if (rendererCount == 0) findings.Add("sentinel renderers missing");

            Vector3 sentinelColliderWorldSize = collider != null ? collider.bounds.size : Vector3.zero;
            bool sentinelColliderWorldSizeValid = collider != null &&
                sentinelColliderWorldSize.x >= 0.65f && sentinelColliderWorldSize.x <= 1.05f &&
                sentinelColliderWorldSize.y >= 1.55f && sentinelColliderWorldSize.y <= 1.95f &&
                sentinelColliderWorldSize.z >= 0.65f && sentinelColliderWorldSize.z <= 1.05f;
            if (!sentinelColliderWorldSizeValid)
                findings.Add($"sentinel collider world size invalid: {sentinelColliderWorldSize}");

            bool sentinelOverlapsPlayer = collider != null && playerCharacter != null &&
                collider.bounds.Intersects(playerCharacter.bounds);
            if (sentinelOverlapsPlayer) findings.Add("sentinel collider overlaps player at authored spawn");

            if (companion == null || companion.GetComponent<MuscaCompanion>() == null)
                findings.Add("MUSCA companion missing");
            if (formEnvironment == null) findings.Add("Form v0.3 environment missing");

            Transform floor = functionEnvironment != null ? functionEnvironment.transform.Find("Floor") : null;
            BoxCollider floorCollider = floor != null ? floor.GetComponent<BoxCollider>() : null;
            bool collisionFloorEnabled = floorCollider != null && floorCollider.enabled;
            if (!collisionFloorEnabled) findings.Add("authoritative Function floor collider missing or disabled");

            GateHud gateHud = UnityEngine.Object.FindAnyObjectByType<GateHud>(FindObjectsInactive.Include);
            bool gateHudDisabled = gateHud != null && !gateHud.enabled;
            if (!gateHudDisabled) findings.Add("Gate HUD should be disabled in combat sandbox");

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                }
            }
            if (missingScripts != 0) findings.Add($"missing scripts: {missingScripts}");

            bool buildSettingsPreserved = EditorBuildSettings.scenes.Length == 1 &&
                EditorBuildSettings.scenes[0].enabled &&
                EditorBuildSettings.scenes[0].path == "Assets/MUSCA/Scenes/GateLab_FormV03_Playable.unity";
            if (!buildSettingsPreserved) findings.Add("project build settings drifted from the existing Form v0.3 scene");

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
                sentinelHealth = health != null,
                sentinelBrain = brain != null,
                kaelPrediction = prediction != null,
                telegraphPresent = telegraph != null,
                sentinelCollider = collider != null,
                sentinelColliderWorldSizeValid = sentinelColliderWorldSizeValid,
                sentinelOverlapsPlayer = sentinelOverlapsPlayer,
                sentinelColliderWorldSize = sentinelColliderWorldSize,
                companionPresent = companion != null && companion.GetComponent<MuscaCompanion>() != null,
                formEnvironmentPresent = formEnvironment != null,
                collisionFloorEnabled = collisionFloorEnabled,
                gateHudDisabled = gateHudDisabled,
                buildSettingsPreserved = buildSettingsPreserved,
                findings = findings.ToArray()
            };

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string output = Path.Combine(projectRoot, "Docs", "AI", "CombatSandboxValidation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
            File.WriteAllText(output, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_COMBAT_SANDBOX_VALIDATION status={receipt.status} receipt={output}");
            if (findings.Count != 0) throw new InvalidOperationException(string.Join("; ", findings));
        }

        private static GameObject FindRoot(Scene scene, string name, List<string> findings)
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
