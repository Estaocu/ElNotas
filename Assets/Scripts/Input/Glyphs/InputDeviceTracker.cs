using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ElNotas.Input.Glyphs
{
    /// <summary>
    /// Central tracker of which device family the player is currently using.
    /// Anything that displays a keybind glyph should query <see cref="Current"/> at
    /// activation time and subscribe to <see cref="OnCategoryChanged"/> for live updates.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class InputDeviceTracker : MonoBehaviour
    {
        public static DeviceCategory Current { get; private set; } = DeviceCategory.KeyboardMouse;
        public static string CurrentLayout { get; private set; } = "Keyboard";
        public static string CurrentControlScheme { get; private set; } = "KeyboardScheme";

        public static event Action<DeviceCategory> OnCategoryChanged;

        private static InputDeviceTracker s_Instance;

        [Tooltip("Optional. If set, scheme name is mirrored from this PlayerInput. Otherwise it's inferred from the last-used device.")]
        [SerializeField] private PlayerInput m_PlayerInput;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[InputDeviceTracker]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<InputDeviceTracker>();
        }

        private void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
            if (m_PlayerInput != null)
                m_PlayerInput.onControlsChanged += OnControlsChanged;

            // Seed from whatever is currently connected.
            if (Gamepad.current != null)
                ApplyDevice(Gamepad.current);
            else if (Keyboard.current != null)
                ApplyDevice(Keyboard.current);
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
            if (m_PlayerInput != null)
                m_PlayerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput pi)
        {
            if (pi != null && !string.IsNullOrEmpty(pi.currentControlScheme))
                CurrentControlScheme = pi.currentControlScheme;
        }

        private static void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null) return;
            if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type) return;

            // Ignore noise from devices that produce events without real actuation.
            if (!eventPtr.EnumerateChangedControls(device, magnitudeThreshold: 0.0001f).GetEnumerator().MoveNext())
                return;

            ApplyDevice(device);
        }

        private static void ApplyDevice(InputDevice device)
        {
            var category = Classify(device);
            var layout = device.layout;
            var changed = category != Current || layout != CurrentLayout;
            Current = category;
            CurrentLayout = layout;

            // Best-effort scheme inference if no PlayerInput is wired.
            if (s_Instance == null || s_Instance.m_PlayerInput == null)
                CurrentControlScheme = category == DeviceCategory.KeyboardMouse ? "KeyboardScheme" : "ControllerScheme";

            if (changed)
                OnCategoryChanged?.Invoke(Current);
        }

        public static DeviceCategory Classify(InputDevice device)
        {
            if (device is Keyboard || device is Mouse)
                return DeviceCategory.KeyboardMouse;

            var layout = device.layout;
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "DualShockGamepad") ||
                InputSystem.IsFirstLayoutBasedOnSecond(layout, "DualSenseGamepadHID"))
                return DeviceCategory.PlayStationGamepad;

            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "SwitchProControllerHID"))
                return DeviceCategory.SwitchGamepad;

            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "XInputController"))
                return DeviceCategory.XboxGamepad;

            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad"))
                return DeviceCategory.GenericGamepad;

            return DeviceCategory.KeyboardMouse;
        }
    }
}
