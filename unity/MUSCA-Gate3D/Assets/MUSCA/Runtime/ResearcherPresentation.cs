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

        private FirstPersonController _movement;
        private Vector3 _rootBasePosition;
        private Quaternion _rootBaseRotation;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftThighBase;
        private Quaternion _rightThighBase;
        private Quaternion _leftShinBase;
        private Quaternion _rightShinBase;
        private Quaternion _torsoBase;
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
            leftShin ??= FindDeep(visualRoot, "P03_Shin_-1");
            rightShin ??= FindDeep(visualRoot, "P03_Shin_1");
            torso ??= FindDeep(visualRoot, "P03_Torso");
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
            if (torso != null) _torsoBase = torso.localRotation;
        }

        private void LateUpdate()
        {
            if (controller == null || visualRoot == null) return;

            Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);
            localVelocity.y = 0f;
            float velocityBlend = 1f - Mathf.Exp(-10f * Time.deltaTime);
            _smoothedLocalVelocity = Vector3.Lerp(
                _smoothedLocalVelocity, localVelocity, velocityBlend);

            float speed = _smoothedLocalVelocity.magnitude;
            float movement = Mathf.Clamp01(speed / 4.6f);
            float forward = Mathf.Clamp(_smoothedLocalVelocity.z / 4.6f, -1f, 1f);
            float strafe = Mathf.Clamp(_smoothedLocalVelocity.x / 4.6f, -1f, 1f);
            bool dodging = _movement != null && _movement.IsDodging;
            bool grounded = _movement == null || _movement.IsGrounded;
            float vertical = _movement != null ? _movement.VerticalVelocity : controller.velocity.y;

            _phase += Time.deltaTime * Mathf.Lerp(4.8f, 9.2f, movement);
            float sine = Mathf.Sin(_phase);
            float step = sine * 28f * movement;
            float bob = Mathf.Abs(sine) * 0.026f * movement;

            float strafeLean = -strafe * 8f;
            float forwardLean = Mathf.Abs(forward) * 3.5f;
            float jumpLean = !grounded ? Mathf.Clamp(vertical * -0.7f, -8f, 10f) : 0f;
            float dodgeLean = dodging ? 15f : 0f;
            float rootDrop = dodging ? 0.09f : 0f;

            Quaternion rootTarget = _rootBaseRotation *
                Quaternion.Euler(forwardLean + jumpLean + dodgeLean, 0f, strafeLean);
            Vector3 rootTargetPosition = _rootBasePosition +
                Vector3.up * (bob - rootDrop);

            float poseBlend = 1f - Mathf.Exp(-16f * Time.deltaTime);
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition, rootTargetPosition, poseBlend);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation, rootTarget, poseBlend);

            float armSwing = dodging ? 34f : step;
            float thighSwing = dodging ? -22f : -step * 0.78f;
            float strafeArmBias = strafe * 12f;
            float strafeLegBias = strafe * 9f;
            float airborneTuck = grounded ? 0f : 18f;

            ApplyPose(leftArm, _leftArmBase,
                new Vector3(armSwing + airborneTuck, 0f, strafeArmBias), poseBlend);
            ApplyPose(rightArm, _rightArmBase,
                new Vector3(-armSwing + airborneTuck, 0f, strafeArmBias), poseBlend);
            ApplyPose(leftThigh, _leftThighBase,
                new Vector3(thighSwing - airborneTuck, 0f, strafeLegBias), poseBlend);
            ApplyPose(rightThigh, _rightThighBase,
                new Vector3(-thighSwing - airborneTuck, 0f, strafeLegBias), poseBlend);
            ApplyPose(leftShin, _leftShinBase,
                new Vector3(-thighSwing * 0.42f + airborneTuck * 0.7f, 0f, 0f), poseBlend);
            ApplyPose(rightShin, _rightShinBase,
                new Vector3(thighSwing * 0.42f + airborneTuck * 0.7f, 0f, 0f), poseBlend);
            ApplyPose(torso, _torsoBase,
                new Vector3(dodging ? 8f : 0f, strafe * -5f, strafe * -4f), poseBlend);
        }

        private static void ApplyPose(
            Transform target, Quaternion basis, Vector3 euler, float blend)
        {
            if (target == null) return;
            Quaternion desired = basis * Quaternion.Euler(euler);
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, blend);
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
    }
}
