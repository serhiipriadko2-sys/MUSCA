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
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedStickVelocity = -2f;
        [SerializeField] private float lockTurnSpeedDegrees = 360f;
        [SerializeField] private float lockCameraYawSmoothTime = 0.085f;
        [SerializeField] private float lockCameraMaxYawSpeed = 480f;
        [SerializeField] private float lockYawDeadZoneDegrees = 0.10f;
        [SerializeField] private float lockAimSmoothTime = 0.055f;
        [SerializeField] private float lockCameraPositionSmoothTime = 0.08f;
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 1.68f, 0f);
        [SerializeField] private bool cameraCollisionEnabled;
        [SerializeField] private float cameraCollisionRadius = 0.22f;
        [SerializeField] private float cameraCollisionPadding = 0.08f;
        [SerializeField] private LayerMask cameraCollisionMask = ~0;

        private CharacterController _controller;
        private float _pitch;
        private CollisionFlags _lastMoveCollisionFlags;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private Vector3 _dodgeDirectionWorld;
        private float _dodgeDistance;
        private float _dodgeDurationSeconds;
        private float _dodgeElapsedSeconds;
        private float _dodgeRemainingSeconds;
        private float _dodgeInvulnerableUntil = -1f;
        private Transform _lockOnTarget;
        private float _cameraYaw;
        private float _cameraYawVelocity;
        private Vector3 _cameraPositionVelocity;
        private Vector3 _lockAimPoint;
        private Vector3 _lockAimVelocity;
        private bool _lockAimInitialized;
        private bool _cameraYawInitialized;
        private bool _externalCameraDriverActive;
        private readonly RaycastHit[] _cameraHits = new RaycastHit[16];

        public bool InputEnabled { get; set; } = true;
        public Camera PlayerCamera => playerCamera;
        public Vector3 CameraLocalPosition => cameraLocalPosition;
        public CollisionFlags LastMoveCollisionFlags => _lastMoveCollisionFlags;
        public bool LastMoveGrounded => (_lastMoveCollisionFlags & CollisionFlags.Below) != 0;
        public bool IsDodging => _dodgeRemainingSeconds > 0.0001f;
        public bool IsDodgeInvulnerable => IsDodging && Time.time < _dodgeInvulnerableUntil;
        public bool CanStartDodge => InputEnabled && _controller != null &&
                                     _controller.enabled && !IsDodging && IsGrounded;
        public float PlanarSpeed => _planarVelocity.magnitude;
        public Vector3 PlanarVelocity => _planarVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public float DodgeNormalizedTime => IsDodging && _dodgeDurationSeconds > 0.0001f
            ? Mathf.Clamp01(_dodgeElapsedSeconds / _dodgeDurationSeconds)
            : 0f;
        public Vector3 DodgeDirectionWorld => _dodgeDirectionWorld;
        public bool IsGrounded => _controller != null &&
                                  (_controller.isGrounded || LastMoveGrounded);
        public bool CanJump => InputEnabled && !IsDodging && IsGrounded;
        public Transform LockOnTarget => _lockOnTarget;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
            ApplyCameraPosition();
            SyncCameraYaw();
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
                float dodgeFrameSeconds = Mathf.Max(0f, Time.deltaTime);
                float committedSeconds = ComputeCommittedStepSeconds(
                    _dodgeRemainingSeconds, dodgeFrameSeconds);
                float previousNormalized = _dodgeDurationSeconds > 0.0001f
                    ? Mathf.Clamp01(_dodgeElapsedSeconds / _dodgeDurationSeconds)
                    : 1f;
                float nextElapsed = Mathf.Min(
                    _dodgeDurationSeconds, _dodgeElapsedSeconds + committedSeconds);
                float nextNormalized = _dodgeDurationSeconds > 0.0001f
                    ? Mathf.Clamp01(nextElapsed / _dodgeDurationSeconds)
                    : 1f;
                float distanceStep = _dodgeDistance *
                    (ComputeDodgeProgress(nextNormalized) -
                     ComputeDodgeProgress(previousNormalized));
                Vector3 dodgeDisplacement =
                    _dodgeDirectionWorld * Mathf.Max(0f, distanceStep);

                if (IsGrounded && _verticalVelocity < 0f)
                {
                    _verticalVelocity = groundedStickVelocity;
                }

                dodgeDisplacement.y = ComputeVerticalDisplacement(
                    _verticalVelocity, gravity, committedSeconds);
                _verticalVelocity = ComputeVerticalVelocity(
                    _verticalVelocity, gravity, committedSeconds);
                _lastMoveCollisionFlags = _controller.Move(dodgeDisplacement);
                _dodgeElapsedSeconds = nextElapsed;
                _dodgeRemainingSeconds = Mathf.Max(
                    0f, _dodgeRemainingSeconds - committedSeconds);
                if (!IsDodging)
                {
                    _dodgeDirectionWorld = Vector3.zero;
                    _dodgeDistance = 0f;
                }
                return;
            }

            bool gameplayInputActive = Cursor.lockState == CursorLockMode.Locked;
            if (gameplayInputActive)
            {
                if (_externalCameraDriverActive)
                {
                    UpdateExternalBodyFacing();
                }
                else
                {
                    UpdateView();
                }
            }

            Vector3 input = gameplayInputActive
                ? new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"))
                : Vector3.zero;
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 desiredDirection = _externalCameraDriverActive
                ? ResolveCameraRelativeMovement(input, playerCamera, transform)
                : ResolveMovementDirection(
                    input, transform, _cameraYaw, _lockOnTarget != null);
            Vector3 desiredVelocity = desiredDirection * moveSpeed;
            desiredVelocity.y = 0f;

            float rate = input.sqrMagnitude > 0.0001f ? moveAcceleration : moveDeceleration;
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity, desiredVelocity, Mathf.Max(0f, rate) * Time.deltaTime);

            bool groundedBeforeMove = IsGrounded;
            if (groundedBeforeMove && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedStickVelocity;
            }

            bool jumpStarted = gameplayInputActive &&
                groundedBeforeMove &&
                Input.GetKeyDown(jumpKey) &&
                TryStartJump();

            float frameSeconds = Mathf.Max(0f, Time.deltaTime);
            float verticalDisplacement = ComputeVerticalDisplacement(
                _verticalVelocity, gravity, frameSeconds);
            _verticalVelocity = ComputeVerticalVelocity(
                _verticalVelocity, gravity, frameSeconds);

            Vector3 displacement = _planarVelocity * frameSeconds;
            displacement.y = verticalDisplacement;
            _lastMoveCollisionFlags = _controller.Move(displacement);

            if ((_lastMoveCollisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }
        }

        private void LateUpdate()
        {
            if (_externalCameraDriverActive)
            {
                return;
            }

            UpdateLockCameraYaw();
            UpdateCameraPose();
            UpdateCameraCollision();
        }

        private void UpdateExternalBodyFacing()
        {
            float targetYaw = transform.eulerAngles.y;
            if (_lockOnTarget != null)
            {
                Vector3 toTarget = _lockOnTarget.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                }
            }
            else if (playerCamera != null)
            {
                targetYaw = playerCamera.transform.eulerAngles.y;
            }

            float bodyYaw = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetYaw,
                Mathf.Max(1f, lockTurnSpeedDegrees) * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);
        }

        private void UpdateView()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            EnsureCameraYawInitialized();

            if (_lockOnTarget != null)
            {
                // Camera orbit and body facing are deliberately decoupled.
                // The body follows the previous LateUpdate camera solution while
                // movement remains camera-relative, so strafing cannot feed back
                // into its own reference axes.
                float bodyYaw = Mathf.MoveTowardsAngle(
                    transform.eulerAngles.y,
                    _cameraYaw,
                    Mathf.Max(1f, lockTurnSpeedDegrees) * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);
            }
            else
            {
                transform.Rotate(Vector3.up, mouseX, Space.World);
                _cameraYaw = transform.eulerAngles.y;
                _cameraYawVelocity = 0f;
            }

            _pitch = Mathf.Clamp(_pitch - mouseY, -80f, 80f);
        }

        private void UpdateLockCameraYaw()
        {
            EnsureCameraYawInitialized();
            if (_lockOnTarget == null)
            {
                _cameraYaw = transform.eulerAngles.y;
                _cameraYawVelocity = 0f;
                _lockAimVelocity = Vector3.zero;
                _lockAimInitialized = false;
                return;
            }

            Vector3 rawAim = _lockOnTarget.position;
            if (!_lockAimInitialized)
            {
                _lockAimPoint = rawAim;
                _lockAimVelocity = Vector3.zero;
                _lockAimInitialized = true;
            }
            else
            {
                _lockAimPoint = Vector3.SmoothDamp(
                    _lockAimPoint,
                    rawAim,
                    ref _lockAimVelocity,
                    Mathf.Max(0.0001f, lockAimSmoothTime),
                    Mathf.Infinity,
                    Time.deltaTime);
            }

            Vector3 toTarget = _lockAimPoint - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float desiredYaw = Mathf.Atan2(
                toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            _cameraYaw = ComputeLockYawStep(
                _cameraYaw,
                desiredYaw,
                ref _cameraYawVelocity,
                lockCameraYawSmoothTime,
                lockCameraMaxYawSpeed,
                lockYawDeadZoneDegrees,
                Time.deltaTime);
        }

        private void UpdateCameraPose()
        {
            EnsureCameraYawInitialized();
            if (playerCamera != null)
            {
                playerCamera.transform.rotation =
                    Quaternion.Euler(_pitch, _cameraYaw, 0f);
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
            _dodgeDirectionWorld = worldDirection.normalized;
            _dodgeDistance = speed * durationSeconds;
            _dodgeDurationSeconds = durationSeconds;
            _dodgeElapsedSeconds = 0f;
            _dodgeRemainingSeconds = durationSeconds;
            _dodgeInvulnerableUntil = Time.time +
                Mathf.Clamp(invulnerableSeconds, 0f, durationSeconds);
            return true;
        }

        public void CancelDodge()
        {
            _dodgeRemainingSeconds = 0f;
            _dodgeInvulnerableUntil = -1f;
            _dodgeDirectionWorld = Vector3.zero;
            _dodgeDistance = 0f;
            _dodgeDurationSeconds = 0f;
            _dodgeElapsedSeconds = 0f;
        }

        public static Vector3 ResolveCameraRelativeMovement(
            Vector3 input,
            Camera camera,
            Transform fallbackBody)
        {
            input.y = 0f;
            input = Vector3.ClampMagnitude(input, 1f);
            if (input.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            if (camera == null)
            {
                return fallbackBody != null
                    ? fallbackBody.TransformDirection(input).normalized
                    : input.normalized;
            }

            Vector3 forward = camera.transform.forward;
            Vector3 right = camera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f
                ? right.normalized
                : Vector3.right;

            Vector3 world = right * input.x + forward * input.z;
            return world.sqrMagnitude > 0.0001f
                ? world.normalized
                : Vector3.zero;
        }

        public static float ComputeDodgeProgress(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            float inverse = 1f - t;
            // Quadratic ease-out keeps the evade decisive without the v0.5
            // cubic curve's near-teleport launch. Endpoints remain exact.
            return 1f - inverse * inverse;
        }

        public static Vector3 ResolveMovementDirection(
            Vector3 input,
            Transform body,
            float cameraYawDegrees,
            bool lockOn)
        {
            input.y = 0f;
            input = Vector3.ClampMagnitude(input, 1f);
            if (input.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            if (!lockOn || body == null)
            {
                return body != null
                    ? body.TransformDirection(input).normalized
                    : input.normalized;
            }

            Quaternion cameraBasis = Quaternion.Euler(0f, cameraYawDegrees, 0f);
            Vector3 forward = cameraBasis * Vector3.forward;
            Vector3 right = cameraBasis * Vector3.right;
            Vector3 world = right * input.x + forward * input.z;
            world.y = 0f;
            return world.sqrMagnitude > 0.0001f
                ? world.normalized
                : Vector3.zero;
        }

        public static float ComputeLockYawStep(
            float currentYaw,
            float desiredYaw,
            ref float yawVelocity,
            float smoothTime,
            float maxSpeed,
            float deadZoneDegrees,
            float deltaTime)
        {
            float delta = Mathf.DeltaAngle(currentYaw, desiredYaw);
            if (Mathf.Abs(delta) <= Mathf.Max(0f, deadZoneDegrees))
            {
                yawVelocity = 0f;
                return currentYaw;
            }

            return Mathf.SmoothDampAngle(
                currentYaw,
                desiredYaw,
                ref yawVelocity,
                Mathf.Max(0.0001f, smoothTime),
                Mathf.Max(1f, maxSpeed),
                Mathf.Max(0f, deltaTime));
        }

        public static float ComputeCommittedStepSeconds(float remainingSeconds, float deltaTime)
        {
            return Mathf.Min(Mathf.Max(0f, remainingSeconds), Mathf.Max(0f, deltaTime));
        }

        public static float ComputeJumpVelocity(float height, float gravityValue)
        {
            if (height <= 0f || gravityValue >= 0f) return 0f;
            return Mathf.Sqrt(height * -2f * gravityValue);
        }

        public static float ComputeVerticalDisplacement(
            float velocity, float gravityValue, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            return velocity * dt + 0.5f * gravityValue * dt * dt;
        }

        public static float ComputeVerticalVelocity(
            float velocity, float gravityValue, float deltaTime)
        {
            return velocity + gravityValue * Mathf.Max(0f, deltaTime);
        }

        public bool TryStartJump()
        {
            if (!CanJump) return false;
            _verticalVelocity = ComputeJumpVelocity(jumpHeight, gravity);
            return _verticalVelocity > 0f;
        }

        public void SetExternalCameraDriver(bool active)
        {
            _externalCameraDriverActive = active;
            if (active)
            {
                _cameraPositionVelocity = Vector3.zero;
                _cameraYawVelocity = 0f;
                _lockAimVelocity = Vector3.zero;
                _lockAimInitialized = false;
            }
            else
            {
                SyncCameraYaw();
            }
        }

        public void SetLockOnTarget(Transform target)
        {
            EnsureCameraYawInitialized();
            if (_lockOnTarget != null && target == null)
            {
                transform.rotation = Quaternion.Euler(0f, _cameraYaw, 0f);
                _cameraYawVelocity = 0f;
                _lockAimVelocity = Vector3.zero;
                _lockAimInitialized = false;
            }

            _lockOnTarget = target;
            if (_lockOnTarget != null)
            {
                _lockAimPoint = _lockOnTarget.position;
                _lockAimVelocity = Vector3.zero;
                _lockAimInitialized = true;
            }
        }

        public void ConfigureCamera(Camera value, Vector3 localPosition, float fieldOfView)
        {
            playerCamera = value;
            cameraLocalPosition = localPosition;
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = fieldOfView;
                ApplyCameraPosition();
                SyncCameraYaw();
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
            _verticalVelocity = groundedStickVelocity;
            CancelDodge();
            ApplyCameraPosition();
            SyncCameraYaw();
            if (playerCamera != null)
            {
                playerCamera.transform.rotation =
                    Quaternion.Euler(_pitch, _cameraYaw, 0f);
            }
            _controller.enabled = true;
        }

        private void ApplyCameraPosition()
        {
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = cameraLocalPosition;
                _cameraPositionVelocity = Vector3.zero;
            }
        }

        private void SyncCameraYaw()
        {
            if (playerCamera == null)
            {
                _cameraYaw = transform.eulerAngles.y;
            }
            else
            {
                _cameraYaw = playerCamera.transform.eulerAngles.y;
            }
            _cameraYawVelocity = 0f;
            _cameraYawInitialized = true;
        }

        private void EnsureCameraYawInitialized()
        {
            if (!_cameraYawInitialized)
            {
                SyncCameraYaw();
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

            Vector3 targetPosition =
                pivotWorld + direction * allowedDistance;
            if (_lockOnTarget != null)
            {
                playerCamera.transform.position = Vector3.SmoothDamp(
                    playerCamera.transform.position,
                    targetPosition,
                    ref _cameraPositionVelocity,
                    Mathf.Max(0.0001f, lockCameraPositionSmoothTime),
                    Mathf.Infinity,
                    Time.deltaTime);
            }
            else
            {
                playerCamera.transform.position = targetPosition;
                _cameraPositionVelocity = Vector3.zero;
            }
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
