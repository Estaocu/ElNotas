using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public class CharacterUnityInput : CharacterInput
    {
        [Header("Input Settings")]
        [Tooltip("Drag the 'Move' action from your Input Action Asset here")]
        public InputActionReference moveAction;

        private Vector2 currentMovementInput;

        private void OnEnable()
        {
            // Enable the action when the script is enabled
            if (moveAction != null)
            {
                moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            // Disable the action when the script is disabled
            if (moveAction != null)
            {
                moveAction.action.Disable();
            }
        }

        private void Update()
        {
            if (moveAction != null)
            {
                currentMovementInput = moveAction.action.ReadValue<Vector2>();
            }
        }

        public override float GetHorizontalMovementInput()
        {
            return currentMovementInput.x;
        }

        public override float GetVerticalMovementInput()
        {
            return currentMovementInput.y;
        }

        public override bool IsJumpKeyPressed()
        {
            // Return false to prevent compilation errors while disabling the jump functionality
            return false;
        }
    }
}