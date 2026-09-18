using UnityEngine;

namespace MUSCA.Gate3D
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 1.68f, 0f);
        [SerializeField] private bool cameraCollisionEnabled;
        [SerializeField] private float cameraCollisionRadius = 0.22f;
        [SerializeField] private float cameraCollisionPadding = 0.08f;
        [SerializeField] private LayerMask cameraCollisionMask = ~0;

        private CharacterController _controller;
        private float _pitch;
        private readonly RaycastHit[] _cameraHits = new RaycastHit[16];

        public bool InputEnabled { get; set; } = true;
        public Camera PlayerCamera => playerCamera;
        public Vector3 CameraLocalPosition => cameraLocalPosition;

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

            if (!InputEnabled || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            transform.Rotate(Vector3.up, mouseX, Space.World);
            _pitch = Mathf.Clamp(_pitch - mouseY, -80f, 80f);
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 velocity = transform.TransformDirection(input) * moveSpeed;
            if (!_controller.isGrounded)
            {
                velocity.y = -2f;
            }
            _controller.Move(velocity * Time.deltaTime);
        }

        private void LateUpdate()
        {
            UpdateCameraCollision();
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
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, _cameraHits[i].distance - cameraCollisionPadding));
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
