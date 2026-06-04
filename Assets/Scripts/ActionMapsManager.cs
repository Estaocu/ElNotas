using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public enum DefaultActionMap { Gameplay, Notebook, PauseMenu, RestrictedInput, Conversation }

    public class ActionMapsManager : MonoBehaviour
    {
        public static ActionMapsManager Instance { get; private set; }

        [SerializeField] private DefaultActionMap defaultActionMap = DefaultActionMap.Gameplay;

        private PlayerInput playerInput;
        private string mapBeforePause;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            playerInput = GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
        }

        private void Start() => SwapActionMap(defaultActionMap);

        public void SwapActionMap(DefaultActionMap map) => SwapActionMap(map.ToString());

        public void SwapActionMap(string mapName)
        {
            if (playerInput == null) return;
            playerInput.SwitchCurrentActionMap(mapName);
            Debug.Log($"Cambiado a mapa: {mapName}");
        }

        public void SetRestrictedInput() => SwapActionMap(DefaultActionMap.RestrictedInput);
        public void SetPlayerInput()     => SwapActionMap(DefaultActionMap.Gameplay);
        public void SetConversationInput()     => SwapActionMap(DefaultActionMap.Conversation);

        public void StoreCurrentMap()    => mapBeforePause = playerInput?.currentActionMap?.name;

        public void RestoreLastMap()     => SwapActionMap(
            string.IsNullOrEmpty(mapBeforePause) ? DefaultActionMap.Gameplay.ToString() : mapBeforePause);
    }
}
