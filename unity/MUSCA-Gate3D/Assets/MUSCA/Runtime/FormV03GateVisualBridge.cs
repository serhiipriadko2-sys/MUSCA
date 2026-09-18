using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class FormV03GateVisualBridge : MonoBehaviour
    {
        [SerializeField] private Transform functionLeft;
        [SerializeField] private Transform functionRight;
        [SerializeField] private Transform visualLeft;
        [SerializeField] private Transform visualRight;

        private Vector3 _functionLeftClosed;
        private Vector3 _functionRightClosed;
        private Vector3 _visualLeftClosed;
        private Vector3 _visualRightClosed;
        private bool _cached;

        public bool IsConfigured => functionLeft != null && functionRight != null && visualLeft != null && visualRight != null;

        public void Configure(Transform leftFunction, Transform rightFunction, Transform leftVisual, Transform rightVisual)
        {
            functionLeft = leftFunction;
            functionRight = rightFunction;
            visualLeft = leftVisual;
            visualRight = rightVisual;
            CacheClosedState();
        }

        private void Awake()
        {
            CacheClosedState();
        }

        private void LateUpdate()
        {
            if (!_cached || !IsConfigured)
            {
                return;
            }

            visualLeft.position = _visualLeftClosed + (functionLeft.position - _functionLeftClosed);
            visualRight.position = _visualRightClosed + (functionRight.position - _functionRightClosed);
        }

        private void CacheClosedState()
        {
            if (!IsConfigured)
            {
                _cached = false;
                return;
            }

            _functionLeftClosed = functionLeft.position;
            _functionRightClosed = functionRight.position;
            _visualLeftClosed = visualLeft.position;
            _visualRightClosed = visualRight.position;
            _cached = true;
        }
    }
}
