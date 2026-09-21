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
        [SerializeField] private Transform predictionIndicator;
        [SerializeField] private float aggroRange = 8.5f;
        [SerializeField] private float approachSpeed = 1.65f;
        [SerializeField] private float stopDistance = 1.35f;
        [SerializeField] private float telegraphSeconds = 0.7f;
        [SerializeField] private float recoverySeconds = 0.85f;
        [SerializeField] private float attackDamage = 26f;
        [SerializeField] private float turnSpeedDegrees = 540f;
        [SerializeField] private KaelPredictionProbe prediction;
        [SerializeField] private float predictedDodgeDistance = 2.38f;
        [SerializeField] private float strikeRadius = 0.78f;
        [SerializeField] private float predictionStrikeRadius = 0.86f;

        private CombatDamageReceiver _self;
        private SentinelCombatState _state = SentinelCombatState.Idle;
        private float _stateStartedAt;
        private float _stateDuration;
        private float _stateEndsAt;
        private Vector3 _strikePoint;
        private Vector3 _predictionStrikePoint;
        private bool _predictionApplied;

        public SentinelCombatState State => _state;
        public bool TelegraphVisible => telegraphIndicator != null && telegraphIndicator.gameObject.activeSelf;
        public Vector3 CurrentStrikePoint => _strikePoint;
        public Vector3 CurrentPredictionStrikePoint => _predictionStrikePoint;
        public bool PredictionAppliedThisTelegraph => _predictionApplied;
        public float StateProgress => _stateDuration > 0.0001f
            ? Mathf.Clamp01((Time.time - _stateStartedAt) / _stateDuration)
            : 0f;
        public int TelegraphCount { get; private set; }
        public int AttackCount { get; private set; }
        public bool LastAttackHit { get; private set; }

        private void Awake()
        {
            _self = GetComponent<CombatDamageReceiver>();
            SetTelegraphVisible(false);
            SetPredictionVisible(false);
        }

        public void Configure(
            PlayerCombatVitals targetValue,
            Transform telegraphValue,
            Transform predictionIndicatorValue,
            KaelPredictionProbe predictionValue = null,
            float predictionDistance = 2.38f,
            float committedStrikeRadius = 0.78f,
            float predictedStrikeRadius = 0.86f)
        {
            target = targetValue;
            telegraphIndicator = telegraphValue;
            predictionIndicator = predictionIndicatorValue;
            prediction = predictionValue;
            predictedDodgeDistance = Mathf.Max(0f, predictionDistance);
            strikeRadius = Mathf.Max(0.05f, committedStrikeRadius);
            predictionStrikeRadius = Mathf.Max(0.05f, predictedStrikeRadius);
            SetTelegraphVisible(false);
            SetPredictionVisible(false);
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
            _stateStartedAt = Time.time;
            _stateDuration = 0f;
            _predictionApplied = false;
            SetTelegraphVisible(false);
            SetPredictionVisible(false);
        }

        private void EnterApproach()
        {
            _state = SentinelCombatState.Approach;
            _stateStartedAt = Time.time;
            _stateDuration = 0f;
            _predictionApplied = false;
            SetTelegraphVisible(false);
            SetPredictionVisible(false);
        }

        private void EnterTelegraph()
        {
            _state = SentinelCombatState.Telegraph;
            _stateStartedAt = Time.time;
            _stateDuration = telegraphSeconds;
            _stateEndsAt = Time.time + telegraphSeconds;
            TelegraphCount++;

            _strikePoint = target != null
                ? target.transform.position
                : transform.position;
            _predictionStrikePoint = _strikePoint;
            _predictionApplied = prediction != null &&
                prediction.TryGetPredictedStrikePoint(
                    predictedDodgeDistance, out _predictionStrikePoint);

            SetTelegraphVisible(true);
            SetPredictionVisible(_predictionApplied);
            UpdateTelegraphPosition();
        }

        private void EnterRecovery()
        {
            _state = SentinelCombatState.Recovery;
            _stateStartedAt = Time.time;
            _stateDuration = recoverySeconds;
            _stateEndsAt = Time.time + recoverySeconds;
            SetTelegraphVisible(false);
            SetPredictionVisible(false);
        }

        private void Strike()
        {
            AttackCount++;
            Vector3 point = target.transform.position;

            bool baseHit = IsPointInsideStrike(
                point, _strikePoint, strikeRadius);
            bool predictionHit = _predictionApplied &&
                IsPointInsideStrike(
                    point, _predictionStrikePoint, predictionStrikeRadius);

            LastAttackHit = baseHit || predictionHit;
            if (LastAttackHit)
            {
                target.ApplyDamage(attackDamage);
            }
        }

        private void UpdateTelegraphPosition()
        {
            if (telegraphIndicator != null)
            {
                Vector3 position = _strikePoint;
                position.y = 0.04f;
                telegraphIndicator.position = position;
            }

            if (predictionIndicator != null)
            {
                Vector3 predictionPosition = _predictionStrikePoint;
                predictionPosition.y = 0.045f;
                predictionIndicator.position = predictionPosition;
            }
        }

        private void SetTelegraphVisible(bool value)
        {
            if (telegraphIndicator != null &&
                telegraphIndicator.gameObject.activeSelf != value)
            {
                telegraphIndicator.gameObject.SetActive(value);
            }
        }

        private void SetPredictionVisible(bool value)
        {
            if (predictionIndicator != null &&
                predictionIndicator.gameObject.activeSelf != value)
            {
                predictionIndicator.gameObject.SetActive(value);
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
