using System;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public enum DodgeDirection
    {
        None = 0,
        Forward = 1,
        Backward = 2,
        Left = 3,
        Right = 4
    }

    [RequireComponent(typeof(FirstPersonController))]
    [RequireComponent(typeof(PlayerCombatVitals))]
    public sealed class PlayerDodgeController : MonoBehaviour
    {
        [SerializeField] private KeyCode dodgeKey = KeyCode.LeftShift;
        [SerializeField] private float staminaCost = 28f;
        [SerializeField] private float dodgeSpeed = 8.5f;
        [SerializeField] private float dodgeDurationSeconds = 0.28f;
        [SerializeField] private float invulnerableSeconds = 0.18f;

        private FirstPersonController _movement;
        private PlayerCombatVitals _vitals;

        public event Action<DodgeDirection> Dodged;

        public float StaminaCost => staminaCost;
        public bool CanDodge => _movement != null && _movement.CanStartDodge &&
                                _vitals != null && _vitals.IsAlive &&
                                _vitals.CurrentStamina + 0.0001f >= staminaCost;

        private void Awake()
        {
            _movement = GetComponent<FirstPersonController>();
            _vitals = GetComponent<PlayerCombatVitals>();
        }

        private void Update()
        {
            if (!_movement.InputEnabled || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (Input.GetKeyDown(dodgeKey))
            {
                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");
                Vector3 direction = ResolveDodgeDirection(horizontal, vertical, transform.forward, transform.right);
                TryDodge(direction);
            }
        }

        public bool TryDodge(Vector3 worldDirection)
        {
            if (!CanDodge)
            {
                return false;
            }

            if (!_movement.TryStartDodge(worldDirection, dodgeSpeed, dodgeDurationSeconds, invulnerableSeconds))
            {
                return false;
            }

            if (!_vitals.TrySpendStamina(staminaCost))
            {
                _movement.CancelDodge();
                return false;
            }

            Dodged?.Invoke(ClassifyDirection(worldDirection, transform.forward, transform.right));
            return true;
        }

        public static Vector3 ResolveDodgeDirection(float horizontal, float vertical, Vector3 forward, Vector3 right)
        {
            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            Vector3 direction = right * horizontal + forward * vertical;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = -forward;
            }
            return direction.normalized;
        }

        public static DodgeDirection ClassifyDirection(Vector3 worldDirection, Vector3 forward, Vector3 right)
        {
            Vector3 direction = worldDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return DodgeDirection.None;
            direction.Normalize();

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            float forwardDot = Vector3.Dot(direction, forward);
            float rightDot = Vector3.Dot(direction, right);
            if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
            {
                return forwardDot >= 0f ? DodgeDirection.Forward : DodgeDirection.Backward;
            }
            return rightDot >= 0f ? DodgeDirection.Right : DodgeDirection.Left;
        }
    }
}
