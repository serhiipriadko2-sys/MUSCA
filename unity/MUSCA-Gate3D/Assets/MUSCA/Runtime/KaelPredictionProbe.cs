using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class KaelPredictionProbe : MonoBehaviour
    {
        [SerializeField] private PlayerDodgeController playerDodge;
        [SerializeField] private PlayerCombatVitals target;
        [SerializeField] private KeyCode toggleKey = KeyCode.K;
        [SerializeField] private bool prototypeActive;
        [SerializeField] private int historyWindow = 7;
        [SerializeField] private int minimumSamples = 4;
        [SerializeField] private float lockConfidence = 0.6f;
        [SerializeField] private float maxPredictionAgeSeconds = 3.2f;

        private KaelPredictionModel _model;
        private bool _subscribed;
        private float _lastDodgeRecordedAt = float.NegativeInfinity;

        public bool PrototypeActive => prototypeActive;
        public KaelPredictionSnapshot Snapshot => Model.Snapshot();
        public float LastDodgeAgeSeconds => float.IsNegativeInfinity(_lastDodgeRecordedAt)
            ? float.PositiveInfinity
            : Mathf.Max(0f, Time.time - _lastDodgeRecordedAt);
        public bool PredictionFresh =>
            LastDodgeAgeSeconds <= Mathf.Max(0.1f, maxPredictionAgeSeconds);

        private KaelPredictionModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = new KaelPredictionModel(
                        Mathf.Max(1, historyWindow),
                        Mathf.Clamp(minimumSamples, 1, Mathf.Max(1, historyWindow)),
                        Mathf.Clamp(lockConfidence, 0.01f, 1f));
                }
                return _model;
            }
        }

        private void Awake()
        {
            if (playerDodge == null) playerDodge = FindAnyObjectByType<PlayerDodgeController>();
            if (target == null) target = FindAnyObjectByType<PlayerCombatVitals>();
            _ = Model;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetPrototypeActive(!prototypeActive, true);
            }
        }

        public void Configure(PlayerDodgeController dodge, PlayerCombatVitals targetValue)
        {
            Unsubscribe();
            playerDodge = dodge;
            target = targetValue;
            if (Application.isPlaying) Subscribe();
        }

        public void SetPrototypeActive(bool active, bool resetHistory)
        {
            prototypeActive = active;
            if (resetHistory)
            {
                Model.Clear();
                _lastDodgeRecordedAt = float.NegativeInfinity;
            }
        }

        public void RecordDodgeForQa(DodgeDirection direction)
        {
            Model.Record(direction);
            if (direction != DodgeDirection.None)
            {
                _lastDodgeRecordedAt = Time.time;
            }
        }

        public bool TryGetPredictedStrikePoint(float projectedDodgeDistance, out Vector3 point)
        {
            point = target != null ? target.transform.position : transform.position;
            if (!prototypeActive || target == null)
            {
                return false;
            }

            KaelPredictionSnapshot snapshot = Snapshot;
            if (!PredictionFresh ||
                !snapshot.Locked ||
                snapshot.Direction == DodgeDirection.None)
            {
                return false;
            }

            Vector3 direction = KaelPredictionModel.ResolveWorldDirection(
                snapshot.Direction, target.transform.forward, target.transform.right);
            point = target.transform.position + direction * Mathf.Max(0f, projectedDodgeDistance);
            return direction.sqrMagnitude > 0.0001f;
        }

        private void Subscribe()
        {
            if (_subscribed || playerDodge == null) return;
            playerDodge.Dodged += HandleDodge;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || playerDodge == null) return;
            playerDodge.Dodged -= HandleDodge;
            _subscribed = false;
        }

        private void HandleDodge(DodgeDirection direction)
        {
            if (prototypeActive)
            {
                Model.Record(direction);
                if (direction != DodgeDirection.None)
                {
                    _lastDodgeRecordedAt = Time.time;
                }
            }
        }
    }
}
