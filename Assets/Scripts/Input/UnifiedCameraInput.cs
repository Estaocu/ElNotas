using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CMF;

public class UnifiedCameraInput : CameraInput
{
    private PlayerInputs playerInputs;
    private float horizontalInput = 0f;
    private float verticalInput = 0f;

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
        Vector2 rotateCameraInput = playerInputs.Gameplay.RotateCamera.ReadValue<Vector2>();

        horizontalInput = rotateCameraInput.x;
        verticalInput = -rotateCameraInput.y;
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