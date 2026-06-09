using UnityEngine;

namespace ElNotas.Input.Glyphs
{
    /// <summary>
    /// Holds the per-category <see cref="GlyphDatabase"/> assets. Resolve glyphs for the
    /// currently active device via <see cref="GetGlyph"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "El Notas/Input/Glyph Registry", fileName = "GlyphRegistry")]
    public class GlyphRegistry : ScriptableObject
    {
        private static GlyphRegistry s_Instance;
        public static GlyphRegistry Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = Resources.Load<GlyphRegistry>("GlyphRegistry");
                return s_Instance;
            }
        }

        public GlyphDatabase keyboardMouse;
        public GlyphDatabase xbox;
        public GlyphDatabase playStation;
        public GlyphDatabase nintendoSwitch;
        public GlyphDatabase genericGamepad;

        public GlyphDatabase GetDatabase(DeviceCategory category)
        {
            switch (category)
            {
                case DeviceCategory.KeyboardMouse: return keyboardMouse;
                case DeviceCategory.XboxGamepad: return xbox;
                case DeviceCategory.PlayStationGamepad: return playStation;
                case DeviceCategory.SwitchGamepad: return nintendoSwitch;
                case DeviceCategory.GenericGamepad: return genericGamepad != null ? genericGamepad : xbox;
            }
            return null;
        }

        public Sprite GetGlyph(DeviceCategory category, string controlPath)
        {
            var db = GetDatabase(category);
            return db != null ? db.GetGlyph(controlPath) : null;
        }

        public Sprite GetCurrentGlyph(string controlPath)
        {
            return GetGlyph(InputDeviceTracker.Current, controlPath);
        }
    }
}
