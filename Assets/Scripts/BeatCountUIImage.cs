using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatCountUIImage : MonoBehaviour
{
    public Texture2D[] texturesArray;
    public RawImage displayImage;
    public RawImage displayImageSubNotes;
    void Awake()
    {
     if (texturesArray.Length > 0 && displayImage != null)
     displayImage.texture = texturesArray[0];

    }

    public void SwapImage(int i)
    {
        if (displayImage != null && i <= texturesArray.Length)
        displayImage.texture = texturesArray[i];

    }

    public void SwapSubNoteImage(int subBeatIndex)
{
    if (displayImageSubNotes != null && subBeatIndex < texturesArray.Length)
        displayImageSubNotes.texture = texturesArray[subBeatIndex];
}
        
        
    }
