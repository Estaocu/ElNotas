using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ElNotas.Input.Glyphs
{
    /// <summary>
    /// Drop-in component for any UI element or 3D object that displays a single keybind.
    /// Listens to device-change and rebind events and refreshes its Image / SpriteRenderer / Text
    /// to match the binding the player would actually press right now.
    /// </summary>
    public class BindingGlyphView : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private InputActionReference m_Action;
        [Tooltip("Optional binding id (Guid string). Leave empty to use the first binding that matches the current control scheme.")]
        [SerializeField] private string m_BindingId;

        [Header("Display Targets")]
        [SerializeField] private Image m_GlyphImage;
        [SerializeField] private SpriteRenderer m_GlyphSpriteRenderer;
        [SerializeField] private TMP_Text m_FallbackText;

        [Header("Options")]
        [SerializeField] private GlyphRegistry m_Registry;
        [SerializeField] private InputBinding.DisplayStringOptions m_DisplayOptions;

        public InputActionReference action
        {
            get => m_Action;
            set { m_Action = value; Refresh(); }
        }

        public string bindingId
        {
            get => m_BindingId;
            set { m_BindingId = value; Refresh(); }
        }

        private void OnEnable()
        {
            InputDeviceTracker.OnCategoryChanged += OnCategoryChanged;
            InputSystem.onActionChange += OnActionChange;
            Refresh();
        }

        private void OnDisable()
        {
            InputDeviceTracker.OnCategoryChanged -= OnCategoryChanged;
            InputSystem.onActionChange -= OnActionChange;
        }

        private void OnCategoryChanged(DeviceCategory _) => Refresh();

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged) return;
            var currentAction = m_Action?.action;
            if (currentAction == null) return;

            var changedAction = obj as InputAction;
            var changedMap = changedAction?.actionMap ?? obj as InputActionMap;
            var changedAsset = changedMap?.asset ?? obj as InputActionAsset;

            if (changedAction == currentAction ||
                changedMap == currentAction.actionMap ||
                changedAsset == currentAction.actionMap?.asset)
                Refresh();
        }

        /// <summary>
        /// Called by RebindActionUI.updateBindingUIEvent when this view sits next to a
        /// rebind row — same signature as the sample's GamepadIconsExample callback.
        /// </summary>
        public void OnUpdateBindingDisplay(Rebinding.RebindActionUI sender, string displayString, string deviceLayout, string controlPath)
        {
            Apply(displayString, controlPath);
        }

        public void Refresh()
        {
            var currentAction = m_Action?.action;
            if (currentAction == null) { Apply(string.Empty, null); return; }

            string displayString;
            string controlPath = null;
            string deviceLayout = null;

            int bindingIndex = ResolveBindingIndex(currentAction);
            if (bindingIndex >= 0)
                displayString = currentAction.GetBindingDisplayString(bindingIndex, out deviceLayout, out controlPath, m_DisplayOptions);
            else
                displayString = currentAction.GetBindingDisplayString(group: InputDeviceTracker.CurrentControlScheme, options: m_DisplayOptions);

            Apply(displayString, controlPath);
        }

        private int ResolveBindingIndex(InputAction actionToResolve)
        {
            if (!string.IsNullOrEmpty(m_BindingId))
            {
                for (int i = 0; i < actionToResolve.bindings.Count; i++)
                    if (actionToResolve.bindings[i].id.ToString() == m_BindingId)
                        return i;
            }

            // Fall back: first binding whose group matches the active scheme.
            var scheme = InputDeviceTracker.CurrentControlScheme;
            if (!string.IsNullOrEmpty(scheme))
            {
                for (int i = 0; i < actionToResolve.bindings.Count; i++)
                {
                    var b = actionToResolve.bindings[i];
                    if (b.isComposite) continue;
                    if (!string.IsNullOrEmpty(b.groups) && b.groups.Contains(scheme))
                        return i;
                }
            }
            return actionToResolve.bindings.Count > 0 ? 0 : -1;
        }

        private void Apply(string displayString, string controlPath)
        {
            var registry = m_Registry != null ? m_Registry : GlyphRegistry.Instance;
            Sprite sprite = registry != null ? registry.GetCurrentGlyph(controlPath) : null;

            if (m_GlyphImage != null)
            {
                m_GlyphImage.enabled = sprite != null;
                m_GlyphImage.sprite = sprite;
            }

            if (m_GlyphSpriteRenderer != null)
            {
                m_GlyphSpriteRenderer.enabled = sprite != null;
                m_GlyphSpriteRenderer.sprite = sprite;
            }

            if (m_FallbackText != null)
            {
                m_FallbackText.enabled = sprite == null;
                m_FallbackText.text = displayString ?? string.Empty;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled) Refresh();
        }
#endif
    }
}