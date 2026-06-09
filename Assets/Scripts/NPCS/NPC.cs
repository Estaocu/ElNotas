using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueMode {Intro, Monologue, Answer, Question};
public class NPC : MonoBehaviour
{
    public string npcName;
    public DialogueMode startMode;
    // float text speed
    // variables para customizar el flavor del texto
    [SerializeField] private DialogueManager manager;

    [SerializeField] private SphereCollider playerDetection;

    void OnTriggerEnter(Collider other)
    {
        manager.SetNewNPC(this);
    }

    void OnTriggerExit(Collider other)
    {
        manager.RemoveCurrentNPC(this);
    }








}
