using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
	//This script is a slightly more specialized version of the regular 'CameraController' script, intended for games using a third-person camera.
	//By enabling 'turnCameraTowardMovementDirection', the camera will gradually rotate toward the current movement direction of the gameobject it is attached to;
	//The rate and speed of this rotation can be controlled using 'maximumMovementSpeed' and 'cameraTurnSpeed';
	public class ThirdPersonCameraController : CameraController {

		//Whether or not the camera turns towards the controller's movement direction;
		public bool turnCameraTowardMovementDirection = true;

		public Controller controller;

		//The maximum expected movement speed of this game object;
		//This value should be set to the maximum movement speed achievable by this gameobject;
		//The closer the current movement speed is to 'maximumMovementSpeed', the faster the camera will turn;
		//As a result, if the gameobject moves slower (i.e. "walking" instead of "running", in case of a character), the camera will turn slower as well.
		public float maximumMovementSpeed = 7f;

		//The general rate at which the camera turns toward the movement direction;
		public float cameraTurnSpeed = 120f;

		[Header("Camera Sensitivity")]
		[Range(0.1f, 5f)]
		public float mouseHorizontalSensitivity = 1f;

		[Range(0.1f, 5f)]
		public float mouseVerticalSensitivity = 1f;

		[Range(0.1f, 5f)]
		public float gamepadHorizontalSensitivity = 1f;

		[Range(0.1f, 5f)]
		public float gamepadVerticalSensitivity = 1f;

		private UnifiedCameraInput unifiedCameraInput;
		private PlayerInputs playerInputs;
		private bool isGamepadActive = false;
		private InputDevice lastInputDevice = null;

		protected override void Setup()
		{
			if(controller == null)
				Debug.LogWarning("No controller reference has been assigned to this script.", this.gameObject);

			//Hide and lock cursor on game start
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			//Check if old CameraMouseInput or CameraJoystickInput exists and replace with UnifiedCameraInput
			CameraInput oldCameraInput = GetComponent<CameraInput>();
			if(oldCameraInput != null && !(oldCameraInput is UnifiedCameraInput))
			{
				Debug.Log("Replacing legacy camera input (" + oldCameraInput.GetType().Name + ") with UnifiedCameraInput.");
				//Remove old input handler
				DestroyImmediate(oldCameraInput);
				//Add new unified input handler
				unifiedCameraInput = gameObject.AddComponent<UnifiedCameraInput>();
			}
			else if(oldCameraInput is UnifiedCameraInput)
			{
				unifiedCameraInput = (UnifiedCameraInput)oldCameraInput;
			}

			if(unifiedCameraInput != null)
			{
				playerInputs = unifiedCameraInput.PlayerInputsInstance;

				//Subscribe to RotateCamera action to detect which device is providing input
				playerInputs.Gameplay.RotateCamera.performed += OnRotateCameraInput;
				playerInputs.Gameplay.RotateCamera.canceled += OnRotateCameraInput;
			}
			else
			{
				Debug.LogWarning("Could not set up UnifiedCameraInput component. Device detection will not work properly.", this.gameObject);
			}

			//Subscribe to device change events
			InputSystem.onDeviceChange += OnDeviceChange;

			//Initial device state check
			UpdateInputDeviceState();
		}

		private void OnDestroy()
		{
			//Unsubscribe from events
			if(playerInputs != null)
			{
				playerInputs.Gameplay.RotateCamera.performed -= OnRotateCameraInput;
				playerInputs.Gameplay.RotateCamera.canceled -= OnRotateCameraInput;
			}

			InputSystem.onDeviceChange -= OnDeviceChange;
		}

		private void OnRotateCameraInput(InputAction.CallbackContext context)
		{
			//Detect which device generated the input
			if(context.control != null)
			{
				lastInputDevice = context.control.device;
				UpdateInputDeviceState();
			}
		}

		private void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			//Update device state when devices are added/removed/reconnected
			if(change == InputDeviceChange.Added || change == InputDeviceChange.Removed || change == InputDeviceChange.Reconnected)
			{
				UpdateInputDeviceState();
			}
		}

		private void UpdateInputDeviceState()
		{
			//Check if gamepad is active based on last input device
			isGamepadActive = CheckIfGamepadIsActive();
		}

		private bool CheckIfGamepadIsActive()
		{
			//If we have tracked the last input device and it's a gamepad, gamepad is active
			if(lastInputDevice is Gamepad)
				return true;

			//Check if a gamepad is currently connected
			if(Gamepad.current == null)
				return false;

			//If no input device has been tracked yet but gamepad exists, default to gamepad mode
			return lastInputDevice == null;
		}

		protected override void HandleCameraRotation ()
		{
			//Get input from camera input handler
			if(cameraInput == null)
				return;

			//Get raw input values
			float _inputHorizontal = cameraInput.GetHorizontalCameraInput();
			float _inputVertical = cameraInput.GetVerticalCameraInput();

			//Apply sensitivity based on current input device
			float horizontalSensitivity = isGamepadActive ? gamepadHorizontalSensitivity : mouseHorizontalSensitivity;
			float verticalSensitivity = isGamepadActive ? gamepadVerticalSensitivity : mouseVerticalSensitivity;

			_inputHorizontal *= horizontalSensitivity;
			_inputVertical *= verticalSensitivity;

			//Apply camera rotation with sensitivity-adjusted input
			RotateCamera(_inputHorizontal, _inputVertical);

			if(controller == null)
				return;

			//Only apply automatic camera rotation toward movement direction if gamepad is active
			if(turnCameraTowardMovementDirection && controller != null && isGamepadActive)
			{
				//Get controller velocity;
				Vector3 _controllerVelocity = controller.GetVelocity();

				RotateTowardsVelocity(_controllerVelocity, cameraTurnSpeed);
			}
		}

		//Rotate camera toward '_direction', at the rate of '_speed', around the upwards vector of this gameobject;
		public void RotateTowardsVelocity(Vector3 _velocity, float _speed)
		{
			//Remove any unwanted components of direction;
			_velocity = VectorMath.RemoveDotVector(_velocity, GetUpDirection());
			
			//Calculate angle difference of current direction and new direction;
			float _angle = VectorMath.GetAngle(GetFacingDirection(), _velocity, GetUpDirection());

			//Calculate sign of angle;
			float _sign = Mathf.Sign (_angle);

			//Calculate final angle difference;
			float _finalAngle =  Time.deltaTime * _speed * _sign * Mathf.Abs(_angle/90f);

			//If angle is greater than 90 degrees, recalculate final angle difference;
			if(Mathf.Abs(_angle) > 90f)
				_finalAngle = Time.deltaTime * _speed * _sign * ((Mathf.Abs (180f - Mathf.Abs(_angle)))/90f);

			//Check if calculated angle overshoots;
			if(Mathf.Abs (_finalAngle) > Mathf.Abs (_angle))
				_finalAngle = _angle;

			//Take movement speed into account by comparing it to 'maximumMovementSpeed';
			_finalAngle *= Mathf.InverseLerp(0f, maximumMovementSpeed, _velocity.magnitude);
		    
            SetRotationAngles(GetCurrentXAngle(), GetCurrentYAngle() + _finalAngle);
		}	
	}
}
