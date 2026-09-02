using UnityEngine;
using UnityEngine.Localization;

public enum EntryType {Word, Melody}
public enum PageSize {Half, One, Two} 


[CreateAssetMenu(fileName = "NotebookEntry", menuName = "ScriptableObjects/Notebook Entry", order = 1)]
public class NotebookEntry : ScriptableObject
{
    public LocalizedString id;
    public EntryType type;
    public notesEnum[] melody;
    public GameObject visual;
    public PageSize size;

}