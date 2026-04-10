using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewMelody", menuName = "ScriptableObjects/Melody", order = 1)]
public class Melody : ScriptableObject
{
    [Header("Melody Settings")]
    public string melodyName;
    public notesEnum[] notes;
    public Color color;
}

public enum notesEnum
{
    [InspectorName("1")] Note1 = 0,
    [InspectorName("2")] Note2 = 1,
    [InspectorName("3")] Note3 = 2,
    [InspectorName("4")] Note4 = 3
}