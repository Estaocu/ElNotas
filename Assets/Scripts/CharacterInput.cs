using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public class CharacterUnityInput : CharacterInput
    {
        [Header("Input Settings")]
        [Tooltip("Arrastra aquí la acción 'Move' de tu Input Action Asset")]
        public InputActionReference moveAction;
        [Tooltip("Arrastra aquí la acción 'Jump' de tu Input Action Asset")]
        public InputActionReference jumpAction;

        private Vector2 currentMovementInput;

        private void OnEnable()
        {
            // Habilitar la acción cuando el script se activa
            if (moveAction != null)
            {
                moveAction.action.Enable();
            }
            if (jumpAction != null)
            {
                jumpAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.action.Disable();
            }
            if (jumpAction != null)
            {
                jumpAction.action.Disable();
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
            if (jumpAction == null)
                return false;
            return jumpAction.action.triggered;
        }
    }
}