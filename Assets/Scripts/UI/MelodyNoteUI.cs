using System.Collections;
using System.Collections.Generic;
using ElNotas.Input.Glyphs;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MelodyNoteUI : MonoBehaviour
{
    [SerializeField] private BindingGlyphView glyphView;
    public int order;
    public notesEnum note;

    public void SetGlyph(InputActionReference newAction)
    {
        glyphView.action = newAction;
    }
}
