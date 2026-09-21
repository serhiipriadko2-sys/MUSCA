using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class SentinelPresentation : MonoBehaviour
    {
        [SerializeField] private SentinelCombatBrain brain;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform spearRoot;

        private Transform _leftShoulder;
        private Transform _rightShoulder;
        private Transform _leftElbow;
        private Transform _rightElbow;
        private Transform _leftHip;
        private Transform _rightHip;
        private Transform _leftKnee;
        private Transform _rightKnee;
        private Transform _pelvis;
        private Transform _leftAnkle;
        private Transform _rightAnkle;
        private Transform _torso;
        private Transform _head;

        private Quaternion _leftShoulderBase;
        private Quaternion _rightShoulderBase;
        private Quaternion _leftElbowBase;
        private Quaternion _rightElbowBase;
        private Quaternion _leftHipBase;
        private Quaternion _rightHipBase;
        private Quaternion _leftKneeBase;
        private Quaternion _rightKneeBase;
        private Quaternion _pelvisBase;
        private Quaternion _leftAnkleBase;
        private Quaternion _rightAnkleBase;
        private Quaternion _torsoBase;
        private Quaternion _headBase;
        private Vector3 _visualBasePosition;
        private Vector3 _spearBasePosition;
        private Quaternion _spearBaseRotation;
        private Vector3 _lastWorldPosition;
        private bool _positionInitialized;
        private float _phase;

        public void Configure(
            SentinelCombatBrain brainValue,
            Transform visualValue,
            Transform spearValue)
        {
            brain = brainValue;
            visualRoot = visualValue;
            spearRoot = spearValue;
            ResolveParts();
            CapturePose();
        }

        private void Awake()
        {
            if (brain == null) brain = GetComponent<SentinelCombatBrain>();
            if (visualRoot == null) visualRoot = transform.Find("SentinelVisual");
            ResolveParts();
            CapturePose();
        }

        private void ResolveParts()
        {
            if (visualRoot == null) return;

            _leftShoulder = Prefer(
                "SV04_Shoulder_L", "SV01_UpperArm_-1");
            _rightShoulder = Prefer(
                "SV04_Shoulder_R", "SV01_UpperArm_1");
            _leftElbow = FindDeep(visualRoot, "SV04_Elbow_L");
            _rightElbow = FindDeep(visualRoot, "SV04_Elbow_R");
            _leftHip = Prefer(
                "SV04_Hip_L", "SV01_Thigh_-1");
            _rightHip = Prefer(
                "SV04_Hip_R", "SV01_Thigh_1");
            _leftKnee = Prefer(
                "SV04_Knee_L", "SV01_Shin_-1");
            _rightKnee = Prefer(
                "SV04_Knee_R", "SV01_Shin_1");
            _pelvis = FindDeep(visualRoot, "SV04_PelvisPivot");
            _leftAnkle = FindDeep(visualRoot, "SV04_Ankle_L");
            _rightAnkle = FindDeep(visualRoot, "SV04_Ankle_R");
            _torso = Prefer(
                "SV04_TorsoPivot", "SV01_Torso");
            _head = Prefer(
                "SV04_HeadPivot", "SV01_Head");
        }

        private Transform Prefer(string primary, string fallback)
        {
            Transform value = FindDeep(visualRoot, primary);
            return value != null ? value : FindDeep(visualRoot, fallback);
        }

        private void CapturePose()
        {
            if (visualRoot != null)
            {
                _visualBasePosition = visualRoot.localPosition;
            }
            if (_leftShoulder != null)
                _leftShoulderBase = _leftShoulder.localRotation;
            if (_rightShoulder != null)
                _rightShoulderBase = _rightShoulder.localRotation;
            if (_leftElbow != null)
                _leftElbowBase = _leftElbow.localRotation;
            if (_rightElbow != null)
                _rightElbowBase = _rightElbow.localRotation;
            if (_leftHip != null)
                _leftHipBase = _leftHip.localRotation;
            if (_rightHip != null)
                _rightHipBase = _rightHip.localRotation;
            if (_leftKnee != null)
                _leftKneeBase = _leftKnee.localRotation;
            if (_rightKnee != null)
                _rightKneeBase = _rightKnee.localRotation;
            if (_pelvis != null) _pelvisBase = _pelvis.localRotation;
            if (_leftAnkle != null) _leftAnkleBase = _leftAnkle.localRotation;
            if (_rightAnkle != null) _rightAnkleBase = _rightAnkle.localRotation;
            if (_torso != null) _torsoBase = _torso.localRotation;
            if (_head != null) _headBase = _head.localRotation;
            if (spearRoot != null)
            {
                _spearBasePosition = spearRoot.localPosition;
                _spearBaseRotation = spearRoot.localRotation;
            }
            _lastWorldPosition = transform.position;
            _positionInitialized = true;
        }

        private void LateUpdate()
        {
            if (brain == null || visualRoot == null) return;

            Vector3 worldDelta = _positionInitialized
                ? transform.position - _lastWorldPosition
                : Vector3.zero;
            worldDelta.y = 0f;
            float traveled = worldDelta.magnitude;
            _lastWorldPosition = transform.position;
            _positionInitialized = true;

            if (brain.State == SentinelCombatState.Approach)
            {
                _phase += traveled * 3.55f;
            }
            else
            {
                _phase += Time.deltaTime * 1.15f;
            }

            float cycle = Mathf.Sin(_phase);
            float blend = 1f - Mathf.Exp(-18f * Time.deltaTime);

            float leftShoulderX = 0f;
            float rightShoulderX = 0f;
            float leftElbowX = 10f;
            float rightElbowX = 10f;
            float leftHipX = 0f;
            float rightHipX = 0f;
            float leftKneeX = 0f;
            float rightKneeX = 0f;
            Vector3 pelvisEuler = Vector3.zero;
            Vector3 leftAnkleEuler = Vector3.zero;
            Vector3 rightAnkleEuler = Vector3.zero;
            Vector3 torsoEuler = Vector3.zero;
            Vector3 headEuler = new Vector3(0f, cycle * 1.2f, 0f);
            float bob = 0f;
            Vector3 spearOffset = Vector3.zero;
            Vector3 spearEuler = Vector3.zero;

            switch (brain.State)
            {
                case SentinelCombatState.Approach:
                {
                    float stride = cycle;
                    leftShoulderX = stride * 26f;
                    rightShoulderX = -stride * 26f;
                    leftElbowX = 12f + Mathf.Max(0f, -stride) * 20f;
                    rightElbowX = 12f + Mathf.Max(0f, stride) * 20f;
                    leftHipX = -stride * 33f;
                    rightHipX = stride * 33f;
                    leftKneeX = Mathf.Max(0f, stride) * 40f;
                    rightKneeX = Mathf.Max(0f, -stride) * 40f;
                    pelvisEuler = new Vector3(
                        0f, stride * 4.5f, stride * 2.5f);
                    leftAnkleEuler = new Vector3(
                        Mathf.Clamp(
                            -leftHipX * 0.24f - leftKneeX * 0.44f,
                            -20f,
                            16f),
                        0f,
                        0f);
                    rightAnkleEuler = new Vector3(
                        Mathf.Clamp(
                            -rightHipX * 0.24f - rightKneeX * 0.44f,
                            -20f,
                            16f),
                        0f,
                        0f);
                    torsoEuler = new Vector3(
                        3.5f, -stride * 7f, -stride * 3.5f);
                    headEuler = new Vector3(
                        -1.5f, stride * 2.5f, 0f);
                    bob = Mathf.Abs(stride) * 0.045f;
                    break;
                }

                case SentinelCombatState.Telegraph:
                {
                    // Reach the committed wind-up early, then hold it long enough
                    // for the player to read the body rather than only the floor.
                    float t = Smooth01(Mathf.Clamp01(
                        brain.StateProgress / 0.72f));
                    float compression = Mathf.Sin(t * Mathf.PI * 0.5f);

                    rightShoulderX = Mathf.Lerp(0f, -116f, t);
                    rightElbowX = Mathf.Lerp(10f, 62f, t);
                    leftShoulderX = Mathf.Lerp(0f, 34f, t);
                    leftElbowX = Mathf.Lerp(10f, 36f, t);
                    leftHipX = Mathf.Lerp(0f, 24f, t);
                    rightHipX = Mathf.Lerp(0f, -18f, t);
                    leftKneeX = Mathf.Lerp(0f, 24f, t);
                    rightKneeX = Mathf.Lerp(0f, 36f, t);
                    pelvisEuler = new Vector3(
                        Mathf.Lerp(0f, -8f, t),
                        Mathf.Lerp(0f, 20f, t),
                        Mathf.Lerp(0f, -9f, t));
                    leftAnkleEuler = new Vector3(
                        Mathf.Lerp(0f, -8f, t), 0f, 5f * t);
                    rightAnkleEuler = new Vector3(
                        Mathf.Lerp(0f, 10f, t), 0f, -7f * t);
                    torsoEuler = new Vector3(
                        Mathf.Lerp(0f, -20f, t),
                        Mathf.Lerp(0f, -34f, t),
                        Mathf.Lerp(0f, 12f, t));
                    headEuler = new Vector3(
                        Mathf.Lerp(0f, 6f, t),
                        Mathf.Lerp(0f, 17f, t),
                        Mathf.Lerp(0f, -4f, t));
                    bob = -0.075f * compression;
                    spearOffset = new Vector3(
                        0.05f * t, 0.24f * t, -0.23f * t);
                    spearEuler = new Vector3(
                        Mathf.Lerp(0f, -84f, t),
                        Mathf.Lerp(0f, -19f, t),
                        Mathf.Lerp(0f, 11f, t));
                    break;
                }

                case SentinelCombatState.Recovery:
                {
                    float t = Smooth01(brain.StateProgress);
                    float inverse = 1f - t;
                    rightShoulderX = 82f * inverse;
                    rightElbowX = 44f * inverse + 10f;
                    leftShoulderX = -24f * inverse;
                    leftElbowX = 28f * inverse + 10f;
                    leftHipX = -16f * inverse;
                    rightHipX = 18f * inverse;
                    leftKneeX = 16f * inverse;
                    rightKneeX = 12f * inverse;
                    pelvisEuler = new Vector3(
                        7f * inverse,
                        -17f * inverse,
                        7f * inverse);
                    leftAnkleEuler = new Vector3(
                        7f * inverse, 0f, -4f * inverse);
                    rightAnkleEuler = new Vector3(
                        -9f * inverse, 0f, 5f * inverse);
                    torsoEuler = new Vector3(
                        21f * inverse,
                        29f * inverse,
                        -11f * inverse);
                    headEuler = new Vector3(
                        -6f * inverse,
                        -11f * inverse,
                        0f);
                    spearOffset = new Vector3(
                        0f, -0.11f * inverse, 0.28f * inverse);
                    spearEuler = new Vector3(
                        68f * inverse,
                        14f * inverse,
                        -8f * inverse);
                    break;
                }
            }

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                _visualBasePosition + Vector3.up * bob,
                blend);

            Apply(
                _leftShoulder,
                _leftShoulderBase,
                new Vector3(leftShoulderX, 0f, 0f),
                blend);
            Apply(
                _rightShoulder,
                _rightShoulderBase,
                new Vector3(rightShoulderX, 0f, 0f),
                blend);
            Apply(
                _leftElbow,
                _leftElbowBase,
                new Vector3(leftElbowX, 0f, 0f),
                blend);
            Apply(
                _rightElbow,
                _rightElbowBase,
                new Vector3(rightElbowX, 0f, 0f),
                blend);
            Apply(
                _leftHip,
                _leftHipBase,
                new Vector3(leftHipX, 0f, 0f),
                blend);
            Apply(
                _rightHip,
                _rightHipBase,
                new Vector3(rightHipX, 0f, 0f),
                blend);
            Apply(
                _leftKnee,
                _leftKneeBase,
                new Vector3(leftKneeX, 0f, 0f),
                blend);
            Apply(
                _rightKnee,
                _rightKneeBase,
                new Vector3(rightKneeX, 0f, 0f),
                blend);
            Apply(_pelvis, _pelvisBase, pelvisEuler, blend);
            Apply(_leftAnkle, _leftAnkleBase, leftAnkleEuler, blend);
            Apply(_rightAnkle, _rightAnkleBase, rightAnkleEuler, blend);
            Apply(_torso, _torsoBase, torsoEuler, blend);
            Apply(_head, _headBase, headEuler, blend);

            if (spearRoot != null)
            {
                spearRoot.localPosition = Vector3.Lerp(
                    spearRoot.localPosition,
                    _spearBasePosition + spearOffset,
                    blend);
                Quaternion desired =
                    _spearBaseRotation * Quaternion.Euler(spearEuler);
                spearRoot.localRotation = Quaternion.Slerp(
                    spearRoot.localRotation, desired, blend);
            }
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static void Apply(
            Transform target, Quaternion basis, Vector3 euler, float blend)
        {
            if (target == null) return;
            Quaternion desired = basis * Quaternion.Euler(euler);
            target.localRotation = Quaternion.Slerp(
                target.localRotation, desired, blend);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
