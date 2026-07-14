using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    // This script is a slightly more specialized version of the regular 'CameraController' script, intended for games using a third-person camera.
    // By enabling 'turnCameraTowardMovementDirection', the camera will gradually rotate toward the current movement direction of the gameobject it is attached to.
    public class ThirdPersonCameraController : CameraController {

        public bool turnCameraTowardMovementDirection = true;
        public Controller controller;
        public float maximumMovementSpeed = 7f;
        public float cameraTurnSpeed = 120f;

        [Header("Camera Sensitivity")]
        [Range(0f, 1f)]
        public float mouseHorizontalSensitivity = 1f;
        [Range(0f, 1f)]
        public float mouseVerticalSensitivity = 1f;
        [Range(0.1f, 5f)]
        public float gamepadHorizontalSensitivity = 1f;
        [Range(0.1f, 5f)]
        public float gamepadVerticalSensitivity = 1f;

        [Header("Mouse Smoothing (AAA Style)")]
        public bool useMouseSmoothing = true;
        [Range(0.01f, 0.3f)]
        public float mouseSmoothTime = 0.05f; // Lower values are more responsive, higher values add more weight/inertia

        private UnifiedCameraInput unifiedCameraInput;
        private PlayerInputs playerInputs;
        private bool isGamepadActive = false;

        // Physics-based smoothing variables
        private float horizontalInputVelocity;
        private float verticalInputVelocity;
        private float smoothedHorizontalInput;
        private float smoothedVerticalInput;

        protected override void Setup()
        {
            if(controller == null)
                Debug.LogWarning("No controller reference has been assigned to this script.", this.gameObject);

            CameraInput oldCameraInput = GetComponent<CameraInput>();
            if(oldCameraInput != null && !(oldCameraInput is UnifiedCameraInput))
            {
                DestroyImmediate(oldCameraInput);
                unifiedCameraInput = gameObject.AddComponent<UnifiedCameraInput>();
            }
            else if(oldCameraInput is UnifiedCameraInput)
            {
                unifiedCameraInput = (UnifiedCameraInput)oldCameraInput;
            }

            if(unifiedCameraInput != null)
            {
                playerInputs = unifiedCameraInput.PlayerInputsInstance;
            }

            // Listen to all input events globally to detect device changes instantly
            InputSystem.onEvent += OnInputEvent;
        }

        private void OnDestroy()
        {
            InputSystem.onEvent -= OnInputEvent;
        }

        private void OnInputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, InputDevice device)
        {
            if (device is Keyboard || device is Mouse)
            {
                isGamepadActive = false;
            }
            else if (device is Gamepad)
            {
                isGamepadActive = true;
            }
        }

        protected override void HandleCameraRotation ()
        {
            if(cameraInput == null)
                return;

            float inputHorizontal = cameraInput.GetHorizontalCameraInput();
            float inputVertical = cameraInput.GetVerticalCameraInput();

            float horizontalSensitivity = isGamepadActive ? gamepadHorizontalSensitivity : mouseHorizontalSensitivity;
            float verticalSensitivity = isGamepadActive ? gamepadVerticalSensitivity : mouseVerticalSensitivity;

            inputHorizontal *= horizontalSensitivity;
            inputVertical *= verticalSensitivity;

            if (!isGamepadActive)
            {
                // Cancel base Time.deltaTime multiplication for mouse input
                if (Time.deltaTime > 0f)
                {
                    inputHorizontal /= Time.deltaTime;
                    inputVertical /= Time.deltaTime;
                }

                // Smooth mouse input to achieve a high-quality glide feel (Odyssey/BotW style)
                if (useMouseSmoothing)
                {
                    smoothedHorizontalInput = Mathf.SmoothDamp(smoothedHorizontalInput, inputHorizontal, ref horizontalInputVelocity, mouseSmoothTime);
                    smoothedVerticalInput = Mathf.SmoothDamp(smoothedVerticalInput, inputVertical, ref verticalInputVelocity, mouseSmoothTime);
                }
                else
                {
                    smoothedHorizontalInput = inputHorizontal;
                    smoothedVerticalInput = inputVertical;
                }
            }
            else
            {
                // Gamepads naturally have physical spring resistance, so we bypass extra smoothing
                smoothedHorizontalInput = inputHorizontal;
                smoothedVerticalInput = inputVertical;
            }

            RotateCamera(smoothedHorizontalInput, smoothedVerticalInput);

            if(controller == null)
                return;

            // Only apply automatic camera rotation toward movement direction if gamepad is explicitly active
            if(turnCameraTowardMovementDirection && isGamepadActive)
            {
                Vector3 controllerVelocity = controller.GetVelocity();
                RotateTowardsVelocity(controllerVelocity, cameraTurnSpeed);
            }
        }

        public void RotateTowardsVelocity(Vector3 velocity, float speed)
        {
            velocity = VectorMath.RemoveDotVector(velocity, GetUpDirection());
            float angle = VectorMath.GetAngle(GetFacingDirection(), velocity, GetUpDirection());
            float sign = Mathf.Sign(angle);
            float finalAngle = Time.deltaTime * speed * sign * Mathf.Abs(angle / 90f);

            if(Mathf.Abs(angle) > 90f)
                finalAngle = Time.deltaTime * speed * sign * ((Mathf.Abs(180f - Mathf.Abs(angle))) / 90f);

            if(Mathf.Abs(finalAngle) > Mathf.Abs(angle))
                finalAngle = angle;

            finalAngle *= Mathf.InverseLerp(0f, maximumMovementSpeed, velocity.magnitude);
            
            SetRotationAngles(GetCurrentXAngle(), GetCurrentYAngle() + finalAngle);
        }   
    }
}