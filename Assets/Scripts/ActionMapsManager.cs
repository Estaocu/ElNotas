using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public class ActionMapsManager : MonoBehaviour
    {
        public static ActionMapsManager Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField] private PlayerInput playerInput;

        private const string MAP_PLAYER = "Player";
        private const string MAP_RESTRICTED = "RestrictedInput";
        
        // Variable para recordar el mapa antes de pausar
        private string mapBeforePause;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Esta es la función que te faltaba o tenía el nombre cambiado
        public void SwapActionMap(string mapName)
        {
            if (playerInput == null) return;
            
            playerInput.SwitchCurrentActionMap(mapName);
            Debug.Log($"Cambiado a mapa: {mapName}");
        }

        // --- FUNCIONES PARA TIMELINE Y TRIGGERS ---
        public void SetRestrictedInput()
        {
            SwapActionMap(MAP_RESTRICTED);
        }

        public void SetPlayerInput()
        {
            SwapActionMap(MAP_PLAYER);
        }

        // --- FUNCIONES PARA EL PAUSE MANAGER ---
        
        public void StoreCurrentMap()
        {
            if (playerInput != null && playerInput.currentActionMap != null)
            {
                mapBeforePause = playerInput.currentActionMap.name;
            }
        }

        public void RestoreLastMap()
        {
            if (!string.IsNullOrEmpty(mapBeforePause))
            {
                SwapActionMap(mapBeforePause);
            }
            else
            {
                // Por si acaso fallara algo, volvemos al modo normal
                SetPlayerInput();
            }
        }
    }
}