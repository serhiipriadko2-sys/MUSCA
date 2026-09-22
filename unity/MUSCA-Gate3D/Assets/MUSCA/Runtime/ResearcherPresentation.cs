using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class ResearcherPresentation : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftThigh;
        [SerializeField] private Transform rightThigh;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform rightShin;
        [SerializeField] private Transform torso;

        private Transform _leftElbow;
        private Transform _rightElbow;
        private Transform _pelvis;
        private Transform _leftAnkle;
        private Transform _rightAnkle;
        private Transform _head;
        private FirstPersonController _movement;

        private Vector3 _rootBasePosition;
        private Quaternion _rootBaseRotation;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftThighBase;
        private Quaternion _rightThighBase;
        private Quaternion _leftShinBase;
        private Quaternion _rightShinBase;
        private Quaternion _leftElbowBase;
        private Quaternion _rightElbowBase;
        private Quaternion _pelvisBase;
        private Quaternion _leftAnkleBase;
        private Quaternion _rightAnkleBase;
        private Quaternion _torsoBase;
        private Quaternion _headBase;

        private Vector3 _smoothedLocalVelocity;
        private float _phase;

        private void Awake()
        {
            _movement = GetComponent<FirstPersonController>();
            ResolveOptionalParts();
            CapturePose();
        }

        public void Configure(
            CharacterController value,
            Transform root,
            Transform armLeft,
            Transform armRight,
            Transform thighLeft,
            Transform thighRight)
        {
            controller = value;
            visualRoot = root;
            leftArm = armLeft;
            rightArm = armRight;
            leftThigh = thighLeft;
            rightThigh = thighRight;
            _movement = GetComponent<FirstPersonController>();
            ResolveOptionalParts();
            CapturePose();
        }

        private void ResolveOptionalParts()
        {
            if (visualRoot == null) return;

            leftArm = PreferPivot("P04_Shoulder_L", leftArm);
            rightArm = PreferPivot("P04_Shoulder_R", rightArm);
            leftThigh = PreferPivot("P04_Hip_L", leftThigh);
            rightThigh = PreferPivot("P04_Hip_R", rightThigh);
            leftShin = PreferPivot("P04_Knee_L", leftShin);
            rightShin = PreferPivot("P04_Knee_R", rightShin);
            torso = PreferPivot("P04_TorsoPivot", torso);

            _leftElbow = FindDeep(visualRoot, "P04_Elbow_L");
            _rightElbow = FindDeep(visualRoot, "P04_Elbow_R");
            _pelvis = FindDeep(visualRoot, "P04_PelvisPivot");
            _leftAnkle = FindDeep(visualRoot, "P04_Ankle_L");
            _rightAnkle = FindDeep(visualRoot, "P04_Ankle_R");
            _head = FindDeep(visualRoot, "P04_HeadPivot");

            leftShin ??= FindDeep(visualRoot, "P03_Shin_-1");
            rightShin ??= FindDeep(visualRoot, "P03_Shin_1");
            torso ??= FindDeep(visualRoot, "P03_Torso");
        }

        private Transform PreferPivot(string pivotName, Transform fallback)
        {
            Transform pivot = FindDeep(visualRoot, pivotName);
            return pivot != null ? pivot : fallback;
        }

        private void CapturePose()
        {
            if (visualRoot != null)
            {
                _rootBasePosition = visualRoot.localPosition;
                _rootBaseRotation = visualRoot.localRotation;
            }
            if (leftArm != null) _leftArmBase = leftArm.localRotation;
            if (rightArm != null) _rightArmBase = rightArm.localRotation;
            if (leftThigh != null) _leftThighBase = leftThigh.localRotation;
            if (rightThigh != null) _rightThighBase = rightThigh.localRotation;
            if (leftShin != null) _leftShinBase = leftShin.localRotation;
            if (rightShin != null) _rightShinBase = rightShin.localRotation;
            if (_leftElbow != null) _leftElbowBase = _leftElbow.localRotation;
            if (_rightElbow != null) _rightElbowBase = _rightElbow.localRotation;
            if (_pelvis != null) _pelvisBase = _pelvis.localRotation;
            if (_leftAnkle != null) _leftAnkleBase = _leftAnkle.localRotation;
            if (_rightAnkle != null) _rightAnkleBase = _rightAnkle.localRotation;
            if (torso != null) _torsoBase = torso.localRotation;
            if (_head != null) _headBase = _head.localRotation;
        }

        private void LateUpdate()
        {
            if (controller == null || visualRoot == null) return;

            Vector3 localVelocity =
                transform.InverseTransformDirection(controller.velocity);
            localVelocity.y = 0f;
            float velocityBlend = 1f - Mathf.Exp(-12f * Time.deltaTime);
            _smoothedLocalVelocity = Vector3.Lerp(
                _smoothedLocalVelocity, localVelocity, velocityBlend);

            float speed = _smoothedLocalVelocity.magnitude;
            float movement = Mathf.Clamp01(speed / 4.6f);
            float forward = Mathf.Clamp(
                _smoothedLocalVelocity.z / 4.6f, -1f, 1f);
            float strafe = Mathf.Clamp(
                _smoothedLocalVelocity.x / 4.6f, -1f, 1f);

            bool dodging = _movement != null && _movement.IsDodging;
            bool grounded = _movement == null || _movement.IsGrounded;
            float vertical = _movement != null
                ? _movement.VerticalVelocity
                : controller.velocity.y;

            // Gait phase is distance-driven, not clock-driven. A foot cycle now
            // advances only when the controller really travels through the world,
            // which reduces the treadmill / puppet impression during acceleration.
            _phase += speed * Time.deltaTime * 1.92f;
            float cycle = Mathf.Sin(_phase);
            float forwardWeight = Mathf.Clamp01(Mathf.Abs(forward) * 1.25f);
            float strafeWeight = Mathf.Clamp01(Mathf.Abs(strafe) * 1.35f);
            float gaitDirection = forward < -0.08f ? -1f : 1f;
            float stride = cycle * gaitDirection;
            float sagittalWeight = Mathf.Lerp(0.42f, 1f, forwardWeight);
            float bob = Mathf.Abs(cycle) * 0.028f * movement;
            float lateralSway = cycle * 0.018f * movement;

            float leftShoulderX = stride * 34f * movement * sagittalWeight;
            float rightShoulderX = -stride * 34f * movement * sagittalWeight;
            float leftHipX = -stride * 39f * movement * sagittalWeight;
            float rightHipX = stride * 39f * movement * sagittalWeight;
            float leftKneeX = Mathf.Max(0f, stride) * 48f * movement;
            float rightKneeX = Mathf.Max(0f, -stride) * 48f * movement;
            float leftElbowX = 10f + Mathf.Max(0f, -stride) * 24f * movement;
            float rightElbowX = 10f + Mathf.Max(0f, stride) * 24f * movement;

            float rootPitch = Mathf.Abs(forward) * 3.2f;
            float rootRoll = -strafe * 7.5f;
            float rootYaw = -strafe * 2.5f;
            float rootDrop = 0f;

            Vector3 pelvisEuler = new Vector3(
                0f,
                stride * 5.5f * movement * sagittalWeight,
                -strafe * 5f + cycle * 2.5f * movement);
            // Counter-rotate the feet against hip/knee swing so the sole
            // spends more of each step near level instead of behaving like a
            // rigid pendulum. This is a proxy foot-plant until humanoid IK lands.
            float leftFootPitch = Mathf.Clamp(
                -leftHipX * 0.30f - leftKneeX * 0.48f,
                -22f,
                18f);
            float rightFootPitch = Mathf.Clamp(
                -rightHipX * 0.30f - rightKneeX * 0.48f,
                -22f,
                18f);
            Vector3 leftAnkleEuler = new Vector3(
                leftFootPitch * movement,
                0f,
                -strafe * 6f * strafeWeight);
            Vector3 rightAnkleEuler = new Vector3(
                rightFootPitch * movement,
                0f,
                -strafe * 6f * strafeWeight);

            Vector3 torsoEuler = new Vector3(
                Mathf.Abs(forward) * 2.8f,
                -stride * 6.5f * movement * sagittalWeight,
                -strafe * 5.5f - cycle * 2f * movement);
            Vector3 headEuler = new Vector3(
                0f,
                stride * 2.2f * movement,
                strafe * 1.5f);

            if (!grounded)
            {
                float tuck = Mathf.Clamp01(Mathf.Abs(vertical) / 8f);
                leftHipX = -18f - tuck * 18f;
                rightHipX = -18f - tuck * 18f;
                leftKneeX = 38f + tuck * 20f;
                rightKneeX = 38f + tuck * 20f;
                leftShoulderX = 18f;
                rightShoulderX = 18f;
                leftElbowX = 30f;
                rightElbowX = 30f;
                pelvisEuler = new Vector3(-4f * tuck, 0f, 0f);
                leftAnkleEuler = new Vector3(-12f * tuck, 0f, 0f);
                rightAnkleEuler = new Vector3(-12f * tuck, 0f, 0f);
                rootPitch += Mathf.Clamp(vertical * -0.55f, -6f, 10f);
                rootDrop = 0.035f;
            }

            if (dodging && _movement != null)
            {
                float t = _movement.DodgeNormalizedTime;
                float engage = Smooth01(Mathf.Clamp01(t / 0.18f));
                float release = 1f - Smooth01(
                    Mathf.Clamp01((t - 0.68f) / 0.32f));
                float brace = engage * release;

                Vector3 dodgeLocal = transform.InverseTransformDirection(
                    _movement.DodgeDirectionWorld);
                dodgeLocal.y = 0f;
                if (dodgeLocal.sqrMagnitude > 0.0001f)
                {
                    dodgeLocal.Normalize();
                }

                rootDrop = Mathf.Max(rootDrop, 0.22f * brace);
                rootPitch += dodgeLocal.z * 22f * brace;
                rootRoll += -dodgeLocal.x * 31f * brace;
                rootYaw += dodgeLocal.x * 12f * brace;

                pelvisEuler = new Vector3(
                    -6f * brace,
                    dodgeLocal.x * 10f * brace,
                    -dodgeLocal.x * 13f * brace);
                torsoEuler = new Vector3(
                    dodgeLocal.z * 18f * brace,
                    -dodgeLocal.x * 18f * brace,
                    -dodgeLocal.x * 23f * brace);

                float side = dodgeLocal.x;
                float forwardDodge = dodgeLocal.z;
                leftHipX = -18f + side * 14f * brace -
                    forwardDodge * 9f * brace;
                rightHipX = -18f - side * 14f * brace -
                    forwardDodge * 9f * brace;
                leftKneeX = 38f + Mathf.Max(0f, side) * 24f * brace;
                rightKneeX = 38f + Mathf.Max(0f, -side) * 24f * brace;
                leftShoulderX = 30f - side * 14f * brace;
                rightShoulderX = 30f + side * 14f * brace;
                leftElbowX = 58f;
                rightElbowX = 58f;

                leftAnkleEuler = new Vector3(
                    -10f * brace,
                    0f,
                    -side * 15f * brace);
                rightAnkleEuler = new Vector3(
                    -10f * brace,
                    0f,
                    -side * 15f * brace);
                headEuler = new Vector3(
                    -5f * brace,
                    dodgeLocal.x * 7f * brace,
                    dodgeLocal.x * 10f * brace);
            }

            Quaternion rootTarget = _rootBaseRotation *
                Quaternion.Euler(rootPitch, rootYaw, rootRoll);
            Vector3 rootTargetPosition = _rootBasePosition +
                new Vector3(
                    lateralSway - strafe * 0.012f * movement,
                    bob - rootDrop,
                    0f);

            float poseBlend = 1f - Mathf.Exp(-20f * Time.deltaTime);
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition, rootTargetPosition, poseBlend);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation, rootTarget, poseBlend);

            ApplyPose(leftArm, _leftArmBase,
                new Vector3(leftShoulderX, 0f, 0f), poseBlend);
            ApplyPose(rightArm, _rightArmBase,
                new Vector3(rightShoulderX, 0f, 0f), poseBlend);
            ApplyPose(_leftElbow, _leftElbowBase,
                new Vector3(leftElbowX, 0f, 0f), poseBlend);
            ApplyPose(_rightElbow, _rightElbowBase,
                new Vector3(rightElbowX, 0f, 0f), poseBlend);
            ApplyPose(leftThigh, _leftThighBase,
                new Vector3(leftHipX, 0f, 0f), poseBlend);
            ApplyPose(rightThigh, _rightThighBase,
                new Vector3(rightHipX, 0f, 0f), poseBlend);
            ApplyPose(leftShin, _leftShinBase,
                new Vector3(leftKneeX, 0f, 0f), poseBlend);
            ApplyPose(rightShin, _rightShinBase,
                new Vector3(rightKneeX, 0f, 0f), poseBlend);
            ApplyPose(_pelvis, _pelvisBase, pelvisEuler, poseBlend);
            ApplyPose(_leftAnkle, _leftAnkleBase, leftAnkleEuler, poseBlend);
            ApplyPose(_rightAnkle, _rightAnkleBase, rightAnkleEuler, poseBlend);
            ApplyPose(torso, _torsoBase, torsoEuler, poseBlend);
            ApplyPose(_head, _headBase, headEuler, poseBlend);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static void ApplyPose(
            Transform target, Quaternion basis, Vector3 euler, float blend)
        {
            if (target == null) return;
            Quaternion desired = basis * Quaternion.Euler(euler);
            target.localRotation = Quaternion.Slerp(
                target.localRotation, desired, blend);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
