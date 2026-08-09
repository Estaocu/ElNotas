using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueMode {Intro, Monologue, Answer, Question};
public class NPC : MonoBehaviour
{
    public string npcName;
    public DialogueMode startMode;
    [SerializeField] private DialogueManager manager;






}
