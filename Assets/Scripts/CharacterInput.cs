using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public class CharacterUnityInput : CharacterInput
    {
        [Header("Input Settings")]
        [Tooltip("Arrastra aquí la acción 'Move' de tu Input Action Asset")]
        public InputActionReference moveAction;

        private Vector2 currentMovementInput;

        private void OnEnable()
        {
            // Habilitar la acción cuando el script se activa
            if (moveAction != null)
            {
                moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
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
            return false;
            throw new System.NotImplementedException();
        }
    }
}