
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aviad.Samples
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 5;
        CharacterController characterController;
        InputAction moveAction;
        InputAction lookAction;
        public float mouseSensitivity = .4f;
        private float verticalRotation = 0f;
        private int maxLookAngle = 90;
        public Camera playerCamera;
        bool _active = true;
        [SerializeField] InputActionAsset actions;
        private void OnEnable()
        {
            actions.Enable();
        }
        private void OnDisable()
        {
            actions.Disable();
        }
        public void Start()
        {
            moveAction = actions.FindAction("Move");
            lookAction = actions.FindAction("Look");
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetMovementState(bool active)
        {
            _active = active;
        }

        void Update()
        {
            Vector2 moveValue = moveAction.ReadValue<Vector2>();

            //transform.position += dir.normalized * (Time.deltaTime * speed);
            Vector3 movement = Vector3.zero;
            var xMov = transform.right * moveValue.x * Time.deltaTime * speed;
            var fMov = transform.forward * moveValue.y * Time.deltaTime * speed;
            movement += xMov;
            movement += fMov;
            movement += Physics.gravity;
            if (_active)
            {
                characterController.Move(movement);
                HandleMouseLook();
            }
        }

        void HandleMouseLook()
        {
            Vector2 mouseInput = lookAction.ReadValue<Vector2>();

            // Apply sensitivity
            float mouseX = mouseInput.x * mouseSensitivity;
            float mouseY = mouseInput.y * mouseSensitivity;

            // Rotate the player body left and right
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera up and down
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }
    }
}