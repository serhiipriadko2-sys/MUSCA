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

        private Vector3 _rootBasePosition;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftThighBase;
        private Quaternion _rightThighBase;

        private void Awake()
        {
            CapturePose();
        }

        public void Configure(CharacterController value, Transform root,
            Transform armLeft, Transform armRight, Transform thighLeft, Transform thighRight)
        {
            controller = value;
            visualRoot = root;
            leftArm = armLeft;
            rightArm = armRight;
            leftThigh = thighLeft;
            rightThigh = thighRight;
            CapturePose();
        }

        private void CapturePose()
        {
            if (visualRoot != null) _rootBasePosition = visualRoot.localPosition;
            if (leftArm != null) _leftArmBase = leftArm.localRotation;
            if (rightArm != null) _rightArmBase = rightArm.localRotation;
            if (leftThigh != null) _leftThighBase = leftThigh.localRotation;
            if (rightThigh != null) _rightThighBase = rightThigh.localRotation;
        }

        private void LateUpdate()
        {
            if (controller == null || visualRoot == null) return;

            Vector3 planar = controller.velocity;
            planar.y = 0f;
            float movement = Mathf.Clamp01(planar.magnitude / 4.6f);
            float phase = Time.time * Mathf.Lerp(4f, 8.5f, movement);
            float swing = Mathf.Sin(phase) * 20f * movement;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.018f * movement;

            visualRoot.localPosition = _rootBasePosition + Vector3.up * bob;
            ApplySwing(leftArm, _leftArmBase, swing);
            ApplySwing(rightArm, _rightArmBase, -swing);
            ApplySwing(leftThigh, _leftThighBase, -swing * 0.72f);
            ApplySwing(rightThigh, _rightThighBase, swing * 0.72f);
        }

        private static void ApplySwing(Transform target, Quaternion basis, float degrees)
        {
            if (target == null) return;
            target.localRotation = basis * Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
