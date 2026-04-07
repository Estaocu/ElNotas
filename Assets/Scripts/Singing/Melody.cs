using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewMelody", menuName = "ScriptableObjects/Melody", order = 1)]
public class Melody : ScriptableObject
{
    [Header("Melody Settings")]
    public string melodyName;
    public notesEnum[] notes;
}

public enum notesEnum 
{
    A = 0, B = 1, C = 2, D = 3
}