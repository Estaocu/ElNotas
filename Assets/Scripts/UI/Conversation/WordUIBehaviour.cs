using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Localization.Components;


public class WordUIBehaviour : MonoBehaviour
{
    
    public Image bgWord;
    public Image bgMelody;
    public TextMeshProUGUI wordText;
    public int currentSlot;
    public Word word;
    private RectTransform rectTransform;

    public int minSlot = 0;
    public int maxSlot = 4;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateVisuals();
    }

    public void SetWordText(LocalizedString sourceText)
    {
        wordText.SetText(sourceText.GetLocalizedString());
    }

    public void ToggleMelodyBg(bool state)
    {
        bgMelody.enabled = state;
    }
    public void RotateSlot(int dpadValue)
    {
        if (dpadValue == 0) return;

        rectTransform.transform.Rotate(0f, 0f, dpadValue * 30f);

    }

    public void UpdateVisuals()
    {
        if (currentSlot == minSlot || currentSlot == maxSlot)
        {
            //bgWord.color = Color.black;
            //ToggleVisibility(false);
            ToggleMelodyBg(false);  
            return;
        }

        if (currentSlot == 2)
        {
            //bgWord.color = Color.cyan;
            ToggleMelodyBg(true);
            return;
        }

        ToggleVisibility(true);
        //bgWord.color = Color.blue;
        ToggleMelodyBg(false);
    }

    public void TeleportExtremes(int polarity)
    {
        if (polarity == 1 && currentSlot == maxSlot)
    {
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        else if (polarity == -1 && currentSlot == minSlot)
        {
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, 75f);
        }   
    }

    public void DisplayNewWord(Word targetWord)
    {
        word = targetWord;
        SetWordText(word.displayName);
    }

    public void ToggleVisibility(bool isVisible)
    {
        // Desactiva o activa el componente visual sin afectar al GameObject
        if (wordText != null)
        {
            wordText.enabled = isVisible;
        }
        if (bgWord != null)
        {
            bgWord.enabled = isVisible;
        }
    }

    public void SetMaxSlots(int min, int max)
    {
        minSlot = min; maxSlot = max;
    }

    public void RecalculateSlotPosition()
{
    float zAngle = Mathf.DeltaAngle(0f, rectTransform.localEulerAngles.z);

    // 1. Cálculo de tu fórmula
    float nf = (75f - zAngle) / 30f;
    int rawSlot = Mathf.RoundToInt(nf);

    // 2. Total de slots (ej: 4 - 0 + 1 = 5 slots)
    int totalSlots = (maxSlot - minSlot) + 1;

    // 3. Módulo cíclico seguro: si rawSlot es -1 pasa a ser 4 (maxSlot), si es 5 pasa a ser 0 (minSlot)
    currentSlot = minSlot + ((rawSlot - minSlot) % totalSlots + totalSlots) % totalSlots;

    Debug.Log($"Rotation: {zAngle}° | Slot: [{currentSlot}]");
}
}
