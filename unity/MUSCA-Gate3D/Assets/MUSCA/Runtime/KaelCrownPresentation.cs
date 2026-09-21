using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class KaelCrownPresentation : MonoBehaviour
    {
        [SerializeField] private KaelPredictionProbe prediction;
        [SerializeField] private Transform ringA;
        [SerializeField] private Transform ringB;
        [SerializeField] private Transform ringC;

        private LineRenderer _lineA;
        private LineRenderer _lineB;
        private LineRenderer _lineC;

        public void Configure(
            KaelPredictionProbe probe,
            Transform first,
            Transform second,
            Transform third)
        {
            prediction = probe;
            ringA = first;
            ringB = second;
            ringC = third;
            CacheLines();
        }

        private void Awake()
        {
            CacheLines();
        }

        private void Update()
        {
            if (prediction == null) return;

            KaelPredictionSnapshot snapshot = prediction.Snapshot;
            float confidence = prediction.PrototypeActive ? snapshot.Confidence : 0f;
            float speed = Mathf.Lerp(12f, 75f, confidence);
            if (snapshot.Locked) speed *= 1.35f;

            if (ringA != null) ringA.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
            if (ringB != null) ringB.Rotate(Vector3.right, speed * 0.72f * Time.deltaTime, Space.Self);
            if (ringC != null) ringC.Rotate(Vector3.forward, -speed * 0.55f * Time.deltaTime, Space.Self);

            Color color = snapshot.Locked
                ? new Color(1f, 0.42f, 0.08f, 0.95f)
                : new Color(0.10f, 0.78f, 1f, prediction.PrototypeActive ? 0.82f : 0.38f);
            ApplyColor(_lineA, color);
            ApplyColor(_lineB, color);
            ApplyColor(_lineC, color);
        }

        private void CacheLines()
        {
            _lineA = ringA != null ? ringA.GetComponent<LineRenderer>() : null;
            _lineB = ringB != null ? ringB.GetComponent<LineRenderer>() : null;
            _lineC = ringC != null ? ringC.GetComponent<LineRenderer>() : null;
        }

        private static void ApplyColor(LineRenderer line, Color color)
        {
            if (line == null) return;
            line.startColor = color;
            line.endColor = color;
        }
    }
}
