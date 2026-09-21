using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class SentinelPresentation : MonoBehaviour
    {
        [SerializeField] private SentinelCombatBrain brain;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform spearRoot;

        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftThigh;
        private Transform _rightThigh;
        private Transform _torso;
        private Transform _head;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftThighBase;
        private Quaternion _rightThighBase;
        private Quaternion _torsoBase;
        private Quaternion _headBase;
        private Vector3 _visualBasePosition;
        private Vector3 _spearBasePosition;
        private Quaternion _spearBaseRotation;
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
            _leftArm = FindDeep(visualRoot, "SV01_UpperArm_-1");
            _rightArm = FindDeep(visualRoot, "SV01_UpperArm_1");
            _leftThigh = FindDeep(visualRoot, "SV01_Thigh_-1");
            _rightThigh = FindDeep(visualRoot, "SV01_Thigh_1");
            _torso = FindDeep(visualRoot, "SV01_Torso");
            _head = FindDeep(visualRoot, "SV01_Head");
        }

        private void CapturePose()
        {
            if (visualRoot != null) _visualBasePosition = visualRoot.localPosition;
            if (_leftArm != null) _leftArmBase = _leftArm.localRotation;
            if (_rightArm != null) _rightArmBase = _rightArm.localRotation;
            if (_leftThigh != null) _leftThighBase = _leftThigh.localRotation;
            if (_rightThigh != null) _rightThighBase = _rightThigh.localRotation;
            if (_torso != null) _torsoBase = _torso.localRotation;
            if (_head != null) _headBase = _head.localRotation;
            if (spearRoot != null)
            {
                _spearBasePosition = spearRoot.localPosition;
                _spearBaseRotation = spearRoot.localRotation;
            }
        }

        private void LateUpdate()
        {
            if (brain == null || visualRoot == null) return;

            _phase += Time.deltaTime * (brain.State == SentinelCombatState.Approach ? 7f : 2f);
            float sine = Mathf.Sin(_phase);
            float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);

            float leftArmX = 0f;
            float rightArmX = 0f;
            float leftThighX = 0f;
            float rightThighX = 0f;
            float torsoX = 0f;
            float torsoZ = 0f;
            float headY = sine * 2f;
            float bob = Mathf.Abs(sine) * 0.008f;
            Vector3 spearOffset = Vector3.zero;
            Vector3 spearEuler = Vector3.zero;

            switch (brain.State)
            {
                case SentinelCombatState.Approach:
                    float walk = sine * 24f;
                    leftArmX = walk;
                    rightArmX = -walk;
                    leftThighX = -walk * 0.72f;
                    rightThighX = walk * 0.72f;
                    torsoZ = sine * 2.5f;
                    bob = Mathf.Abs(sine) * 0.025f;
                    break;

                case SentinelCombatState.Telegraph:
                    rightArmX = -68f;
                    leftArmX = 18f;
                    leftThighX = 10f;
                    rightThighX = -6f;
                    torsoX = -10f;
                    spearOffset = new Vector3(0f, 0.10f, -0.08f);
                    spearEuler = new Vector3(-58f, 0f, 0f);
                    headY = 0f;
                    break;

                case SentinelCombatState.Recovery:
                    rightArmX = 48f;
                    leftArmX = -12f;
                    leftThighX = -8f;
                    rightThighX = 12f;
                    torsoX = 12f;
                    spearOffset = new Vector3(0f, -0.05f, 0.16f);
                    spearEuler = new Vector3(52f, 0f, 0f);
                    headY = 0f;
                    break;
            }

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition, _visualBasePosition + Vector3.up * bob, blend);
            Apply(_leftArm, _leftArmBase, new Vector3(leftArmX, 0f, 0f), blend);
            Apply(_rightArm, _rightArmBase, new Vector3(rightArmX, 0f, 0f), blend);
            Apply(_leftThigh, _leftThighBase, new Vector3(leftThighX, 0f, 0f), blend);
            Apply(_rightThigh, _rightThighBase, new Vector3(rightThighX, 0f, 0f), blend);
            Apply(_torso, _torsoBase, new Vector3(torsoX, 0f, torsoZ), blend);
            Apply(_head, _headBase, new Vector3(0f, headY, 0f), blend);

            if (spearRoot != null)
            {
                spearRoot.localPosition = Vector3.Lerp(
                    spearRoot.localPosition, _spearBasePosition + spearOffset, blend);
                Quaternion desired = _spearBaseRotation * Quaternion.Euler(spearEuler);
                spearRoot.localRotation = Quaternion.Slerp(
                    spearRoot.localRotation, desired, blend);
            }
        }

        private static void Apply(
            Transform target, Quaternion basis, Vector3 euler, float blend)
        {
            if (target == null) return;
            Quaternion desired = basis * Quaternion.Euler(euler);
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, blend);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
