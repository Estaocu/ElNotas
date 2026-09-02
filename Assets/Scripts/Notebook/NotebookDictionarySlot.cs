using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotebookDictionarySlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI wordTxt;
    [SerializeField] private MelodyUI score; 



    public void RecalculateDisplay()
    {
        //Debug.Log("Recalculated Display");
    }

    public void DisplayScore()
    {
        score.ShowMelody();
        score.SetYValues();
    }
}
