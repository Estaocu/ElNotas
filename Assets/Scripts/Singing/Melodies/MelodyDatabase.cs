using UnityEngine;

[CreateAssetMenu(fileName = "MelodyDatabase", menuName = "ScriptableObjects/MelodyDatabase", order = 2)]
public class MelodyDatabase : ScriptableObject
{
    public Melody[] melodies;
}
