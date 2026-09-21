using System;
using UnityEngine;

namespace MUSCA.Gate3D
{
    public readonly struct KaelPredictionSnapshot
    {
        public DodgeDirection Direction { get; }
        public int SampleCount { get; }
        public float Confidence { get; }
        public bool Locked { get; }

        public KaelPredictionSnapshot(DodgeDirection direction, int sampleCount, float confidence, bool locked)
        {
            Direction = direction;
            SampleCount = sampleCount;
            Confidence = confidence;
            Locked = locked;
        }
    }

    public sealed class KaelPredictionModel
    {
        private readonly DodgeDirection[] _history;
        private readonly int _minimumSamples;
        private readonly float _lockConfidence;
        private int _count;
        private int _next;

        public int Capacity => _history.Length;
        public int Count => _count;

        public KaelPredictionModel(int capacity = 7, int minimumSamples = 4, float lockConfidence = 0.6f)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (minimumSamples < 1 || minimumSamples > capacity) throw new ArgumentOutOfRangeException(nameof(minimumSamples));
            if (lockConfidence <= 0f || lockConfidence > 1f) throw new ArgumentOutOfRangeException(nameof(lockConfidence));

            _history = new DodgeDirection[capacity];
            _minimumSamples = minimumSamples;
            _lockConfidence = lockConfidence;
        }

        public void Record(DodgeDirection direction)
        {
            if (direction == DodgeDirection.None) return;

            _history[_next] = direction;
            _next = (_next + 1) % _history.Length;
            if (_count < _history.Length) _count++;
        }

        public void Clear()
        {
            Array.Clear(_history, 0, _history.Length);
            _count = 0;
            _next = 0;
        }

        public KaelPredictionSnapshot Snapshot()
        {
            if (_count == 0)
            {
                return new KaelPredictionSnapshot(DodgeDirection.None, 0, 0f, false);
            }

            int forward = 0;
            int backward = 0;
            int left = 0;
            int right = 0;
            for (int i = 0; i < _count; i++)
            {
                switch (_history[i])
                {
                    case DodgeDirection.Forward: forward++; break;
                    case DodgeDirection.Backward: backward++; break;
                    case DodgeDirection.Left: left++; break;
                    case DodgeDirection.Right: right++; break;
                }
            }

            int maximum = Math.Max(Math.Max(forward, backward), Math.Max(left, right));
            DodgeDirection direction = MostRecentDirectionWithCount(maximum, forward, backward, left, right);
            float confidence = maximum / (float)_count;
            bool locked = _count >= _minimumSamples && confidence >= _lockConfidence;
            return new KaelPredictionSnapshot(direction, _count, confidence, locked);
        }

        private DodgeDirection MostRecentDirectionWithCount(
            int maximum, int forward, int backward, int left, int right)
        {
            for (int offset = 1; offset <= _count; offset++)
            {
                int index = (_next - offset + _history.Length) % _history.Length;
                DodgeDirection candidate = _history[index];
                int count = candidate switch
                {
                    DodgeDirection.Forward => forward,
                    DodgeDirection.Backward => backward,
                    DodgeDirection.Left => left,
                    DodgeDirection.Right => right,
                    _ => 0
                };
                if (count == maximum) return candidate;
            }

            return DodgeDirection.None;
        }

        public static Vector3 ResolveWorldDirection(
            DodgeDirection direction, Vector3 forward, Vector3 right)
        {
            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            return direction switch
            {
                DodgeDirection.Forward => forward,
                DodgeDirection.Backward => -forward,
                DodgeDirection.Left => -right,
                DodgeDirection.Right => right,
                _ => Vector3.zero
            };
        }
    }
}
