using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElNotas.Input.Glyphs
{
    /// <summary>
    /// Maps Input System control paths (e.g. "buttonSouth", "dpad/up", "leftStick", "w")
    /// to sprite glyphs for a single device family. One asset per <see cref="DeviceCategory"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "El Notas/Input/Glyph Database", fileName = "GlyphDatabase")]
    public class GlyphDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Control path as it appears in the binding (no leading slash). E.g. 'buttonSouth', 'dpad/up', 'w', 'leftStick'.")]
            public string controlPath;
            public Sprite sprite;
        }

        public DeviceCategory category = DeviceCategory.KeyboardMouse;
        public List<Entry> entries = new List<Entry>();

        [Tooltip("Sprite used when a control path has no entry. Optional.")]
        public Sprite fallback;

        private Dictionary<string, Sprite> m_Lookup;

        private void OnEnable() { m_Lookup = null; }
        private void OnValidate() { m_Lookup = null; }

        public Sprite GetGlyph(string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath)) return fallback;
            EnsureLookup();
            return m_Lookup.TryGetValue(controlPath, out var s) ? s : fallback;
        }

        private void EnsureLookup()
        {
            if (m_Lookup != null) return;
            m_Lookup = new Dictionary<string, Sprite>(entries.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.controlPath)) continue;
                m_Lookup[e.controlPath] = e.sprite;
            }
        }
    }
}
