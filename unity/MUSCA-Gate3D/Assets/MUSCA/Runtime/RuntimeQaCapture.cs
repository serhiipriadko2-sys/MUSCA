using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Cinemachine;

namespace MUSCA.Gate3D
{
    public sealed class RuntimeQaCapture : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private GateRuntime runtime;

        [Serializable]
        private sealed class QaReceipt
        {
            public string status;
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
            public float playerHealth;
            public float playerStamina;
            public bool grounded;
            public bool collisionSafe;
            public bool groundProbeHit;
            public string groundProbeCollider;
            public float groundProbePointY;
            public float groundProbeDistance;
            public bool dodgeStarted;
            public float dodgeDistance;
            public float dodgeStaminaBefore;
            public float dodgeStaminaAfter;
            public bool jumpStarted;
            public float jumpHeight;
            public bool jumpLanded;
            public bool lockOnAcquired;
            public string lockOnTarget;
            public bool cinemachineBrainPresent;
            public bool cinemachineLockActive;
            public string cinemachineActiveCamera;
            public bool telegraphObserved;
            public string sentinelState;
            public int sentinelTelegraphs;
            public int sentinelAttacks;
            public bool sentinelLastAttackHit;
            public bool kaelPrototypeActive;
            public string kaelDirection;
            public int kaelSamples;
            public float kaelConfidence;
            public bool kaelLocked;
            public bool kaelPredictionApplied;
            public bool kaelPredictionFresh;
            public float kaelBaseStrikeX;
            public float kaelBaseStrikeZ;
            public float kaelPredictionStrikeX;
            public float kaelPredictionStrikeZ;
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
            bool dodgeStarted = false;
            float dodgeDistance = 0f;
            float dodgeStaminaBefore = 0f;
            float dodgeStaminaAfter = 0f;
            bool jumpStarted = false;
            float jumpHeightObserved = 0f;
            bool jumpLanded = false;
            bool lockOnAcquired = false;
            string lockOnTarget = string.Empty;
            bool cinemachineLockActive = false;
            string cinemachineActiveCamera = string.Empty;
            bool telegraphObserved = false;
            bool skipStandardWait = false;
            bool groundingWasGrounded = false;
            float groundingSettledY = 0f;

            CombatDamageReceiver combatTarget = FindAnyObjectByType<CombatDamageReceiver>();
            SentinelCombatBrain brain = FindAnyObjectByType<SentinelCombatBrain>();
            KaelPredictionProbe prediction = FindAnyObjectByType<KaelPredictionProbe>();
            PlayerCombatVitals vitals = player.GetComponent<PlayerCombatVitals>();
            PlayerDodgeController dodge = player.GetComponent<PlayerDodgeController>();
            PlayerLockOn lockOn = player.GetComponent<PlayerLockOn>();
            MuscaCinemachineController cinemachineController =
                player.GetComponent<MuscaCinemachineController>();
            CinemachineBrain cameraBrain = player.PlayerCamera != null
                ? player.PlayerCamera.GetComponent<CinemachineBrain>()
                : null;
            CharacterController character = player.GetComponent<CharacterController>();

            Vector3 authoredPlayerSpawn = FindMarkerPosition(
                "CombatPlayerSpawn", new Vector3(0f, 0f, 18f));
            Vector3 bossPosition = combatTarget != null
                ? combatTarget.transform.position
                : new Vector3(0f, 0f, 34f);
            Vector3 strikePlayerPosition = bossPosition + Vector3.back * 1.55f;
            Vector3 lockPlayerPosition = bossPosition + Vector3.back * 8f;

            if (brain != null && view != "telegraph" && view != "enemyai" && view != "kaelprediction")
            {
                brain.enabled = false;
            }

