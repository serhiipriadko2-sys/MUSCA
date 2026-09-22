using UnityEngine;

namespace MUSCA.Gate3D
{
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class PlayerLockOn : MonoBehaviour
    {
        [SerializeField] private KeyCode keyboardToggle = KeyCode.Q;
        [SerializeField] private bool middleMouseToggle = true;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float maxAngleDegrees = 82f;

        private FirstPersonController _movement;
        private CombatDamageReceiver _target;

        public bool IsLocked => _target != null && _target.IsAlive;
        public CombatDamageReceiver Target => IsLocked ? _target : null;
        public string TargetName => IsLocked ? _target.gameObject.name : string.Empty;

        private void Awake()
        {
            _movement = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (_target != null)
            {
                if (!_target.IsAlive || PlanarDistance(transform.position, _target.transform.position) > maxDistance * 1.25f)
                {
                    ClearTarget();
                }
            }

            if (!_movement.InputEnabled || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            bool toggle = Input.GetKeyDown(keyboardToggle) ||
                          (middleMouseToggle && Input.GetMouseButtonDown(2));
            if (!toggle) return;

            if (IsLocked) ClearTarget();
            else TryLockNearest();
        }

        public bool TryLockNearest()
        {
            CombatDamageReceiver[] candidates =
                FindObjectsByType<CombatDamageReceiver>(FindObjectsInactive.Exclude);

            Camera camera = _movement.PlayerCamera;
            Vector3 playerOrigin = transform.position + Vector3.up;
            Vector3 viewOrigin = camera != null
                ? camera.transform.position
                : playerOrigin;
            Vector3 forward = camera != null
                ? camera.transform.forward
                : transform.forward;

            CombatDamageReceiver best = null;
            float bestScore = float.PositiveInfinity;
            foreach (CombatDamageReceiver candidate in candidates)
            {
                if (candidate == null || !candidate.IsAlive) continue;

                Vector3 playerDelta = candidate.transform.position - playerOrigin;
                float distance = playerDelta.magnitude;
                if (distance > maxDistance || distance < 0.001f) continue;

                Vector3 viewDelta = candidate.transform.position - viewOrigin;
                if (viewDelta.sqrMagnitude < 0.0001f) continue;
                float angle = Vector3.Angle(forward, viewDelta);
                if (angle > maxAngleDegrees) continue;

                float score = ScoreCandidate(distance, angle, maxDistance, maxAngleDegrees);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null)
            {
                ClearTarget();
                return false;
            }

            _target = best;
            _movement.SetLockOnTarget(best.transform);
            return true;
        }

        public void ClearTarget()
        {
            _target = null;
            if (_movement != null) _movement.SetLockOnTarget(null);
        }

        public static float ScoreCandidate(float distance, float angle, float distanceLimit, float angleLimit)
        {
            float d = distanceLimit <= 0f ? 1f : Mathf.Clamp01(distance / distanceLimit);
            float a = angleLimit <= 0f ? 1f : Mathf.Clamp01(angle / angleLimit);
            return a * 0.7f + d * 0.3f;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
