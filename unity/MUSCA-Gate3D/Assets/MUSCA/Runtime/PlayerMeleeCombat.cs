using System.Collections.Generic;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class PlayerMeleeCombat : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float damage = 34f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackRadius = 0.72f;
        [SerializeField] private float attackCooldownSeconds = 0.55f;
        [SerializeField] private float minForwardDot = 0.12f;
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly Collider[] _hits = new Collider[24];
        private readonly HashSet<CombatDamageReceiver> _receivers = new HashSet<CombatDamageReceiver>();
        private float _nextAttackAt;

        public int LastHitCount { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, _nextAttackAt - Time.time);

        public void Configure(Camera cameraValue, float damageValue = 34f)
        {
            viewCamera = cameraValue;
            damage = Mathf.Max(1f, damageValue);
        }

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryAttack();
            }
        }

        public int TryAttack()
        {
            if (Time.time < _nextAttackAt)
            {
                return 0;
            }

            _nextAttackAt = Time.time + attackCooldownSeconds;
            LastHitCount = 0;
            _receivers.Clear();

            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 1.05f + forward * 0.25f;
            Vector3 center = origin + forward * Mathf.Min(attackRange * 0.55f, 0.9f);
            int count = Physics.OverlapSphereNonAlloc(center, attackRange + attackRadius, _hits,
                hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider item = _hits[i];
                if (item == null || item.transform.IsChildOf(transform))
                {
                    continue;
                }

                CombatDamageReceiver receiver = item.GetComponentInParent<CombatDamageReceiver>();
                if (receiver == null || !receiver.IsAlive || !_receivers.Add(receiver))
                {
                    continue;
                }

                Vector3 target = item.ClosestPoint(origin);
                if (!IsWithinAttackArc(origin, forward, target, attackRange, minForwardDot))
                {
                    continue;
                }

                receiver.ApplyDamage(damage);
                LastHitCount++;
            }

            return LastHitCount;
        }

        public static bool IsWithinAttackArc(Vector3 origin, Vector3 forward, Vector3 point,
            float range, float minimumDot)
        {
            Vector3 delta = point - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude > range * range)
            {
                return false;
            }

            if (delta.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Vector3 planarForward = forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float dot = Vector3.Dot(planarForward.normalized, delta.normalized);
            return dot >= minimumDot;
        }
    }
}
