using UnityEngine;

namespace MUSCA.Gate3D
{
    public enum MuscaBehaviorState
    {
        Calm,
        Curious,
        Scanning,
        Alert,
        Celebrate
    }

    public sealed class MuscaCompanion : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [SerializeField] private Transform leftWingSecondary;
        [SerializeField] private Transform rightWingSecondary;
        [SerializeField] private Vector3 localOffset = new Vector3(-1.05f, -0.15f, -1.15f);
        [SerializeField] private float followSharpness = 4f;
        [SerializeField] private float bobAmplitude = 0.07f;
        [SerializeField] private float wingAmplitudeDegrees = 19f;
        [SerializeField] private MuscaBehaviorState behaviorState = MuscaBehaviorState.Calm;

        private Vector3 _homeOffset;
        private Transform _attentionTarget;
        private Quaternion _leftBase;
        private Quaternion _rightBase;
        private Quaternion _leftSecondaryBase;
        private Quaternion _rightSecondaryBase;

        public MuscaBehaviorState BehaviorState => behaviorState;

        private void Awake()
        {
            _homeOffset = localOffset;
            CaptureWingPose();
        }

        public void Configure(Transform target, Transform wingLeft, Transform wingRight)
        {
            followTarget = target;
            leftWing = wingLeft;
            rightWing = wingRight;
            CaptureWingPose();
        }

        public void ConfigureVisualWings(Transform primaryLeft, Transform primaryRight,
            Transform secondaryLeft, Transform secondaryRight)
        {
            leftWing = primaryLeft;
            rightWing = primaryRight;
            leftWingSecondary = secondaryLeft;
            rightWingSecondary = secondaryRight;
            CaptureWingPose();
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetOffset(Vector3 offset)
        {
            localOffset = offset;
            _homeOffset = offset;
        }

        public void SetBehaviorState(MuscaBehaviorState state)
        {
            behaviorState = state;
        }

        public void SetAttentionTarget(Transform target)
        {
            _attentionTarget = target;
        }

        private void CaptureWingPose()
        {
            if (leftWing != null) _leftBase = leftWing.localRotation;
            if (rightWing != null) _rightBase = rightWing.localRotation;
            if (leftWingSecondary != null) _leftSecondaryBase = leftWingSecondary.localRotation;
            if (rightWingSecondary != null) _rightSecondaryBase = rightWingSecondary.localRotation;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 stateOffset = Vector3.zero;
            float bob = bobAmplitude;
            float flapSpeed = 15f;
            float flapAmplitude = wingAmplitudeDegrees;
            float sharpness = followSharpness;

            switch (behaviorState)
            {
                case MuscaBehaviorState.Curious:
                    stateOffset = new Vector3(-0.05f, 0.08f, 0.18f); bob = 0.045f; flapSpeed = 18f; flapAmplitude = 23f; sharpness = 5.5f;
                    break;
                case MuscaBehaviorState.Scanning:
                    stateOffset = new Vector3(0.10f, 0.03f, 0.28f); bob = 0.022f; flapSpeed = 24f; flapAmplitude = 12f; sharpness = 7f;
                    break;
                case MuscaBehaviorState.Alert:
                    stateOffset = new Vector3(0.20f, 0.16f, 0.04f); bob = 0.035f; flapSpeed = 27f; flapAmplitude = 29f; sharpness = 8f;
                    break;
                case MuscaBehaviorState.Celebrate:
                    stateOffset = new Vector3(Mathf.Sin(Time.time * 2.8f) * 0.14f, 0.15f, 0.20f); bob = 0.11f; flapSpeed = 21f; flapAmplitude = 31f; sharpness = 6f;
                    break;
            }

            Vector3 target = followTarget.TransformPoint(_homeOffset + stateOffset);
            target.y += Mathf.Sin(Time.time * 3.2f) * bob;
            float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);

            Vector3 lookPoint = _attentionTarget != null
                ? _attentionTarget.position
                : followTarget.position + followTarget.forward * 3f + Vector3.up * 1.2f;
            Vector3 lookDirection = lookPoint - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(lookDirection, Vector3.up), t);
            }

            float flap = Mathf.Sin(Time.time * flapSpeed) * flapAmplitude;
            ApplyWing(leftWing, _leftBase, flap);
            ApplyWing(rightWing, _rightBase, -flap);
            ApplyWing(leftWingSecondary, _leftSecondaryBase, -flap * 0.72f);
            ApplyWing(rightWingSecondary, _rightSecondaryBase, flap * 0.72f);
        }

        private static void ApplyWing(Transform wing, Quaternion basis, float degrees)
        {
            if (wing == null) return;
            wing.localRotation = basis * Quaternion.Euler(0f, degrees, 0f);
        }
    }
}