            if (IsCombatView(view))
            {
                GateHud combatHud = FindAnyObjectByType<GateHud>();
                if (combatHud != null) combatHud.enabled = false;
            }

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
                    ResetCombatPlayer(vitals, authoredPlayerSpawn, 0f);
                    player.InputEnabled = false;
                    break;
                case "combatstrike":
                    ResetCombatPlayer(vitals, strikePlayerPosition, 0f);
                    player.InputEnabled = false;
                    break;
                case "grounding":
                    ResetCombatPlayer(
                        vitals, authoredPlayerSpawn + Vector3.up * 0.75f, 0f);
                    player.InputEnabled = true;
                    FirstPersonController.LockCursor();
                    break;
                case "dodge":
                    ResetCombatPlayer(vitals, authoredPlayerSpawn, 0f);
                    player.InputEnabled = true;
                    FirstPersonController.LockCursor();
                    break;
                case "jump":
                    ResetCombatPlayer(vitals, authoredPlayerSpawn, 0f);
                    player.InputEnabled = true;
                    FirstPersonController.LockCursor();
                    break;
                case "lockon":
                    ResetCombatPlayer(vitals, lockPlayerPosition, 0f);
                    player.InputEnabled = false;
                    break;
                case "telegraph":
                case "enemyai":
                case "kaelprediction":
                    ResetCombatPlayer(vitals, strikePlayerPosition, 0f);
                    player.InputEnabled = false;
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
                skipStandardWait = true;
            }

            if (view == "grounding")
            {
                yield return new WaitForSeconds(1.0f);
                groundingWasGrounded = player.LastMoveGrounded ||
                    (character != null && character.isGrounded);
                groundingSettledY = player.transform.position.y;
                player.InputEnabled = false;
                FirstPersonController.UnlockCursor();
                skipStandardWait = true;
            }

            if (view == "dodge")
            {
                float groundedTimeout = Time.time + 0.6f;
                while (!player.IsGrounded && Time.time < groundedTimeout)
                {
                    yield return null;
                }
                Vector3 dodgeStart = player.transform.position;
                dodgeStaminaBefore = vitals != null ? vitals.CurrentStamina : -1f;
                dodgeStarted = dodge != null && dodge.TryDodge(player.transform.forward);
                yield return new WaitForSeconds(0.42f);
                dodgeDistance = PlanarDistance(dodgeStart, player.transform.position);
                dodgeStaminaAfter = vitals != null ? vitals.CurrentStamina : -1f;
                player.InputEnabled = false;
                FirstPersonController.UnlockCursor();
                skipStandardWait = true;
            }

            if (view == "jump")
            {
                float groundedTimeout = Time.time + 0.6f;
                while (!player.IsGrounded && Time.time < groundedTimeout)
                {
                    yield return null;
                }
                float jumpStartY = player.transform.position.y;
                float jumpPeakY = jumpStartY;
                jumpStarted = player.TryStartJump();
                float startedAt = Time.time;
                float timeoutAt = startedAt + 1.8f;
                while (Time.time < timeoutAt)
                {
                    jumpPeakY = Mathf.Max(jumpPeakY, player.transform.position.y);
                    if (jumpStarted &&
                        Time.time > startedAt + 0.25f &&
                        player.IsGrounded &&
                        Mathf.Abs(player.transform.position.y - jumpStartY) < 0.12f)
                    {
                        jumpLanded = true;
                        break;
                    }
                    yield return null;
                }
                jumpHeightObserved = jumpPeakY - jumpStartY;
                player.InputEnabled = false;
                FirstPersonController.UnlockCursor();
                skipStandardWait = true;
            }

            if (view == "lockon")
            {
                // Let CinemachineBrain settle the detached output camera before
                // evaluating screen-facing target acquisition.
                yield return new WaitForSeconds(0.15f);
                lockOnAcquired = lockOn != null && lockOn.TryLockNearest();
                lockOnTarget = lockOn != null ? lockOn.TargetName : string.Empty;
                yield return new WaitForSeconds(0.20f);
                cinemachineLockActive =
                    cinemachineController != null &&
                    cinemachineController.IsUsingLockCamera;
                cinemachineActiveCamera =
                    cameraBrain != null && cameraBrain.ActiveVirtualCamera != null
                        ? cameraBrain.ActiveVirtualCamera.Name
                        : string.Empty;
                skipStandardWait = true;
            }

