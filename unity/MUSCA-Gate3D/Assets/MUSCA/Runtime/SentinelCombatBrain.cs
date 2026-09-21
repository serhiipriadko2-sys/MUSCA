using UnityEngine;

namespace MUSCA.Gate3D
{
    public enum SentinelCombatState
    {
        Idle = 0,
        Approach = 1,
        Telegraph = 2,
        Recovery = 3
    }

    [RequireComponent(typeof(CombatDamageReceiver))]
    public sealed class SentinelCombatBrain : MonoBehaviour
    {
        [SerializeField] private PlayerCombatVitals target;
        [SerializeField] private Transform telegraphIndicator;
        [SerializeField] private float aggroRange = 8.5f;
        [SerializeField] private float approachSpeed = 1.65f;
        [SerializeField] private float stopDistance = 1.35f;
        [SerializeField] private float telegraphSeconds = 0.7f;
        [SerializeField] private float recoverySeconds = 0.85f;
        [SerializeField] private float attackDamage = 26f;
        [SerializeField] private float turnSpeedDegrees = 540f;
        [SerializeField] private KaelPredictionProbe prediction;
        [SerializeField] private float predictedDodgeDistance = 2.38f;
        [SerializeField] private float strikeRadius = 0.95f;

        private CombatDamageReceiver _self;
        private SentinelCombatState _state = SentinelCombatState.Idle;
        private float _stateEndsAt;
        private Vector3 _strikePoint;
        private bool _predictionApplied;

        public SentinelCombatState State => _state;
        public bool TelegraphVisible => telegraphIndicator != null && telegraphIndicator.gameObject.activeSelf;
        public Vector3 CurrentStrikePoint => _strikePoint;
        public bool PredictionAppliedThisTelegraph => _predictionApplied;
        public int TelegraphCount { get; private set; }
        public int AttackCount { get; private set; }
        public bool LastAttackHit { get; private set; }

        private void Awake()
        {
            _self = GetComponent<CombatDamageReceiver>();
            SetTelegraphVisible(false);
        }

        public void Configure(
            PlayerCombatVitals targetValue,
            Transform telegraphValue,
            KaelPredictionProbe predictionValue = null,
            float predictionDistance = 2.38f,
            float committedStrikeRadius = 0.95f)
        {
            target = targetValue;
            telegraphIndicator = telegraphValue;
            prediction = predictionValue;
            predictedDodgeDistance = Mathf.Max(0f, predictionDistance);
            strikeRadius = Mathf.Max(0.05f, committedStrikeRadius);
            SetTelegraphVisible(false);
        }

        private void Update()
        {
            if (_self == null || !_self.IsAlive || target == null || !target.IsAlive)
            {
                EnterIdle();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (_state == SentinelCombatState.Idle)
            {
                if (distance <= aggroRange) EnterApproach();
                return;
            }

            FaceTarget(toTarget);

            switch (_state)
            {
                case SentinelCombatState.Approach:
                    if (distance <= stopDistance)
                    {
                        EnterTelegraph();
                    }
                    else
                    {
                        float step = Mathf.Min(approachSpeed * Time.deltaTime, Mathf.Max(0f, distance - stopDistance));
                        transform.position += toTarget.normalized * step;
                    }
                    break;

                case SentinelCombatState.Telegraph:
                    UpdateTelegraphPosition();
                    if (Time.time >= _stateEndsAt)
                    {
                        Strike();
                        EnterRecovery();
                    }
                    break;

                case SentinelCombatState.Recovery:
                    if (Time.time >= _stateEndsAt)
                    {
                        EnterApproach();
                    }
                    break;
            }
        }

        private void FaceTarget(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude < 0.0001f) return;
            Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, desired, turnSpeedDegrees * Time.deltaTime);
        }

        private void EnterIdle()
        {
            _state = SentinelCombatState.Idle;
            _predictionApplied = false;
            SetTelegraphVisible(false);
        }

        private void EnterApproach()
        {
            _state = SentinelCombatState.Approach;
            _predictionApplied = false;
            SetTelegraphVisible(false);
        }

        private void EnterTelegraph()
        {
            _state = SentinelCombatState.Telegraph;
            _stateEndsAt = Time.time + telegraphSeconds;
            TelegraphCount++;
            _strikePoint = target != null ? target.transform.position : transform.position;
            _predictionApplied = prediction != null &&
                prediction.TryGetPredictedStrikePoint(predictedDodgeDistance, out _strikePoint);
            SetTelegraphVisible(true);
            UpdateTelegraphPosition();
        }

        private void EnterRecovery()
        {
            _state = SentinelCombatState.Recovery;
            _stateEndsAt = Time.time + recoverySeconds;
            SetTelegraphVisible(false);
        }

        private void Strike()
        {
            AttackCount++;
            Vector3 point = target.transform.position;
            // The telegraph commits the hit point. Ordinary attacks commit to the
            // player's observed position; prediction shifts that committed point.
            // In both cases the visible marker and the damaging area are identical.
            LastAttackHit = IsPointInsideStrike(point, _strikePoint, strikeRadius);
            if (LastAttackHit)
            {
                target.ApplyDamage(attackDamage);
            }
        }

        private void UpdateTelegraphPosition()
        {
            if (telegraphIndicator == null) return;
            Vector3 position = _strikePoint;
            position.y = 0.04f;
            telegraphIndicator.position = position;
        }

        private void SetTelegraphVisible(bool value)
        {
            if (telegraphIndicator != null && telegraphIndicator.gameObject.activeSelf != value)
            {
                telegraphIndicator.gameObject.SetActive(value);
            }
        }

        public static bool IsPointInsideStrike(
            Vector3 currentPoint, Vector3 committedPoint, float radius)
        {
            if (radius < 0f) return false;
            currentPoint.y = 0f;
            committedPoint.y = 0f;
            return Vector3.Distance(currentPoint, committedPoint) <= radius;
        }

    }
}
