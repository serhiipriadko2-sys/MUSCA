using UnityEngine;

namespace MUSCA.Gate3D
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float eyeHeight = 1.68f;

        private CharacterController _controller;
        private float _pitch;

        public bool InputEnabled { get; set; } = true;
        public Camera PlayerCamera => playerCamera;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    UnlockCursor();
                }
                else
                {
                    LockCursor();
                }
            }

            if (!InputEnabled || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            transform.Rotate(Vector3.up, mouseX, Space.World);
            _pitch = Mathf.Clamp(_pitch - mouseY, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 velocity = transform.TransformDirection(input) * moveSpeed;
            if (!_controller.isGrounded)
            {
                velocity.y = -2f;
            }
            _controller.Move(velocity * Time.deltaTime);
        }

        public void Teleport(Vector3 position, float yawDegrees)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            _pitch = 0f;
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
                playerCamera.transform.localRotation = Quaternion.identity;
            }
            _controller.enabled = true;
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
