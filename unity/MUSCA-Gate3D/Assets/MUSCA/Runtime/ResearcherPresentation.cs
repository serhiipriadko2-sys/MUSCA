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

            _phase += Time.deltaTime * Mathf.Lerp(4.2f, 8.6f, movement);
            float cycle = Mathf.Sin(_phase);
            float oppositeCycle = -cycle;
            float bob = Mathf.Abs(cycle) * 0.035f * movement;

            float leftShoulderX = cycle * 30f * movement;
            float rightShoulderX = oppositeCycle * 30f * movement;
            float leftHipX = oppositeCycle * 34f * movement;
            float rightHipX = cycle * 34f * movement;
            float leftKneeX = Mathf.Max(0f, cycle) * 42f * movement;
            float rightKneeX = Mathf.Max(0f, -cycle) * 42f * movement;
            float leftElbowX = 12f + Mathf.Max(0f, -cycle) * 18f * movement;
            float rightElbowX = 12f + Mathf.Max(0f, cycle) * 18f * movement;

            float rootPitch = Mathf.Abs(forward) * 2.5f;
            float rootRoll = -strafe * 5.5f;
            float rootYaw = 0f;
            float rootDrop = 0f;

            Vector3 torsoEuler = new Vector3(
                Mathf.Abs(forward) * 2f,
                -cycle * 4.5f * movement,
                -strafe * 4f);
            Vector3 headEuler = new Vector3(
                0f,
                cycle * 1.8f * movement,
                strafe * 1.2f);

            if (!grounded)
            {
                float tuck = Mathf.Clamp01(Mathf.Abs(vertical) / 8f);
                leftHipX = -18f - tuck * 16f;
                rightHipX = -18f - tuck * 16f;
                leftKneeX = 38f + tuck * 18f;
                rightKneeX = 38f + tuck * 18f;
                leftShoulderX = 18f;
                rightShoulderX = 18f;
                leftElbowX = 28f;
                rightElbowX = 28f;
                rootPitch += Mathf.Clamp(vertical * -0.55f, -6f, 10f);
                rootDrop = 0.035f;
            }

            if (dodging && _movement != null)
            {
                float t = _movement.DodgeNormalizedTime;
                float pulse = Mathf.Sin(Mathf.PI * t);
                Vector3 dodgeLocal = transform.InverseTransformDirection(
                    _movement.DodgeDirectionWorld);
                dodgeLocal.y = 0f;
                if (dodgeLocal.sqrMagnitude > 0.0001f)
                {
                    dodgeLocal.Normalize();
                }

                rootDrop = Mathf.Max(rootDrop, 0.17f * pulse);
                rootPitch += dodgeLocal.z * 18f * pulse;
                rootRoll += -dodgeLocal.x * 24f * pulse;
                rootYaw += dodgeLocal.x * 9f * pulse;

                torsoEuler = new Vector3(
                    dodgeLocal.z * 14f * pulse,
                    -dodgeLocal.x * 14f * pulse,
                    -dodgeLocal.x * 18f * pulse);

                float side = dodgeLocal.x;
                leftHipX = -14f + side * 12f * pulse;
                rightHipX = -14f - side * 12f * pulse;
                leftKneeX = 34f + Mathf.Max(0f, side) * 18f * pulse;
                rightKneeX = 34f + Mathf.Max(0f, -side) * 18f * pulse;
                leftShoulderX = 26f - side * 10f * pulse;
                rightShoulderX = 26f + side * 10f * pulse;
                leftElbowX = 54f;
                rightElbowX = 54f;
                headEuler = new Vector3(
                    -4f * pulse,
                    dodgeLocal.x * 6f * pulse,
                    dodgeLocal.x * 8f * pulse);
            }

            Quaternion rootTarget = _rootBaseRotation *
                Quaternion.Euler(rootPitch, rootYaw, rootRoll);
            Vector3 rootTargetPosition = _rootBasePosition +
                Vector3.up * (bob - rootDrop);

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
            ApplyPose(torso, _torsoBase, torsoEuler, poseBlend);
            ApplyPose(_head, _headBase, headEuler, poseBlend);
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
