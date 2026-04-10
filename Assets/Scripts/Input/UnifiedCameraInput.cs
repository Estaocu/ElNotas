using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CMF;

/// <summary>
/// Camera input handler using the new Unity Input System (PlayerInputs)
/// Supports both mouse and gamepad input automatically
/// Exposes PlayerInputs instance for device detection in other systems
/// </summary>
public class UnifiedCameraInput : CameraInput
{
    private PlayerInputs playerInputs;
    private float horizontalInput = 0f;
    private float verticalInput = 0f;
    private InputDevice lastInputDevice = null;

    [Header("Mouse Sensitivity")]
    [Range(0.1f, 5f)]
    public float mouseHorizontalSensitivity = 1f;

    [Range(0.1f, 5f)]
    public float mouseVerticalSensitivity = 1f;

    [Header("Gamepad Sensitivity")]
    [Range(0.1f, 5f)]
    public float gamepadHorizontalSensitivity = 1f;

    [Range(0.1f, 5f)]
    public float gamepadVerticalSensitivity = 1f;

    /// <summary>
    /// Public reference to PlayerInputs for device detection in ThirdPersonCameraController
    /// </summary>
    public PlayerInputs PlayerInputsInstance => playerInputs;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        playerInputs.Gameplay.Enable();
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
            playerInputs.Dispose();
    }

    private void Update()
    {
        // Read RotateCamera action which supports both mouse delta and gamepad right stick
        Vector2 rotateCameraInput = playerInputs.Gameplay.RotateCamera.ReadValue<Vector2>();

        // Detect which device is providing input
        DetectActiveDevice();

        // Apply sensitivity based on input device
        float horizontalSensitivity = IsGamepadActive() ? gamepadHorizontalSensitivity : mouseHorizontalSensitivity;
        float verticalSensitivity = IsGamepadActive() ? gamepadVerticalSensitivity : mouseVerticalSensitivity;

        horizontalInput = rotateCameraInput.x * horizontalSensitivity;
        verticalInput = -rotateCameraInput.y * verticalSensitivity; // Invert vertical input (mouse and gamepad)
    }

    private void DetectActiveDevice()
    {
        // Check if any gamepad is connected and had input this frame
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            lastInputDevice = Gamepad.current;
            return;
        }

        // Check if mouse had input this frame
        if (Mouse.current != null && Mouse.current.wasUpdatedThisFrame)
        {
            lastInputDevice = Mouse.current;
            return;
        }
    }

    private bool IsGamepadActive()
    {
        return lastInputDevice is Gamepad;
    }

    public override float GetHorizontalCameraInput()
    {
        return horizontalInput;
    }

    public override float GetVerticalCameraInput()
    {
        return verticalInput;
    }
}