            if (view == "telegraph")
            {
                float timeoutAt = Time.time + 2.5f;
                while (brain != null && brain.State != SentinelCombatState.Telegraph && Time.time < timeoutAt)
                {
                    yield return null;
                }
                telegraphObserved = brain != null &&
                    brain.State == SentinelCombatState.Telegraph &&
                    brain.TelegraphVisible;
                yield return new WaitForSeconds(0.08f);
                skipStandardWait = true;
            }

            if (view == "enemyai")
            {
                float timeoutAt = Time.time + 3.5f;
                while (brain != null && brain.AttackCount == 0 && Time.time < timeoutAt)
                {
                    if (brain.State == SentinelCombatState.Telegraph && brain.TelegraphVisible)
                    {
                        telegraphObserved = true;
                    }
                    yield return null;
                }
                yield return new WaitForSeconds(0.12f);
                skipStandardWait = true;
            }

            if (view == "kaelprediction")
            {
                if (prediction != null)
                {
                    prediction.SetPrototypeActive(true, true);
                    for (int i = 0; i < 5; i++)
                    {
                        prediction.RecordDodgeForQa(DodgeDirection.Right);
                    }
                }

                float timeoutAt = Time.time + 2.5f;
                while (brain != null && brain.State != SentinelCombatState.Telegraph && Time.time < timeoutAt)
                {
                    yield return null;
                }
                telegraphObserved = brain != null &&
                    brain.State == SentinelCombatState.Telegraph &&
                    brain.TelegraphVisible;
                yield return new WaitForSeconds(0.08f);
                skipStandardWait = true;
            }

            if (!skipStandardWait)
            {
                yield return new WaitForSeconds(1.0f);
            }

