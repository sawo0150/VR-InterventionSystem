using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float verticalSpeed = 3f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;

    private float rotationX = 0f;
    private Vector2 moveInput;
    private Vector2 lookInput;

    void Start()
    {
        // Lock and hide cursor for better camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleCursorToggle();
    }

    private void HandleMovement()
    {
        // Get input from new Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // WASD movement
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;

        // Calculate movement direction
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize(); // Prevent faster diagonal movement

        // Check if sprinting
        float currentSpeed = keyboard.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;

        // Apply movement
        transform.position += moveDirection * currentSpeed * Time.deltaTime;

        // Vertical movement (up/down)
        if (keyboard.eKey.isPressed)
        {
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
        }
        if (keyboard.qKey.isPressed)
        {
            transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
        }
    }

    private void HandleMouseLook()
    {
        // Get mouse input from new Input System
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

        // Apply Y-axis inversion if enabled
        if (invertY)
        {
            mouseY = -mouseY;
        }

        // Rotate camera up/down (X rotation)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Limit vertical rotation

        // Apply rotations
        transform.localRotation = Quaternion.Euler(rotationX, transform.localEulerAngles.y + mouseX, 0f);
    }

    private void HandleCursorToggle()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null)
            return;

        // Press ESC to unlock cursor
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock cursor again
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
