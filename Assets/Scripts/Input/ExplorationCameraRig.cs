using UnityEngine;
using UnityEngine.InputSystem;
using CMF;

// Attach this script to a root "CameraPivot" object.
// The Main Camera should be a child of this object, offset backwards on the Z axis.
public class ExplorationCameraRig : MonoBehaviour
{
    [Header("Tracking")]
    public Transform followTarget; // Usually the player's center or head
    public Controller characterController; // Reference to CMF controller for velocity

    [Header("Mouse Settings (Raw Delta)")]
    public float mouseSensitivity = 0.5f; 

    [Header("Gamepad Settings (Time Based)")]
    public float gamepadSensitivity = 150f; 

    [Header("Smoothing (Odyssey / BotW feel)")]
    [Range(0.01f, 0.2f)]
    public float rotationSmoothTime = 0.08f;
    [Range(0.01f, 0.2f)]
    public float positionSmoothTime = 0.05f; // Slight delay when following the character

    [Header("Limits")]
    public float minTilt = -25f;
    public float maxTilt = 75f;

    [Header("Auto Alignment (Gamepad Only)")]
    public bool alignToMovement = true;
    public float alignSpeed = 60f; // Degrees per second
    public float timeBeforeAlignment = 1.5f; // Wait time after user stops moving camera

    // Internal input and state
    private PlayerInputs playerInputs;
    private bool isGamepadActive;
    private float lastCameraInputTime;

    // Rotation targets and current values
    private float targetPan;
    private float targetTilt;
    private float currentPan;
    private float currentTilt;
    
    // Smoothing velocities
    private float panVelocity;
    private float tiltVelocity;
    private Vector3 positionVelocity;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        playerInputs.Gameplay.Enable();

        // Initialize angles to prevent snapping on start
        Vector3 initialAngles = transform.eulerAngles;
        targetPan = currentPan = initialAngles.y;
        targetTilt = currentTilt = initialAngles.x;

        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
            playerInputs.Dispose();

        InputSystem.onEvent -= OnInputEvent;
    }

    private void OnInputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, InputDevice device)
    {
        // Detect hardware seamlessly
        if (device is Keyboard || device is Mouse)
            isGamepadActive = false;
        else if (device is Gamepad)
            isGamepadActive = true;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        HandleCameraInput();
        HandleAutoAlignment();
        ApplySmoothingAndMovement();
    }

    private void HandleCameraInput()
    {
        Vector2 input = playerInputs.Gameplay.RotateCamera.ReadValue<Vector2>();
        
        // Invert Y input for natural feel
        input.y = -input.y;

        if (Mathf.Abs(input.x) > 0.01f || Mathf.Abs(input.y) > 0.01f)
        {
            lastCameraInputTime = Time.time;

            if (isGamepadActive)
            {
                // Gamepad requires Time.deltaTime because it acts as a constant speed (degrees per second)
                targetPan += input.x * gamepadSensitivity * Time.deltaTime;
                targetTilt += input.y * gamepadSensitivity * Time.deltaTime;
            }
            else
            {
                // Mouse is physical movement (pixels). NEVER use Time.deltaTime here.
                // This completely eliminates inverse acceleration.
                targetPan += input.x * mouseSensitivity;
                targetTilt += input.y * mouseSensitivity;
            }
        }

        // Clamp vertical tilt
        targetTilt = Mathf.Clamp(targetTilt, minTilt, maxTilt);
    }

    private void HandleAutoAlignment()
    {
        // Only align if using gamepad, auto-align is enabled, and we have a reference to the controller
        if (isGamepadActive && alignToMovement && characterController != null)
        {
            // Only intervene if the user hasn't touched the camera recently
            if (Time.time - lastCameraInputTime > timeBeforeAlignment)
            {
                Vector3 currentVelocity = characterController.GetVelocity();
                currentVelocity.y = 0f; // Discard vertical momentum (jumping/falling)

                // Only align if character is actually moving
                if (currentVelocity.sqrMagnitude > 0.5f)
                {
                    float targetAlignAngle = Quaternion.LookRotation(currentVelocity).eulerAngles.y;
                    
                    // Gently push the target pan towards the movement direction
                    targetPan = Mathf.MoveTowardsAngle(targetPan, targetAlignAngle, alignSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void ApplySmoothingAndMovement()
    {
        // 1. Smoothly interpolate rotation (The Odyssey/BotW spring effect)
        currentPan = Mathf.SmoothDampAngle(currentPan, targetPan, ref panVelocity, rotationSmoothTime);
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(currentTilt, currentPan, 0f);

        // 2. Smoothly follow the target position (adds a subtle "weight" to the character's movement)
        transform.position = Vector3.SmoothDamp(transform.position, followTarget.position, ref positionVelocity, positionSmoothTime);
    }
}