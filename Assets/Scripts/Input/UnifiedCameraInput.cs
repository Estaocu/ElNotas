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


    /// <summary>
    /// Public reference to PlayerInputs for device detection in ThirdPersonCameraController.
    /// Llama a EnsureInitialized() para evitar depender del orden de Awake.
    /// </summary>
    public PlayerInputs PlayerInputsInstance
    {
        get
        {
            EnsureInitialized();
            return playerInputs;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (playerInputs != null) return;
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

        horizontalInput = rotateCameraInput.x;
        verticalInput = -rotateCameraInput.y; // Invert vertical input (mouse and gamepad)
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
