using UnityEngine;
using UnityEngine.UI;

public class UIPlayerNote : MonoBehaviour
{
    private Image sprite;
    private RectTransform rt;
    private float baseHeight;


    void Start()
    {
        sprite = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
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
    }

    public void CleanNote()
    {
        gameObject.SetActive(false);
    }
}
