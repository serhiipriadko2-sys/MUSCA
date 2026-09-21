using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class RuntimeQaCapture : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private GateRuntime runtime;

        [Serializable]
        private sealed class QaReceipt
        {
            public string view;
            public string outcome;
            public int cells;
            public int signal;
            public float x;
            public float y;
            public float z;
            public int combatHits;
            public float sentinelHealth;
            public bool sentinelAlive;
            public bool grounded;
            public bool collisionSafe;
            public bool groundProbeHit;
            public string groundProbeCollider;
            public float groundProbePointY;
            public float groundProbeDistance;
            public string screenshot;
        }

        public void Configure(FirstPersonController controller, GateRuntime gateRuntime)
        {
            player = controller;
            runtime = gateRuntime;
        }

        private IEnumerator Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                yield break;
            }

            string view = ArgValue("--musca-qa=");
            if (string.IsNullOrEmpty(view) || player == null || runtime == null)
            {
                yield break;
            }

            player.InputEnabled = false;
            FirstPersonController.UnlockCursor();
            int combatHits = 0;
            CombatDamageReceiver combatTarget = FindAnyObjectByType<CombatDamageReceiver>();
            CharacterController character = player.GetComponent<CharacterController>();
            bool groundingWasGrounded = false;
            float groundingSettledY = 0f;
            switch (view)
            {
                case "spawn":
                    player.Teleport(new Vector3(0f, 0f, 10f), 180f);
                    break;
                case "decision":
                    player.Teleport(new Vector3(0f, 0f, -1.8f), 180f);
                    break;
                case "gate":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    break;
                case "amber":
                    player.Teleport(new Vector3(-2.1f, 0f, -5.8f), 235f);
                    break;
                case "opened":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    runtime.PrepareQaOpenedState();
                    break;
                case "openedworld":
                    player.Teleport(new Vector3(0f, 0f, -5.5f), 180f);
                    runtime.PrepareQaOpenedState();
                    GateHud hud = FindAnyObjectByType<GateHud>();
                    if (hud != null) hud.enabled = false;
                    break;
                case "combat":
                    player.Teleport(new Vector3(0f, 0f, 8.2f), 180f);
                    GateHud combatHud = FindAnyObjectByType<GateHud>();
                    if (combatHud != null) combatHud.enabled = false;
                    break;
                case "combatstrike":
                    player.Teleport(new Vector3(0f, 0f, 6.4f), 180f);
                    GateHud combatStrikeHud = FindAnyObjectByType<GateHud>();
                    if (combatStrikeHud != null) combatStrikeHud.enabled = false;
                    break;
                case "grounding":
                    player.Teleport(new Vector3(0f, 0.75f, 8.2f), 180f);
                    GateHud groundingHud = FindAnyObjectByType<GateHud>();
                    if (groundingHud != null) groundingHud.enabled = false;
                    player.InputEnabled = true;
                    FirstPersonController.LockCursor();
                    break;
                default:
                    Debug.LogError($"Unknown MUSCA QA view: {view}");
                    Application.Quit(3);
                    yield break;
            }

            if (view == "combatstrike")
            {
                yield return new WaitForFixedUpdate();
                PlayerMeleeCombat combat = player.GetComponent<PlayerMeleeCombat>();
                combatHits = combat != null ? combat.TryAttack() : 0;
                yield return new WaitForSeconds(0.15f);
            }

            if (view == "grounding")
            {
                yield return new WaitForSeconds(1.0f);
                groundingWasGrounded = player.LastMoveGrounded ||
                    (character != null && character.isGrounded);
                groundingSettledY = player.transform.position.y;
                player.InputEnabled = false;
                FirstPersonController.UnlockCursor();
            }

            yield return new WaitForSeconds(1.0f);
            string output = ArgValue("--musca-qa-output=");
            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(Application.persistentDataPath, $"musca-qa-{view}.png");
            }

            string fullOutput = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutput) ?? Application.persistentDataPath);
            ScreenCapture.CaptureScreenshot(fullOutput, 1);
            yield return new WaitForSeconds(0.75f);

            GateSnapshot snapshot = runtime.Snapshot();
            Vector3 position = player.transform.position;
            bool grounded = view == "grounding"
                ? groundingWasGrounded
                : character != null && character.isGrounded;
            float collisionCheckY = view == "grounding" ? groundingSettledY : position.y;
            bool groundProbeHit = false;
            string groundProbeCollider = string.Empty;
            float groundProbePointY = 0f;
            float groundProbeDistance = 0f;
            if (view == "grounding")
            {
                RaycastHit groundHit;
                groundProbeHit = TryGroundProbe(player.transform, out groundHit);
                if (groundProbeHit)
                {
                    groundProbeCollider = TransformPath(groundHit.collider.transform);
                    groundProbePointY = groundHit.point.y;
                    groundProbeDistance = groundHit.distance;
                }
            }
            bool collisionSafe = view != "grounding" ||
                (collisionCheckY > -0.1f && groundProbeHit &&
                 groundProbePointY > -0.5f && groundProbePointY < 0.5f);
            var receipt = new QaReceipt
            {
                view = view,
                outcome = snapshot.Outcome.ToString(),
                cells = snapshot.Cells,
                signal = runtime.SignalStrength,
                x = position.x,
                y = position.y,
                z = position.z,
                combatHits = combatHits,
                sentinelHealth = combatTarget != null ? combatTarget.CurrentHealth : -1f,
                sentinelAlive = combatTarget != null && combatTarget.IsAlive,
                grounded = grounded,
                collisionSafe = collisionSafe,
                groundProbeHit = groundProbeHit,
                groundProbeCollider = groundProbeCollider,
                groundProbePointY = groundProbePointY,
                groundProbeDistance = groundProbeDistance,
                screenshot = fullOutput
            };
            string jsonPath = Path.ChangeExtension(fullOutput, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_QA_CAPTURE view={view} screenshot={fullOutput} receipt={jsonPath}");
            yield return null;
            Application.Quit(0);
        }

        private static bool TryGroundProbe(Transform owner, out RaycastHit nearest)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                new Vector3(owner.position.x, 3f, owner.position.z),
                Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null ||
                    hit.collider.transform == owner ||
                    hit.collider.transform.IsChildOf(owner))
                {
                    continue;
                }

                nearest = hit;
                return true;
            }

            nearest = default;
            return false;
        }

        private static string TransformPath(Transform value)
        {
            string path = value.name;
            Transform current = value.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private static string ArgValue(string prefix)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return arg.Substring(prefix.Length).Trim('"');
                }
            }
            return string.Empty;
        }
    }
}
