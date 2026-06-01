using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Word", menuName = "ScriptableObjects/Word", order = 1)]
public class Word : ScriptableObject
{
    public string wordID; //English key word representing the word. The player doesn't see this
    public LocalizedString displayName; //Localized string with the word displayed to the player 
    public WordCategory category; //Word category, for example: "Forest"'s category is "Place"
    public notesEnum[] melody; //Sequence of 4 notes, the sung pronunciation of the word
}

public enum WordCategory {Place, Music, Creatures, Other}