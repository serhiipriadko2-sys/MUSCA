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

        private static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 SentinelSpawn = new Vector3(0f, 0f, 34f);
        private const float BossVisualScale = 1.52f;

        [MenuItem("MUSCA/Combat/Build First Threshold v0.4")]
        public static void Build()
        {
            RequireAsset(SourceScene);
            RequireAsset(SentinelAsset);

            Scene scene = OpenOrCreateTargetScene();

            GameObject player = FindRoot(scene, "Player");
            GameObject functionEnvironment = FindRootOptional(scene, "Function_Environment");
            GameObject formEnvironment = FindRootOptional(scene, "FormV03_Environment");

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

            ApplyCombatInputBindings(movement, dodge, lockOn);

            GameObject sandbox = FindRootOptional(scene, "CombatSandbox_v01");
            if (sandbox == null) sandbox = new GameObject("CombatSandbox_v01");

            Dictionary<string, Material> materials = LoadMaterialMap();
            BuildResearcherProxyRig(player.transform);
            BuildFirstThresholdArena(sandbox.transform, materials);

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.020f, 0.035f);

            if (functionEnvironment != null) functionEnvironment.SetActive(false);
            if (formEnvironment != null) formEnvironment.SetActive(false);

            GateRuntime gateRuntime = UnityEngine.Object.FindAnyObjectByType<GateRuntime>(
                FindObjectsInactive.Include);
            if (gateRuntime != null) gateRuntime.enabled = false;

            GateHud gateHud = UnityEngine.Object.FindAnyObjectByType<GateHud>(
                FindObjectsInactive.Include);
            if (gateHud != null) gateHud.enabled = false;

            CharacterController playerCharacter = player.GetComponent<CharacterController>();
            if (playerCharacter != null) playerCharacter.enabled = false;
            player.transform.SetPositionAndRotation(PlayerSpawn, Quaternion.identity);
            if (playerCharacter != null) playerCharacter.enabled = true;

            GameObject sentinel = RebuildSentinel(sandbox.transform, materials);
            Transform sentinelVisual = sentinel.transform.Find("SentinelVisual");

            CapsuleCollider capsule = sentinel.AddComponent<CapsuleCollider>();
            ConfigureWorldCapsule(capsule, 2.66f, 0.56f, 1.33f);

            CombatDamageReceiver receiver = sentinel.AddComponent<CombatDamageReceiver>();
            receiver.Configure(130f, 2.15f);

            GameObject telegraph = EnsureTelegraphMarker(
                sandbox.transform, sentinel.transform.position);
            GameObject predictionTelegraph = EnsurePredictionTelegraphMarker(
                sandbox.transform, sentinel.transform.position);

            KaelPredictionProbe prediction = sentinel.AddComponent<KaelPredictionProbe>();
            prediction.Configure(dodge, vitals);
            prediction.SetPrototypeActive(false, true);

            SentinelCombatBrain brain = sentinel.AddComponent<SentinelCombatBrain>();
            brain.Configure(
                vitals,
                telegraph.transform,
                predictionTelegraph.transform,
                prediction);

            Transform spear = BuildKaelSpear(sentinel.transform, materials);
            Transform crown = BuildPredictionCrown(sentinel.transform, materials);

            SentinelPresentation presentation = sentinel.AddComponent<SentinelPresentation>();
            presentation.Configure(brain, sentinelVisual, spear);

            KaelCrownPresentation crownPresentation =
                crown.gameObject.AddComponent<KaelCrownPresentation>();
            crownPresentation.Configure(
                prediction,
                crown.Find("CrownRing_A"),
                crown.Find("CrownRing_B"),
                crown.Find("CrownRing_C"));

            GameObject hudRoot = EnsureChild(sandbox.transform, "CombatSandboxHUD");
            CombatSandboxHud hud = hudRoot.GetComponent<CombatSandboxHud>();
            if (hud == null) hud = hudRoot.AddComponent<CombatSandboxHud>();
            hud.Configure(receiver, combat, vitals, dodge, lockOn, brain, prediction);

            DestroyChildIfPresent(sandbox.transform, "CombatBoundary_Left");
            DestroyChildIfPresent(sandbox.transform, "CombatBoundary_Right");
            DestroyChildIfPresent(sandbox.transform, "CombatFocus");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TargetScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = sentinel;
            Debug.Log($"MUSCA_COMBAT_SANDBOX_BUILT scene={TargetScene}");
        }

        private static void ApplyCombatInputBindings(
            FirstPersonController movement,
            PlayerDodgeController dodge,
            PlayerLockOn lockOn)
        {
            SetKeyCode(movement, "jumpKey", KeyCode.Space);
            SetKeyCode(dodge, "dodgeKey", KeyCode.LeftShift);
            SetKeyCode(lockOn, "keyboardToggle", KeyCode.Q);

            SerializedObject lockSerialized = new SerializedObject(lockOn);
            SerializedProperty middleMouse = lockSerialized.FindProperty("middleMouseToggle");
            if (middleMouse == null)
            {
                throw new InvalidOperationException(
                    "PlayerLockOn.middleMouseToggle serialized field missing.");
            }
            middleMouse.boolValue = true;
            lockSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetKeyCode(
            UnityEngine.Object target, string fieldName, KeyCode keyCode)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{fieldName} serialized field missing.");
            }

            property.intValue = (int)keyCode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static void BuildResearcherProxyRig(Transform player)
        {
            Transform visual = FindDeepOptional(player, "FormV03_Researcher_Visual");
            if (visual == null)
            {
                throw new InvalidOperationException(
                    "Combat sandbox requires FormV03_Researcher_Visual.");
            }

            UnpackPrefabIfNeeded(visual.gameObject);

            string[] movableParts =
            {
                "P03_Torso", "P03_ChestArmor", "P03_Pelvis", "P03_Head",
                "P03_HairCap", "P03_Bun", "P03_UpperArm_-1", "P03_UpperArm_1",
                "P03_Shoulder_-1", "P03_Shoulder_1", "P03_Forearm_-1",
                "P03_Forearm_1", "P03_Thigh_-1", "P03_Thigh_1",
                "P03_Shin_-1", "P03_Shin_1", "P03_Boot_-1", "P03_Boot_1",
                "P03_Backpack", "P03_PackCore", "P03_Harness_-1",
                "P03_Harness_1", "P03_BeltAccent", "P03_WristDisplay",
                "P03_Detail_Collar", "P03_Detail_ChestLink",
                "P03_Detail_PackSide_L", "P03_Detail_PackSide_R",
                "P03_Detail_PackAmber", "P03_Detail_WristFrame",
                "P03_Detail_Knee_L", "P03_Detail_Knee_R",
                "P03_Detail_EarLink", "P03_Detail_HairTie"
            };
            RestorePartsToRoot(visual, movableParts);
            DestroyChildIfPresent(visual, "P04_ProxyRig");

            Transform torsoMesh = RequireDeep(visual, "P03_Torso");
            Transform pelvisMesh = RequireDeep(visual, "P03_Pelvis");
            Transform headMesh = RequireDeep(visual, "P03_Head");
            Transform leftUpperArm = RequireDeep(visual, "P03_UpperArm_-1");
            Transform rightUpperArm = RequireDeep(visual, "P03_UpperArm_1");
            Transform leftForearm = RequireDeep(visual, "P03_Forearm_-1");
            Transform rightForearm = RequireDeep(visual, "P03_Forearm_1");
            Transform leftThigh = RequireDeep(visual, "P03_Thigh_-1");
            Transform rightThigh = RequireDeep(visual, "P03_Thigh_1");
            Transform leftShin = RequireDeep(visual, "P03_Shin_-1");
            Transform rightShin = RequireDeep(visual, "P03_Shin_1");

            Transform rig = CreateRigPivot(
                visual, "P04_ProxyRig", visual.position, visual.rotation);
            Transform torso = CreateRigPivot(
                rig, "P04_TorsoPivot",
                JointAtY(torsoMesh, RendererBounds(pelvisMesh).max.y),
                visual.rotation);
            Transform head = CreateRigPivot(
                torso, "P04_HeadPivot",
                JointAtY(headMesh, RendererBounds(headMesh).min.y),
                visual.rotation);

            Transform shoulderLeft = CreateRigPivot(
                torso, "P04_Shoulder_L",
                JointAtY(leftUpperArm, RendererBounds(leftUpperArm).max.y),
                visual.rotation);
            Transform shoulderRight = CreateRigPivot(
                torso, "P04_Shoulder_R",
                JointAtY(rightUpperArm, RendererBounds(rightUpperArm).max.y),
                visual.rotation);
            Transform elbowLeft = CreateRigPivot(
                shoulderLeft, "P04_Elbow_L",
                JointBetween(leftUpperArm, leftForearm),
                visual.rotation);
            Transform elbowRight = CreateRigPivot(
                shoulderRight, "P04_Elbow_R",
                JointBetween(rightUpperArm, rightForearm),
                visual.rotation);

            Transform hipLeft = CreateRigPivot(
                rig, "P04_Hip_L",
                JointAtY(leftThigh, RendererBounds(leftThigh).max.y),
                visual.rotation);
            Transform hipRight = CreateRigPivot(
                rig, "P04_Hip_R",
                JointAtY(rightThigh, RendererBounds(rightThigh).max.y),
                visual.rotation);
            Transform kneeLeft = CreateRigPivot(
                hipLeft, "P04_Knee_L",
                JointBetween(leftThigh, leftShin),
                visual.rotation);
            Transform kneeRight = CreateRigPivot(
                hipRight, "P04_Knee_R",
                JointBetween(rightThigh, rightShin),
                visual.rotation);

            ParentParts(visual, torso,
                "P03_Torso", "P03_ChestArmor", "P03_Backpack", "P03_PackCore",
                "P03_Harness_-1", "P03_Harness_1", "P03_Detail_Collar",
                "P03_Detail_ChestLink", "P03_Detail_PackSide_L",
                "P03_Detail_PackSide_R", "P03_Detail_PackAmber");
            ParentParts(visual, head,
                "P03_Head", "P03_HairCap", "P03_Bun",
                "P03_Detail_EarLink", "P03_Detail_HairTie");
            ParentParts(visual, shoulderLeft,
                "P03_UpperArm_-1", "P03_Shoulder_-1");
            ParentParts(visual, shoulderRight,
                "P03_UpperArm_1", "P03_Shoulder_1");
            ParentParts(visual, elbowLeft,
                "P03_Forearm_-1", "P03_WristDisplay", "P03_Detail_WristFrame");
            ParentParts(visual, elbowRight, "P03_Forearm_1");
            ParentParts(visual, hipLeft, "P03_Thigh_-1");
            ParentParts(visual, hipRight, "P03_Thigh_1");
            ParentParts(visual, kneeLeft,
                "P03_Shin_-1", "P03_Boot_-1", "P03_Detail_Knee_L");
            ParentParts(visual, kneeRight,
                "P03_Shin_1", "P03_Boot_1", "P03_Detail_Knee_R");
        }

        private static void BuildSentinelProxyRig(Transform visual)
        {
            if (visual == null)
            {
                throw new InvalidOperationException("Sentinel visual missing.");
            }

            UnpackPrefabIfNeeded(visual.gameObject);
            string[] movableParts =
            {
                "SV01_Torso", "SV01_ChestPlate", "SV01_Pelvis", "SV01_Head",
                "SV01_OpticHousing", "SV01_Optic", "SV01_Core",
                "SV01_Shoulder_-1", "SV01_Shoulder_1",
                "SV01_UpperArm_-1", "SV01_UpperArm_1",
                "SV01_Forearm_-1", "SV01_Forearm_1",
                "SV01_Hand_-1", "SV01_Hand_1",
                "SV01_Thigh_-1", "SV01_Thigh_1",
                "SV01_Knee_-1", "SV01_Knee_1",
                "SV01_Shin_-1", "SV01_Shin_1",
                "SV01_Foot_-1", "SV01_Foot_1",
                "SV01_Telegraph_-1", "SV01_Telegraph_1"
            };
            RestorePartsToRoot(visual, movableParts);
            DestroyChildIfPresent(visual, "SV04_ProxyRig");

            Transform torsoMesh = RequireDeep(visual, "SV01_Torso");
            Transform pelvisMesh = RequireDeep(visual, "SV01_Pelvis");
            Transform headMesh = RequireDeep(visual, "SV01_Head");
            Transform leftUpperArm = RequireDeep(visual, "SV01_UpperArm_-1");
            Transform rightUpperArm = RequireDeep(visual, "SV01_UpperArm_1");
            Transform leftForearm = RequireDeep(visual, "SV01_Forearm_-1");
            Transform rightForearm = RequireDeep(visual, "SV01_Forearm_1");
            Transform leftThigh = RequireDeep(visual, "SV01_Thigh_-1");
            Transform rightThigh = RequireDeep(visual, "SV01_Thigh_1");
            Transform leftShin = RequireDeep(visual, "SV01_Shin_-1");
            Transform rightShin = RequireDeep(visual, "SV01_Shin_1");

            Transform rig = CreateRigPivot(
                visual, "SV04_ProxyRig", visual.position, visual.rotation);
            Transform torso = CreateRigPivot(
                rig, "SV04_TorsoPivot",
                JointAtY(torsoMesh, RendererBounds(pelvisMesh).max.y),
                visual.rotation);
            Transform head = CreateRigPivot(
                torso, "SV04_HeadPivot",
                JointAtY(headMesh, RendererBounds(headMesh).min.y),
                visual.rotation);
            Transform shoulderLeft = CreateRigPivot(
                torso, "SV04_Shoulder_L",
                JointAtY(leftUpperArm, RendererBounds(leftUpperArm).max.y),
                visual.rotation);
            Transform shoulderRight = CreateRigPivot(
                torso, "SV04_Shoulder_R",
                JointAtY(rightUpperArm, RendererBounds(rightUpperArm).max.y),
                visual.rotation);
            Transform elbowLeft = CreateRigPivot(
                shoulderLeft, "SV04_Elbow_L",
                JointBetween(leftUpperArm, leftForearm),
                visual.rotation);
            Transform elbowRight = CreateRigPivot(
                shoulderRight, "SV04_Elbow_R",
                JointBetween(rightUpperArm, rightForearm),
                visual.rotation);
            Transform hipLeft = CreateRigPivot(
                rig, "SV04_Hip_L",
                JointAtY(leftThigh, RendererBounds(leftThigh).max.y),
                visual.rotation);
            Transform hipRight = CreateRigPivot(
                rig, "SV04_Hip_R",
                JointAtY(rightThigh, RendererBounds(rightThigh).max.y),
                visual.rotation);
            Transform kneeLeft = CreateRigPivot(
                hipLeft, "SV04_Knee_L",
                JointBetween(leftThigh, leftShin),
                visual.rotation);
            Transform kneeRight = CreateRigPivot(
                hipRight, "SV04_Knee_R",
                JointBetween(rightThigh, rightShin),
                visual.rotation);

            ParentParts(visual, torso,
                "SV01_Torso", "SV01_ChestPlate", "SV01_Core");
            ParentParts(visual, head,
                "SV01_Head", "SV01_OpticHousing", "SV01_Optic");
            ParentParts(visual, shoulderLeft,
                "SV01_Shoulder_-1", "SV01_UpperArm_-1");
            ParentParts(visual, shoulderRight,
                "SV01_Shoulder_1", "SV01_UpperArm_1");
            ParentParts(visual, elbowLeft,
                "SV01_Forearm_-1", "SV01_Hand_-1", "SV01_Telegraph_-1");
            ParentParts(visual, elbowRight,
                "SV01_Forearm_1", "SV01_Hand_1", "SV01_Telegraph_1");
            ParentParts(visual, hipLeft, "SV01_Thigh_-1");
            ParentParts(visual, hipRight, "SV01_Thigh_1");
            ParentParts(visual, kneeLeft,
                "SV01_Knee_-1", "SV01_Shin_-1", "SV01_Foot_-1");
            ParentParts(visual, kneeRight,
                "SV01_Knee_1", "SV01_Shin_1", "SV01_Foot_1");
        }

        private static void UnpackPrefabIfNeeded(GameObject instance)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance);
            if (root != null)
            {
                PrefabUtility.UnpackPrefabInstance(
                    root,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
        }

        private static void RestorePartsToRoot(Transform visual, IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                Transform part = FindDeepOptional(visual, name);
                if (part != null && part.parent != visual)
                {
                    part.SetParent(visual, true);
                }
            }
        }

        private static Transform CreateRigPivot(
            Transform parent, string name, Vector3 worldPosition, Quaternion worldRotation)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, true);
            pivot.transform.SetPositionAndRotation(worldPosition, worldRotation);
            return pivot.transform;
        }

        private static void ParentParts(
            Transform searchRoot, Transform parent, params string[] names)
        {
            foreach (string name in names)
            {
                Transform part = FindDeepOptional(searchRoot, name);
                if (part != null)
                {
                    part.SetParent(parent, true);
                }
            }
        }

        private static Vector3 JointAtY(Transform part, float worldY)
        {
            Bounds bounds = RendererBounds(part);
            return new Vector3(bounds.center.x, worldY, bounds.center.z);
        }

        private static Vector3 JointBetween(Transform upper, Transform lower)
        {
            Bounds upperBounds = RendererBounds(upper);
            Bounds lowerBounds = RendererBounds(lower);
            return new Vector3(
                (upperBounds.center.x + lowerBounds.center.x) * 0.5f,
                (upperBounds.min.y + lowerBounds.max.y) * 0.5f,
                (upperBounds.center.z + lowerBounds.center.z) * 0.5f);
        }

        private static Bounds RendererBounds(Transform part)
        {
            Renderer renderer = part != null
                ? part.GetComponentInChildren<Renderer>(true)
                : null;
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"Rig part has no renderer: {part?.name ?? "<null>"}");
            }
            return renderer.bounds;
        }

        private static Transform RequireDeep(Transform root, string name)
        {
            Transform found = FindDeepOptional(root, name);
            if (found == null)
            {
                throw new InvalidOperationException($"Missing rig part: {name}");
            }
            return found;
        }

        private static Transform FindDeepOptional(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }

        private static GameObject RebuildSentinel(
            Transform sandbox, Dictionary<string, Material> materials)
        {
            DestroyChildIfPresent(sandbox, "Sentinel_v01");

            GameObject sentinel = new GameObject("Sentinel_v01");
            sentinel.transform.SetParent(sandbox, false);
            sentinel.transform.SetPositionAndRotation(SentinelSpawn, Quaternion.identity);

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(SentinelAsset);
            if (asset == null)
            {
                throw new InvalidOperationException($"Model could not be loaded: {SentinelAsset}");
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            visual.name = "SentinelVisual";
            visual.transform.SetParent(sentinel.transform, false);
            visual.transform.localPosition = asset.transform.localPosition;
            visual.transform.localRotation = asset.transform.localRotation;
            visual.transform.localScale = asset.transform.localScale * BossVisualScale;
            RemapMaterials(visual, materials);
            AlignFeetToWorld(visual, SentinelSpawn.y);
            BuildSentinelProxyRig(visual.transform);

            return sentinel;
        }

        private static void BuildFirstThresholdArena(
            Transform sandbox, Dictionary<string, Material> materials)
        {
            DestroyChildIfPresent(sandbox, "FirstThresholdArena_v03");

            GameObject arena = new GameObject("FirstThresholdArena_v03");
            arena.transform.SetParent(sandbox, false);

            Material dark = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/DarkMetal.mat");
            Material ceramic = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/MuscaCeramic.mat");
            Material water = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/CyanSoft.mat");
            Material cyan = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/Cyan.mat");
            Material amber = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/AccentOrange.mat");

            CreateArenaCube(
                arena.transform, "ArenaFloor",
                new Vector3(0f, -0.12f, 34f),
                new Vector3(22f, 0.24f, 42f), dark, true);

            CreateArenaCube(
                arena.transform, "WaterBasin_L",
                new Vector3(-6.8f, 0.005f, 35f),
                new Vector3(7.0f, 0.012f, 36f), water, false);
            CreateArenaCube(
                arena.transform, "WaterBasin_R",
                new Vector3(6.8f, 0.005f, 35f),
                new Vector3(7.0f, 0.012f, 36f), water, false);

            CreateArenaCube(
                arena.transform, "ThresholdCauseway",
                new Vector3(0f, 0.035f, 35f),
                new Vector3(5.8f, 0.07f, 36f), dark, false);
            CreateArenaCube(
                arena.transform, "CausewayEdge_L",
                new Vector3(-2.7f, 0.075f, 35f),
                new Vector3(0.16f, 0.05f, 36f), ceramic, false);
            CreateArenaCube(
                arena.transform, "CausewayEdge_R",
                new Vector3(2.7f, 0.075f, 35f),
                new Vector3(0.16f, 0.05f, 36f), ceramic, false);

            CreateArenaCube(
                arena.transform, "BossDais",
                new Vector3(0f, 0.08f, 34f),
                new Vector3(7.4f, 0.16f, 7.0f), ceramic, false);
            CreateArenaCube(
                arena.transform, "BossDaisTop",
                new Vector3(0f, 0.14f, 34f),
                new Vector3(5.8f, 0.07f, 5.4f), dark, false);

            float[] zPositions = { 22f, 30f, 38f, 46f };
            float[] heightsLeft = { 5.2f, 3.4f, 6.1f, 4.2f };
            float[] heightsRight = { 3.8f, 5.8f, 4.5f, 6.4f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                CreateArenaCube(
                    arena.transform, $"RuinColumn_L_{i}",
                    new Vector3(-7.8f, heightsLeft[i] * 0.5f, zPositions[i]),
                    new Vector3(0.9f, heightsLeft[i], 0.9f), ceramic, true);
                CreateArenaCube(
                    arena.transform, $"RuinColumn_R_{i}",
                    new Vector3(7.8f, heightsRight[i] * 0.5f, zPositions[i] + 1.1f),
                    new Vector3(0.9f, heightsRight[i], 0.9f), ceramic, true);
            }

            GameObject slabA = CreateArenaCube(
                arena.transform, "BrokenSlab_L",
                new Vector3(-5.2f, 0.55f, 28.5f),
                new Vector3(4.8f, 0.32f, 1.25f), ceramic, false);
            slabA.transform.rotation = Quaternion.Euler(8f, 21f, -12f);
            GameObject slabB = CreateArenaCube(
                arena.transform, "BrokenSlab_R",
                new Vector3(5.6f, 0.42f, 42.8f),
                new Vector3(4.2f, 0.28f, 1.15f), dark, false);
            slabB.transform.rotation = Quaternion.Euler(-5f, -27f, 9f);
            GameObject fallenPillar = CreateArenaCube(
                arena.transform, "FallenPillar",
                new Vector3(-6.2f, 0.62f, 43.5f),
                new Vector3(0.85f, 5.5f, 0.85f), ceramic, false);
            fallenPillar.transform.rotation = Quaternion.Euler(72f, 0f, 24f);

            CreateArenaCube(
                arena.transform, "DistantMonolith_L",
                new Vector3(-11.5f, 5.5f, 56f),
                new Vector3(2.0f, 11f, 2.0f), dark, false);
            CreateArenaCube(
                arena.transform, "DistantMonolith_R",
                new Vector3(11.2f, 6.5f, 58f),
                new Vector3(2.4f, 13f, 2.4f), dark, false);

            CreateArenaCube(
                arena.transform, "ThresholdFrame_L",
                new Vector3(-5.4f, 3.5f, 52f),
                new Vector3(0.9f, 7f, 0.9f), dark, true);
            CreateArenaCube(
                arena.transform, "ThresholdFrame_R",
                new Vector3(5.4f, 3.5f, 52f),
                new Vector3(0.9f, 7f, 0.9f), dark, true);
            CreateArenaCube(
                arena.transform, "ThresholdLintel",
                new Vector3(0f, 7.0f, 52f),
                new Vector3(11.7f, 0.9f, 0.9f), dark, true);
            CreateArenaCube(
                arena.transform, "ThresholdLight",
                new Vector3(0f, 4f, 52.15f),
                new Vector3(0.10f, 8f, 0.10f), cyan, false);

            CreateLineRing(
                arena.transform, "ThresholdRing_A",
                new Vector3(0f, 3.6f, 52.05f), 4.15f, 0.055f, water, Quaternion.identity);
            CreateLineRing(
                arena.transform, "ThresholdRing_B",
                new Vector3(0f, 3.6f, 52.00f), 4.80f, 0.035f, water,
                Quaternion.Euler(0f, 0f, 12f));
            CreateLineRing(
                arena.transform, "ThresholdRing_C",
                new Vector3(0f, 3.6f, 51.95f), 5.45f, 0.025f, amber,
                Quaternion.Euler(0f, 0f, -9f));

            CreateArenaLight(
                arena.transform, "ArenaLight_Cyan",
                new Vector3(-5f, 3.2f, 34f), new Color(0.08f, 0.55f, 1f), 14f, 3.2f);
            CreateArenaLight(
                arena.transform, "ArenaLight_Amber",
                new Vector3(5f, 2.6f, 38f), new Color(1f, 0.28f, 0.06f), 12f, 2.4f);
            CreateArenaDirectionalLight(
                arena.transform,
                "ThresholdSun",
                Quaternion.Euler(38f, -32f, 0f),
                new Color(1f, 0.67f, 0.42f),
                1.15f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.11f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.025f, 0.045f, 0.065f);
            RenderSettings.ambientGroundColor = new Color(0.008f, 0.012f, 0.018f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.018f, 0.035f, 0.055f);
            RenderSettings.fogDensity = 0.012f;

            CreateMarkerTransform(arena.transform, "CombatPlayerSpawn", PlayerSpawn);
            CreateMarkerTransform(arena.transform, "CombatBossSpawn", SentinelSpawn);
        }

        private static Transform BuildKaelSpear(
            Transform sentinel, Dictionary<string, Material> materials)
        {
            GameObject root = new GameObject("KaelSpear");
            root.transform.SetParent(sentinel, false);
            root.transform.localPosition = new Vector3(0.76f, 1.68f, 0.04f);

            Material dark = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/DarkMetal.mat");
            Material cyan = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/Cyan.mat");

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "SpearShaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localScale = new Vector3(0.055f, 1.82f, 0.055f);
            RemoveCollider(shaft);
            SetMaterial(shaft, dark);

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "SpearBlade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0f, 2.02f, 0f);
            blade.transform.localScale = new Vector3(0.16f, 0.48f, 0.07f);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RemoveCollider(blade);
            SetMaterial(blade, cyan);

            return root.transform;
        }

        private static Transform BuildPredictionCrown(
            Transform sentinel, Dictionary<string, Material> materials)
        {
            GameObject crown = new GameObject("PredictionCrown");
            crown.transform.SetParent(sentinel, false);
            crown.transform.localPosition = new Vector3(0f, 2.92f, 0f);

            Material cyan = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/CyanSoft.mat");
            CreateLineRing(
                crown.transform, "CrownRing_A", Vector3.zero,
                0.54f, 0.030f, cyan, Quaternion.Euler(90f, 0f, 0f));
            CreateLineRing(
                crown.transform, "CrownRing_B", Vector3.zero,
                0.64f, 0.026f, cyan, Quaternion.Euler(55f, 20f, 0f));
            CreateLineRing(
                crown.transform, "CrownRing_C", Vector3.zero,
                0.75f, 0.022f, cyan, Quaternion.Euler(-48f, -22f, 0f));
            return crown.transform;
        }

        private static GameObject CreateArenaCube(
            Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            item.transform.localScale = scale;
            if (!keepCollider) RemoveCollider(item);
            SetMaterial(item, material);
            return item;
        }

        private static void CreateLineRing(
            Transform parent, string name, Vector3 localPosition,
            float radius, float width, Material material, Quaternion localRotation)
        {
            GameObject ring = new GameObject(name);
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = localPosition;
            ring.transform.localRotation = localRotation;

            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.numCornerVertices = 3;
            line.numCapVertices = 2;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }
        }

        private static void CreateArenaLight(
            Transform parent, string name, Vector3 position,
            Color color, float range, float intensity)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            Light light = item.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateArenaDirectionalLight(
            Transform parent,
            string name,
            Quaternion rotation,
            Color color,
            float intensity)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.rotation = rotation;
            Light light = item.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateMarkerTransform(
            Transform parent, string name, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
        }

        private static void DestroyChildIfPresent(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void SetMaterial(GameObject item, Material material)
        {
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
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
            marker.transform.localScale = new Vector3(0.92f, 0.015f, 0.92f);
            RemoveCollider(marker);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            marker.SetActive(false);
            return marker;
        }

        private static GameObject EnsurePredictionTelegraphMarker(
            Transform parent, Vector3 position)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/Cyan.mat");
            GameObject marker = EnsurePrimitive(
                parent, "KaelPredictionTelegraph", PrimitiveType.Cylinder);
            marker.transform.position = new Vector3(position.x, 0.045f, position.z);
            marker.transform.localScale = new Vector3(0.82f, 0.009f, 0.82f);
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
