using UnityEngine;
using UnityEngine.InputSystem;

namespace ElNotas.Input.Rebinding
{
    /// <summary>
    /// Persists binding overrides for the referenced InputActionAsset in PlayerPrefs.
    /// Load happens on OnEnable so rebinds are present before UI/glyphs read the bindings.
    /// </summary>
    public class RebindSaveLoad : MonoBehaviour
    {
        public const string PlayerPrefsKey = "elnotas_rebinds";

        public InputActionAsset actions;

        public void OnEnable()
        {
            if (actions == null) return;
            var rebinds = PlayerPrefs.GetString(PlayerPrefsKey);
            if (!string.IsNullOrEmpty(rebinds))
                actions.LoadBindingOverridesFromJson(rebinds);
        }

        public void OnDisable()
        {
            if (actions == null) return;
            var rebinds = actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(PlayerPrefsKey, rebinds);
            PlayerPrefs.Save();
        }
    }
}
