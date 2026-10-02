using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerNote : MonoBehaviour
{
    private Image sprite;
    private RectTransform rt;
    private float baseHeight;
    private TextMeshProUGUI glyph;

    void Awake()
    {
        sprite = GetComponentInChildren<Image>();
        rt = GetComponent<RectTransform>();
        glyph = GetComponentInChildren<TextMeshProUGUI>();
    }


    void Start()
    {
        baseHeight = rt.anchoredPosition.y;
        CleanNote();
    }


    private void ChangeColor(Color targetColor)
    {
        sprite.color = targetColor;
        
    }

    private void ChangeHeight(int note)
    {
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseHeight + 45 * note);
    }

    public void MatchPlayedNote(Color color, int note)
    {
        gameObject.SetActive(true);
        ChangeColor(color);
        ChangeHeight(note);
        ChangeGlph(note);
    }

    private void TextOperation(string str)
    {
        glyph.SetText(str);
    }

    private void ChangeGlph(int note)
    {
        
        switch (note)
        {
            case 0:
            TextOperation("A");
            break;

            case 1:
            TextOperation("B");
            break;

            case 2:
            TextOperation("X");
            break;

            case 3:
            TextOperation("Y");
            break;

            default:
            TextOperation("?");
            break;

        }
    }

    public void CleanNote()
    {
        gameObject.SetActive(false);
    }
}
