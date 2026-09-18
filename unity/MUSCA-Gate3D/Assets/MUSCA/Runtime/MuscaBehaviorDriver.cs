using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class MuscaBehaviorDriver : MonoBehaviour
    {
        [SerializeField] private GateRuntime runtime;
        [SerializeField] private MuscaCompanion companion;
        [SerializeField] private float scanReactionSeconds = 1.6f;

        private bool _subscribed;
        private bool _lastScanned;
        private float _scanUntil;

        public MuscaBehaviorState CurrentState => companion != null
            ? companion.BehaviorState
            : MuscaBehaviorState.Calm;

        public void Configure(GateRuntime gateRuntime, MuscaCompanion musca)
        {
            Unsubscribe();
            runtime = gateRuntime;
            companion = musca;
            GateSnapshot snapshot = runtime != null ? runtime.Snapshot() : null;
            _lastScanned = snapshot != null && snapshot.Scanned;
            Subscribe();
            Evaluate();
        }

        private void OnEnable()
        {
            Subscribe();
            Evaluate();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_scanUntil > 0f && Time.time >= _scanUntil)
            {
                _scanUntil = 0f;
                Evaluate();
            }
        }

        private void OnRuntimeStateChanged()
        {
            if (runtime == null) return;
            GateSnapshot snapshot = runtime.Snapshot();
            if (!_lastScanned && snapshot.Scanned)
            {
                _scanUntil = Time.time + scanReactionSeconds;
            }
            _lastScanned = snapshot.Scanned;
            Evaluate();
        }

        private void Evaluate()
        {
            if (runtime == null || companion == null) return;
            GateSnapshot snapshot = runtime.Snapshot();
            if (snapshot == null)
            {
                companion.SetAttentionTarget(null);
                companion.SetBehaviorState(MuscaBehaviorState.Calm);
                return;
            }
            MuscaBehaviorState state;
            Transform attention = runtime.ActiveStation != null ? runtime.ActiveStation.transform : null;

            if (snapshot.Outcome == GateOutcome.Opened)
            {
                state = MuscaBehaviorState.Celebrate;
                attention = null;
            }
            else if (snapshot.Outcome == GateOutcome.Sealed)
            {
                state = MuscaBehaviorState.Alert;
                attention = null;
            }
            else if (_scanUntil > Time.time)
            {
                state = MuscaBehaviorState.Scanning;
            }
            else if (runtime.ActiveStation != null)
            {
                state = MuscaBehaviorState.Curious;
            }
            else
            {
                state = MuscaBehaviorState.Calm;
            }

            companion.SetAttentionTarget(attention);
            companion.SetBehaviorState(state);
        }

        private void Subscribe()
        {
            if (_subscribed || runtime == null) return;
            runtime.StateChanged += OnRuntimeStateChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || runtime == null) return;
            runtime.StateChanged -= OnRuntimeStateChanged;
            _subscribed = false;
        }
    }
}
