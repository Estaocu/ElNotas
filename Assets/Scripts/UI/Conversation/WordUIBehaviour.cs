using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using System.Linq;


public class WordUIBehaviour : MonoBehaviour
{
    
    public Image bgWord;
    public Image bgMelody;
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI melodyString;
    // public bool isSelected; REDUNDANTE, SI ES SLOT 3 (o 2 segun como cuentes) ESTÁ SELECTED SIEMPRE
    public int currentSlot;
    public Word word;
    [SerializeField] private float rotationTime;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private RectTransform rectTransform;
    public float startAngle;
    public float endAngle;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }


    void Start()
    {
        UpdateVisuals();
        DisplayNewWord();
    }

    public void SetWordText(LocalizedString sourceText)
    {
        wordText.SetText(sourceText.GetLocalizedString());
    }

    public void SetMelodyString(string sourceText)
    {
        melodyString.SetText(sourceText);
    }

    public void HideMelody()
    {
        bgMelody.GetComponent<Image>().enabled = false;
        melodyString.enabled = false;
    }
    public void ShowMelody()
    {
        bgMelody.GetComponent<Image>().enabled = true;
        melodyString.enabled = true;
    }
    public void RotateSlot(int dpadValue)
    {
        if (dpadValue == 0) return;
        //Rotation time debe ser igual? independ. de qué operación se va a hacer, mismo tiempo con  30º que 360º
        //SetEndAngle(false, dpadValue, slotAmount);

        if (dpadValue == -1)
        {
            gameObject.transform.Rotate(0f, 0f, -30f);
            Debug.Log("Rotated parriba");
        }
        else gameObject.transform.Rotate(0f, 0f, 30f);
        Debug.Log("Rotated pabajo");

        OnWheelRotated(dpadValue);
    }

    private IEnumerator RotateSlotRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < rotationTime)
        {
            elapsedTime += Time.deltaTime;

            float linearT = Mathf.Clamp01(elapsedTime / rotationTime);

            float curveT = curve.Evaluate(linearT);

            float currentAngle = Mathf.Lerp(startAngle, endAngle, curveT);

            rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }

        rectTransform.localRotation = Quaternion.Euler(0f, 0f, endAngle);
    }

    public void SetEndAngle(bool regularRotation, int slotAmount, int dpadValue = 0)
    {
        if (dpadValue == 0) return;

        startAngle = rectTransform.localRotation.z;

        if (regularRotation)
        {
            endAngle = 1;


        }
        else

        endAngle = startAngle + (360-(slotAmount*30)*dpadValue);
    }

    public void OnWheelRotated(int dpadValue)
    {
        currentSlot = currentSlot - dpadValue;
        UpdateVisuals(dpadValue);
        //TeleportExtremes();
    }

    public void UpdateVisuals(int dpadValue = 0)
    {
        //SetWordText(currentSlot.ToString());
        switch (currentSlot)
        {
            case 0:
            case 4:
            bgWord.color = Color.black;
            //bgWord.enabled = false;
            //wordText.enabled = false;
            ToggleVisibility(false);
            TeleportExtremes();
            HideMelody();    
            DisplayNewWord();

            break;

            case 1:
            case 3:
            ToggleVisibility(true);
            bgWord.color = Color.blue;
            HideMelody();
            break;

            case 2:
            bgWord.color = Color.cyan;
            ShowMelody();
            break;

            default:
            Debug.Log("Default in Switch!");
            break;

        }
    }

    public void TeleportExtremes()
    {
        if (currentSlot != 0 || currentSlot != 4) return;
        if (currentSlot == 0)
        {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
        }
        else
        {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
        }
    }

    public void DisplayNewWord()
    {
        SetWordText(word.displayName);
        string melodyText = string.Concat(word.melody.Select(n => ((int)n + 1).ToString()));
        //melodyString.SetText(melodyText);
        melodyString.SetText(currentSlot.ToString());
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
}
