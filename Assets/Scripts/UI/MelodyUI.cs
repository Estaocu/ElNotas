using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ElNotas.Input.Glyphs;
using System;

public class MelodyUI : MonoBehaviour
{
    [SerializeField] private MelodyNoteUI[] notes = new MelodyNoteUI[4];
    [Range(0f, 500f)]
    [SerializeField] private float xOffset;
    [Range(0f, 250f)]
    [SerializeField] private float yOffset;

    [SerializeField, HideInInspector] private UnityEngine.Vector2[] basePositions;
    [SerializeField] private InputActionReference[] noteActions = new InputActionReference[4];

    void OnValidate()
    {
        if (notes == null || notes.Length == 0) return;

        // Si es la primera vez o el array cambió, inicializamos las posiciones base
        if (basePositions == null || basePositions.Length != notes.Length)
        {
            CaptureBasePositions();
        }

        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] == null) continue;

            RectTransform rTr = notes[i].GetComponent<RectTransform>();
            if (rTr == null) continue;

            // Calculamos la posición final aplicando el offset a la posición original limpia
            float newX = basePositions[i].x + (xOffset * notes[i].order);
            float newY = basePositions[i].y + (yOffset * notes[i].order);

            rTr.anchoredPosition = new UnityEngine.Vector2(newX, newY);
        }
    }

    private void CaptureBasePositions()
    {
        basePositions = new UnityEngine.Vector2[notes.Length];
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] != null)
            {
                RectTransform rTr = notes[i].GetComponent<RectTransform>();
                if (rTr != null)
                {
                    basePositions[i] = rTr.anchoredPosition;
                }
            }
        }
    }

    public void ChangeMelodyDisplayed(notesEnum[] newMelody)
    {
        int count = Mathf.Min(notes.Length, newMelody.Length);
        for (int i = 0; i < count; i++)
        {
            notes[i].note = newMelody[i];
            notes[i].SetGlyph(EnumToInputAction(newMelody[i]));
        }
    }

    public void SetYValues()
    {
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] == null) continue;

            RectTransform rTr = notes[i].GetComponent<RectTransform>();
            if (rTr == null) continue;

            float newY = basePositions[i].y + (yOffset * (int)notes[i].note);

            rTr.anchoredPosition = new UnityEngine.Vector2(rTr.anchoredPosition.x, newY);
        }
        
    }

    public void HideMelody()
    {
        foreach(MelodyNoteUI note in notes)
        {
            if (note != null) note.gameObject.SetActive(false);
        }
    }

    public void ShowMelody()
    {
        foreach(MelodyNoteUI note in notes)
        {
            if (note != null) note.gameObject.SetActive(true);
        }
    }

    public InputActionReference EnumToInputAction(notesEnum note)
    {
        int idx = (int)note;
        return (idx >= 0 && idx < noteActions.Length) ? noteActions[idx] : null;
    }

    [ContextMenu("Reset Base Positions")]
    private void ResetBasePositions()
    {
        CaptureBasePositions();
    }
}
