using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public enum DefaultActionMap { Gameplay, Notebook, PauseMenu, RestrictedInput, Conversation, Text }

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

        private void Start()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                string defaultName = defaultActionMap.ToString();
                foreach (var map in playerInput.actions.actionMaps)
                {
                    if (map.name != defaultName) map.Disable();
                }
            }
            SwapActionMap(defaultActionMap);
        }

        public void SwapActionMap(DefaultActionMap map) => SwapActionMap(map.ToString());

        public void SwapActionMap(string mapName)
        {
            if (playerInput == null) return;
            playerInput.SwitchCurrentActionMap(mapName);
            Debug.Log($"Cambiado a mapa: {mapName}");
        }

        public static void SetActiveMaps(params DefaultActionMap[] maps)
        {
            if (Instance == null || maps == null || maps.Length == 0) return;
            string[] names = new string[maps.Length];
            for (int i = 0; i < maps.Length; i++) names[i] = maps[i].ToString();
            Instance.ApplyActiveMaps(names);
        }

        public static void SetActiveMaps(params string[] mapNames)
        {
            if (Instance == null || mapNames == null || mapNames.Length == 0) return;
            Instance.ApplyActiveMaps(mapNames);
        }

        private void ApplyActiveMaps(string[] activeNames)
        {
            if (playerInput == null || playerInput.actions == null) return;

            foreach (var map in playerInput.actions.actionMaps)
            {
                bool shouldBeActive = System.Array.IndexOf(activeNames, map.name) >= 0;
                if (shouldBeActive) map.Enable();
                else map.Disable();
            }

            // Mantén el "currentActionMap" sincronizado con el primero de la lista
            if (activeNames.Length > 0) playerInput.SwitchCurrentActionMap(activeNames[0]);
            Debug.Log($"Maps activos: {string.Join(", ", activeNames)}");
        }

        public void SetRestrictedInput() => SwapActionMap(DefaultActionMap.RestrictedInput);
        public void SetPlayerInput()     => SwapActionMap(DefaultActionMap.Gameplay);
        public void SetConversationInput()     => SwapActionMap(DefaultActionMap.Conversation);
        public void SetNotesInput()     => SwapActionMap(DefaultActionMap.Notes);
        public void SetTextInput()     => SwapActionMap(DefaultActionMap.Conversation);

        public void StoreCurrentMap()    => mapBeforePause = playerInput?.currentActionMap?.name;

        public void RestoreLastMap()     => SwapActionMap(
            string.IsNullOrEmpty(mapBeforePause) ? DefaultActionMap.Gameplay.ToString() : mapBeforePause);
    }
}
