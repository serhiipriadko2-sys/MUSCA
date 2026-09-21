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

        private KaelPredictionModel _model;
        private bool _subscribed;

        public bool PrototypeActive => prototypeActive;
        public KaelPredictionSnapshot Snapshot => Model.Snapshot();

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
            if (resetHistory) Model.Clear();
        }

        public void RecordDodgeForQa(DodgeDirection direction)
        {
            Model.Record(direction);
        }

        public bool TryGetPredictedStrikePoint(float projectedDodgeDistance, out Vector3 point)
        {
            point = target != null ? target.transform.position : transform.position;
            if (!prototypeActive || target == null)
            {
                return false;
            }

            KaelPredictionSnapshot snapshot = Snapshot;
            if (!snapshot.Locked || snapshot.Direction == DodgeDirection.None)
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
            }
        }
    }
}