            string output = ArgValue("--musca-qa-output=");
            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(Application.persistentDataPath, $"musca-qa-{view}.png");
            }

            string fullOutput = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutput) ?? Application.persistentDataPath);
            ScreenCapture.CaptureScreenshot(fullOutput, 1);
            yield return new WaitForSeconds(0.4f);

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

            bool qaPass = EvaluateCombatQa(
                view,
                combatHits,
                collisionSafe,
                dodgeStarted,
                dodgeDistance,
                dodgeStaminaBefore,
                dodgeStaminaAfter,
                jumpStarted,
                jumpHeightObserved,
                jumpLanded,
                lockOnAcquired,
                lockOnTarget,
                cinemachineLockActive,
                telegraphObserved,
                brain,
                vitals,
                prediction);

            var receipt = new QaReceipt
            {
                status = qaPass ? "PASS" : "FAIL",
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
                playerHealth = vitals != null ? vitals.CurrentHealth : -1f,
                playerStamina = vitals != null ? vitals.CurrentStamina : -1f,
                grounded = grounded,
                collisionSafe = collisionSafe,
                groundProbeHit = groundProbeHit,
                groundProbeCollider = groundProbeCollider,
                groundProbePointY = groundProbePointY,
                groundProbeDistance = groundProbeDistance,
                dodgeStarted = dodgeStarted,
                dodgeDistance = dodgeDistance,
                dodgeStaminaBefore = dodgeStaminaBefore,
                dodgeStaminaAfter = dodgeStaminaAfter,
                jumpStarted = jumpStarted,
                jumpHeight = jumpHeightObserved,
                jumpLanded = jumpLanded,
                lockOnAcquired = lockOnAcquired,
                lockOnTarget = lockOnTarget,
                cinemachineBrainPresent = cameraBrain != null,
                cinemachineLockActive = cinemachineLockActive,
                cinemachineActiveCamera = cinemachineActiveCamera,
                telegraphObserved = telegraphObserved,
                sentinelState = brain != null ? brain.State.ToString() : string.Empty,
                sentinelTelegraphs = brain != null ? brain.TelegraphCount : 0,
                sentinelAttacks = brain != null ? brain.AttackCount : 0,
                sentinelLastAttackHit = brain != null && brain.LastAttackHit,
                kaelPrototypeActive = prediction != null && prediction.PrototypeActive,
                kaelDirection = prediction != null ? prediction.Snapshot.Direction.ToString() : string.Empty,
                kaelSamples = prediction != null ? prediction.Snapshot.SampleCount : 0,
                kaelConfidence = prediction != null ? prediction.Snapshot.Confidence : 0f,
                kaelLocked = prediction != null && prediction.Snapshot.Locked,
                kaelPredictionApplied = brain != null && brain.PredictionAppliedThisTelegraph,
                kaelPredictionFresh = prediction != null && prediction.PredictionFresh,
                kaelBaseStrikeX = brain != null ? brain.CurrentStrikePoint.x : 0f,
                kaelBaseStrikeZ = brain != null ? brain.CurrentStrikePoint.z : 0f,
                kaelPredictionStrikeX = brain != null
                    ? brain.CurrentPredictionStrikePoint.x : 0f,
                kaelPredictionStrikeZ = brain != null
                    ? brain.CurrentPredictionStrikePoint.z : 0f,
                screenshot = fullOutput
            };

            string jsonPath = Path.ChangeExtension(fullOutput, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(receipt, true));
            Debug.Log($"MUSCA_QA_CAPTURE status={receipt.status} view={view} screenshot={fullOutput} receipt={jsonPath}");
            yield return null;
            Application.Quit(qaPass ? 0 : 4);
        }

        private static void ResetCombatPlayer(PlayerCombatVitals vitals, Vector3 position, float yaw)
        {
            if (vitals != null)
            {
                vitals.ResetForQa(position, yaw);
            }
        }

        private static bool IsCombatView(string view)
        {
            return view == "combat" ||
                   view == "combatstrike" ||
                   view == "grounding" ||
                   view == "dodge" ||
                   view == "jump" ||
                   view == "lockon" ||
                   view == "telegraph" ||
                   view == "enemyai" ||
                   view == "kaelprediction";
        }

        private static bool EvaluateCombatQa(
            string view,
            int combatHits,
            bool collisionSafe,
            bool dodgeStarted,
            float dodgeDistance,
            float dodgeStaminaBefore,
            float dodgeStaminaAfter,
            bool jumpStarted,
            float jumpHeight,
            bool jumpLanded,
            bool lockOnAcquired,
            string lockOnTarget,
            bool cinemachineLockActive,
            bool telegraphObserved,
            SentinelCombatBrain brain,
            PlayerCombatVitals vitals,
            KaelPredictionProbe prediction)
        {
            switch (view)
            {
                case "combatstrike":
                    return combatHits == 1;
                case "grounding":
                    return collisionSafe;
                case "dodge":
                    return dodgeStarted &&
                           dodgeDistance >= 1.5f && dodgeDistance <= 3.2f &&
                           dodgeStaminaBefore - dodgeStaminaAfter >= 20f;
                case "jump":
                    return jumpStarted &&
                           jumpHeight >= 0.65f && jumpHeight <= 1.55f &&
                           jumpLanded;
                case "lockon":
                    return lockOnAcquired &&
                           lockOnTarget == "Sentinel_v01" &&
                           cinemachineLockActive;
                case "telegraph":
                    return telegraphObserved && brain != null && brain.TelegraphCount >= 1;
                case "enemyai":
                    return telegraphObserved &&
                           brain != null && brain.AttackCount >= 1 && brain.LastAttackHit &&
                           vitals != null && vitals.CurrentHealth < vitals.MaxHealth;
                case "kaelprediction":
                    if (!telegraphObserved || brain == null || prediction == null || vitals == null)
                    {
                        return false;
                    }
                    KaelPredictionSnapshot snapshot = prediction.Snapshot;
                    return prediction.PrototypeActive &&
                           snapshot.Locked &&
                           snapshot.Direction == DodgeDirection.Right &&
                           snapshot.SampleCount == 5 &&
                           snapshot.Confidence >= 0.99f &&
                           prediction.PredictionFresh &&
                           brain.PredictionAppliedThisTelegraph &&
                           PlanarDistance(
                               vitals.transform.position,
                               brain.CurrentStrikePoint) <= 0.25f &&
                           PlanarDistance(
                               brain.CurrentStrikePoint,
                               brain.CurrentPredictionStrikePoint) >= 1.0f;
                default:
                    return true;
            }
        }

        private static Vector3 FindMarkerPosition(string name, Vector3 fallback)
        {
            GameObject marker = GameObject.Find(name);
            return marker != null ? marker.transform.position : fallback;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
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
