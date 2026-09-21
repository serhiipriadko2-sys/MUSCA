using UnityEngine;

namespace MUSCA.Gate3D
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float moveAcceleration = 26f;
        [SerializeField] private float moveDeceleration = 34f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float lockTurnSpeedDegrees = 720f;
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 1.68f, 0f);
        [SerializeField] private bool cameraCollisionEnabled;
        [SerializeField] private float cameraCollisionRadius = 0.22f;
        [SerializeField] private float cameraCollisionPadding = 0.08f;
        [SerializeField] private LayerMask cameraCollisionMask = ~0;

        private CharacterController _controller;
        private float _pitch;
        private CollisionFlags _lastMoveCollisionFlags;
        private Vector3 _planarVelocity;
        private Vector3 _dodgeVelocity;
        private float _dodgeRemainingSeconds;
        private float _dodgeInvulnerableUntil = -1f;
        private Transform _lockOnTarget;
        private readonly RaycastHit[] _cameraHits = new RaycastHit[16];

        public bool InputEnabled { get; set; } = true;
        public Camera PlayerCamera => playerCamera;
        public Vector3 CameraLocalPosition => cameraLocalPosition;
        public CollisionFlags LastMoveCollisionFlags => _lastMoveCollisionFlags;
        public bool LastMoveGrounded => (_lastMoveCollisionFlags & CollisionFlags.Below) != 0;
        public bool IsDodging => _dodgeRemainingSeconds > 0.0001f;
        public bool IsDodgeInvulnerable => IsDodging && Time.time < _dodgeInvulnerableUntil;
        public bool CanStartDodge => InputEnabled && _controller != null && _controller.enabled && !IsDodging;
        public float PlanarSpeed => _planarVelocity.magnitude;
        public Transform LockOnTarget => _lockOnTarget;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
            ApplyCameraPosition();
        }

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
                else LockCursor();
            }

            if (!InputEnabled)
            {
                return;
            }

            // Once a dodge has been accepted, finish the committed movement even if
            // the cursor becomes unlocked (for example on a focus change). Starting
            // a new dodge still requires the normal gameplay input gate.
            if (IsDodging)
            {
                float committedSeconds = ComputeCommittedStepSeconds(_dodgeRemainingSeconds, Time.deltaTime);
                Vector3 dodgeDisplacement = _dodgeVelocity * committedSeconds;
                dodgeDisplacement.y = -2f * Time.deltaTime;
                _lastMoveCollisionFlags = _controller.Move(dodgeDisplacement);
                _dodgeRemainingSeconds = Mathf.Max(0f, _dodgeRemainingSeconds - committedSeconds);
                if (!IsDodging) _dodgeVelocity = Vector3.zero;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            UpdateView();

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 desiredVelocity = transform.TransformDirection(input) * moveSpeed;
            desiredVelocity.y = 0f;

            float rate = input.sqrMagnitude > 0.0001f ? moveAcceleration : moveDeceleration;
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity, desiredVelocity, Mathf.Max(0f, rate) * Time.deltaTime);

            Vector3 velocity = _planarVelocity;
            // Keep a small downward bias every frame so CharacterController
            // maintains stable contact with the floor instead of flapping
            // between grounded/not-grounded when planar input is idle.
            velocity.y = -2f;
            _lastMoveCollisionFlags = _controller.Move(velocity * Time.deltaTime);
        }

        private void LateUpdate()
        {
            UpdateCameraCollision();
        }

        private void UpdateView()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            if (_lockOnTarget != null)
            {
                Vector3 toTarget = _lockOnTarget.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, desired, lockTurnSpeedDegrees * Time.deltaTime);
                }
            }
            else
            {
                transform.Rotate(Vector3.up, mouseX, Space.World);
            }

            _pitch = Mathf.Clamp(_pitch - mouseY, -80f, 80f);
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        public bool TryStartDodge(
            Vector3 worldDirection,
            float speed,
            float durationSeconds,
            float invulnerableSeconds)
        {
            if (!CanStartDodge || durationSeconds <= 0f || speed <= 0f)
            {
                return false;
            }

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                worldDirection = -transform.forward;
                worldDirection.y = 0f;
            }

            _planarVelocity = Vector3.zero;
            _dodgeVelocity = worldDirection.normalized * speed;
            _dodgeRemainingSeconds = durationSeconds;
            _dodgeInvulnerableUntil = Time.time + Mathf.Clamp(invulnerableSeconds, 0f, durationSeconds);
            return true;
        }

        public void CancelDodge()
        {
            _dodgeRemainingSeconds = 0f;
            _dodgeInvulnerableUntil = -1f;
            _dodgeVelocity = Vector3.zero;
        }

        public static float ComputeCommittedStepSeconds(float remainingSeconds, float deltaTime)
        {
            return Mathf.Min(Mathf.Max(0f, remainingSeconds), Mathf.Max(0f, deltaTime));
        }

        public void SetLockOnTarget(Transform target)
        {
            _lockOnTarget = target;
        }

        public void ConfigureCamera(Camera value, Vector3 localPosition, float fieldOfView)
        {
            playerCamera = value;
            cameraLocalPosition = localPosition;
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = fieldOfView;
                ApplyCameraPosition();
            }
        }

        public void SetCameraCollision(bool enabled, float radius = 0.22f, float padding = 0.08f)
        {
            cameraCollisionEnabled = enabled;
            cameraCollisionRadius = Mathf.Max(0.05f, radius);
            cameraCollisionPadding = Mathf.Max(0f, padding);
            ApplyCameraPosition();
        }

        public void Teleport(Vector3 position, float yawDegrees)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            _pitch = 0f;
            _planarVelocity = Vector3.zero;
            CancelDodge();
            ApplyCameraPosition();
            if (playerCamera != null) playerCamera.transform.localRotation = Quaternion.identity;
            _controller.enabled = true;
        }

        private void ApplyCameraPosition()
        {
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = cameraLocalPosition;
            }
        }

        private void UpdateCameraCollision()
        {
            if (playerCamera == null || !cameraCollisionEnabled)
            {
                return;
            }

            Vector3 pivotLocal = new Vector3(cameraLocalPosition.x, cameraLocalPosition.y, 0f);
            Vector3 pivotWorld = transform.TransformPoint(pivotLocal);
            Vector3 desiredWorld = transform.TransformPoint(cameraLocalPosition);
            Vector3 delta = desiredWorld - pivotWorld;
            float desiredDistance = delta.magnitude;
            if (desiredDistance < 0.001f)
            {
                playerCamera.transform.position = desiredWorld;
                return;
            }

            Vector3 direction = delta / desiredDistance;
            float allowedDistance = desiredDistance;
            int count = Physics.SphereCastNonAlloc(pivotWorld, cameraCollisionRadius, direction, _cameraHits,
                desiredDistance, cameraCollisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = _cameraHits[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }
                allowedDistance = Mathf.Min(
                    allowedDistance,
                    Mathf.Max(0f, _cameraHits[i].distance - cameraCollisionPadding));
            }

            playerCamera.transform.position = pivotWorld + direction * allowedDistance;
        }

        public static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                UnlockCursor();
            }
        }
    }
}
