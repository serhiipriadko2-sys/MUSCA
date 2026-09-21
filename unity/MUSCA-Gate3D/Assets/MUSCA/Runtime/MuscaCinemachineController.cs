using Unity.Cinemachine;
using UnityEngine;

namespace MUSCA.Gate3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FirstPersonController))]
    [RequireComponent(typeof(PlayerLockOn))]
    public sealed class MuscaCinemachineController : MonoBehaviour
    {
        [SerializeField] private Camera outputCamera;
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineCamera freeCamera;
        [SerializeField] private CinemachineCamera lockCamera;
        [SerializeField] private Transform freePivot;
        [SerializeField] private Transform lockPivot;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private PlayerLockOn lockOn;

        [Header("Free orbit")]
        [SerializeField] private float freeMouseSensitivity = 2.0f;
        [SerializeField] private float freePitchMin = -12f;
        [SerializeField] private float freePitchMax = 42f;
        [SerializeField] private float initialPitch = 13f;

        [Header("Lock orbit")]
        [SerializeField] private float lockPitch = 11f;
        [SerializeField] private float lockYawSmoothTime = 0.08f;
        [SerializeField] private float lockYawMaxSpeed = 520f;

        private float _freeYaw;
        private float _freePitch;
        private float _lockYaw;
        private float _lockYawVelocity;
        private bool _initialized;
        private bool _wasLocked;

        public bool IsUsingLockCamera =>
            lockOn != null && lockOn.IsLocked &&
            lockCamera != null && lockCamera.enabled;

        public float MovementYaw =>
            lockOn != null && lockOn.IsLocked ? _lockYaw : _freeYaw;

        public void Configure(
            Camera cameraValue,
            CinemachineBrain brainValue,
            CinemachineCamera freeCameraValue,
            CinemachineCamera lockCameraValue,
            Transform freePivotValue,
            Transform lockPivotValue,
            FirstPersonController movementValue,
            PlayerLockOn lockOnValue)
        {
            outputCamera = cameraValue;
            brain = brainValue;
            freeCamera = freeCameraValue;
            lockCamera = lockCameraValue;
            freePivot = freePivotValue;
            lockPivot = lockPivotValue;
            movement = movementValue;
            lockOn = lockOnValue;
            InitializeState();
        }

        private void Awake()
        {
            movement ??= GetComponent<FirstPersonController>();
            lockOn ??= GetComponent<PlayerLockOn>();
            if (outputCamera == null && movement != null)
                outputCamera = movement.PlayerCamera;
            if (brain == null && outputCamera != null)
                brain = outputCamera.GetComponent<CinemachineBrain>();
            InitializeState();
        }

        private void OnEnable()
        {
            if (movement != null)
                movement.SetExternalCameraDriver(true);
        }

        private void OnDisable()
        {
            if (movement != null)
                movement.SetExternalCameraDriver(false);
        }

        private void InitializeState()
        {
            if (_initialized) return;
            float yaw = outputCamera != null
                ? outputCamera.transform.eulerAngles.y
                : transform.eulerAngles.y;
            _freeYaw = yaw;
            _lockYaw = yaw;
            _freePitch = Mathf.Clamp(initialPitch, freePitchMin, freePitchMax);
            _initialized = true;
        }

        private void Update()
        {
            InitializeState();
            if (movement == null || freePivot == null || lockPivot == null)
                return;

            Vector3 playerPosition = transform.position;
            freePivot.position = playerPosition;
            lockPivot.position = playerPosition;

            bool locked = lockOn != null && lockOn.IsLocked && lockOn.Target != null;
            HandleModeTransition(locked);

            if (locked)
            {
                UpdateLockPivot(lockOn.Target.transform);
            }
            else
            {
                UpdateFreePivot();
            }

            SetCameraMode(locked);
        }

        private void UpdateFreePivot()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _freeYaw += Input.GetAxisRaw("Mouse X") * freeMouseSensitivity;
                _freePitch = Mathf.Clamp(
                    _freePitch - Input.GetAxisRaw("Mouse Y") * freeMouseSensitivity,
                    freePitchMin,
                    freePitchMax);
            }

            freePivot.rotation = Quaternion.Euler(_freePitch, _freeYaw, 0f);
            _lockYaw = _freeYaw;
            _lockYawVelocity = 0f;
        }

        private void UpdateLockPivot(Transform target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                _lockYaw = Mathf.SmoothDampAngle(
                    _lockYaw,
                    desiredYaw,
                    ref _lockYawVelocity,
                    Mathf.Max(0.0001f, lockYawSmoothTime),
                    Mathf.Max(1f, lockYawMaxSpeed),
                    Time.deltaTime);
            }

            lockPivot.rotation = Quaternion.Euler(lockPitch, _lockYaw, 0f);
            if (lockCamera != null)
            {
                lockCamera.LookAt = target;
            }
        }

        private void HandleModeTransition(bool locked)
        {
            if (locked == _wasLocked)
                return;

            if (locked)
            {
                _lockYaw = _freeYaw;
                _lockYawVelocity = 0f;
            }
            else
            {
                // Resume free orbit from the last lock heading exactly once.
                // Never feed the Cinemachine output yaw back into the pivot each
                // frame: that creates a positive feedback loop and continuous spin.
                _freeYaw = _lockYaw;
                _lockYawVelocity = 0f;
            }

            _wasLocked = locked;
        }

        private void SetCameraMode(bool locked)
        {
            if (freeCamera != null)
                freeCamera.enabled = !locked;
            if (lockCamera != null)
                lockCamera.enabled = locked;
        }
    }
}
