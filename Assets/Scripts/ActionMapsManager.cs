using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CMF
{
    public enum DefaultActionMap
    {
        Gameplay,
        Notebook,
        PauseMenu,
        RestrictedInput,
        Conversation,
        Text,
        Notes
    }

    public class ActionMapsManager : MonoBehaviour
    {
        private bool canPlay = true;

        public bool CanPlay => canPlay;

        public static ActionMapsManager Instance { get; private set; }

        [SerializeField] private DefaultActionMap defaultActionMap = DefaultActionMap.Gameplay;

        private PlayerInput playerInput;
        private string mapBeforePause;
        private HudBehaviour hud;

        private readonly HashSet<string> activeMapNames = new();

        public IReadOnlyCollection<string> ActiveMapNames => activeMapNames;

        public DefaultActionMap CurrentActionMap
        {
            get
            {
                if (playerInput?.currentActionMap == null)
                    return defaultActionMap;

                if (System.Enum.TryParse(
                    playerInput.currentActionMap.name,
                    out DefaultActionMap result))
                {
                    return result;
                }

                return defaultActionMap;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            playerInput = GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
            hud = FindFirstObjectByType<HudBehaviour>();
        }

        private void Start()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                string defaultName = defaultActionMap.ToString();

                foreach (var map in playerInput.actions.actionMaps)
                {
                    if (map.name != defaultName)
                        map.Disable();
                }

                activeMapNames.Clear();
                activeMapNames.Add(defaultName);
            }

            SwapActionMap(defaultActionMap);
        }

        public void SwapActionMap(DefaultActionMap map)
        {
            SwapActionMap(map.ToString());
        }

        public void SwapActionMap(string mapName)
        {
            if (playerInput == null || playerInput.actions == null)
                return;

            foreach (var map in playerInput.actions.actionMaps)
            {
                if (map.name == mapName)
                    map.Enable();
                else
                    map.Disable();
            }

            activeMapNames.Clear();
            activeMapNames.Add(mapName);

            playerInput.SwitchCurrentActionMap(mapName);

            UpdateHud(mapName);

            Debug.Log($"Cambiado a mapa: {mapName}");
        }

        private void UpdateHud(string mapName)
        {
            if (hud == null)
                return;

            if (mapName == DefaultActionMap.Gameplay.ToString() || mapName == DefaultActionMap.Notes.ToString())
            {
                hud.ShowHud();
                return;
            }

            hud.HideHud();
        }

        public static void SetActiveMaps(params DefaultActionMap[] maps)
        {
            if (Instance == null || maps == null || maps.Length == 0)
                return;

            string[] names = new string[maps.Length];

            for (int i = 0; i < maps.Length; i++)
                names[i] = maps[i].ToString();

            Instance.ApplyActiveMaps(names);
        }

        public static void SetActiveMaps(params string[] mapNames)
        {
            if (Instance == null || mapNames == null || mapNames.Length == 0)
                return;

            Instance.ApplyActiveMaps(mapNames);
        }

        private void ApplyActiveMaps(string[] activeNames)
        {
            if (playerInput == null || playerInput.actions == null)
                return;

            activeMapNames.Clear();

            foreach (var map in playerInput.actions.actionMaps)
            {
                bool shouldBeActive = System.Array.IndexOf(activeNames, map.name) >= 0;

                if (shouldBeActive)
                {
                    map.Enable();
                    activeMapNames.Add(map.name);
                }
                else
                {
                    map.Disable();
                }
            }

            if (activeNames.Length > 0)
            {
                playerInput.SwitchCurrentActionMap(activeNames[0]);
                UpdateHud(activeNames[0]);
            }

            Debug.Log($"Maps activos: {string.Join(", ", activeMapNames)}");
        }

        public bool IsMapActive(DefaultActionMap map)
        {
            return IsMapActive(map.ToString());
        }

        public bool IsMapActive(string mapName)
        {
            return activeMapNames.Contains(mapName);
        }

        public bool IsCurrentMap(DefaultActionMap map)
        {
            return CurrentActionMap == map;
        }

        public bool IsCurrentMap(string mapName)
        {
            return playerInput?.currentActionMap?.name == mapName;
        }

        public static void SetRestrictedInput()
        {
            if (Instance == null)
                return;

            Instance.SwapActionMap(DefaultActionMap.RestrictedInput);
            Instance.canPlay = false;
        }

        public static void SetPlayerInput()
        {
            if (Instance == null)
                return;

            Instance.SwapActionMap(DefaultActionMap.Gameplay);
            Instance.canPlay = true;
        }

        public static void SetConversationInput()
        {
            if (Instance == null)
                return;

            Instance.SwapActionMap(DefaultActionMap.Conversation);
            Instance.canPlay = true;
        }

        public static void SetNotesInput()
        {
            if (Instance == null)
                return;

            Instance.SwapActionMap(DefaultActionMap.Notes);
            Instance.canPlay = true;
        }

        public static void SetTextInput()
        {
            if (Instance == null)
            {
                Debug.Log("ERROR! Instance es null");
                return;
            }

            Instance.SwapActionMap(DefaultActionMap.Text);
            Instance.canPlay = false;

            Debug.Log("SetTextInput ejecutado correctamente.");
        }

        public static void SetNotebookInput()
        {
            if (Instance == null)
                return;

            Instance.SwapActionMap(DefaultActionMap.Notebook);
            Instance.canPlay = false;
        }

        public void StoreCurrentMap()
        {
            mapBeforePause = playerInput?.currentActionMap?.name;
        }

        public void RestoreLastMap()
        {
            SwapActionMap(
                string.IsNullOrEmpty(mapBeforePause)
                    ? DefaultActionMap.Gameplay.ToString()
                    : mapBeforePause);
        }
    }
}

