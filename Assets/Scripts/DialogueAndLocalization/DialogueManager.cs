using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CMF;

public class DialogueManager : MonoBehaviour
{
    public bool canInteract = false;
    public NPC currentNPC;
    [SerializeField] private DictionaryUIManager dialogueUI;

    public void EnterDialogue(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!canInteract) return;
            dialogueUI.gameObject.SetActive(true);
            dialogueUI.currentNpc = currentNPC;
            RemoveCurrentNPC(currentNPC);
            ActionMapsManager.SetActiveMaps(DefaultActionMap.Conversation);
        }
    }

    public void SetNewNPC(NPC newNpc)
    {
        currentNPC = newNpc;
        canInteract = true;
    }

    public void RemoveCurrentNPC(NPC newNpc) //Al exitear collider de npc
    {
        if (newNpc != null && newNpc !=currentNPC) return;
        currentNPC = null;
        canInteract = false;
    }

    public void ExitDialogue(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            dialogueUI.gameObject.SetActive(false);
            RemoveCurrentNPC(currentNPC);
            ActionMapsManager.SetActiveMaps(DefaultActionMap.Gameplay);
        }
    }
}
