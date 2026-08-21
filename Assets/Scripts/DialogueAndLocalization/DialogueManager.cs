using System.Collections;
using CMF;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public bool canInteract = false;
    public NPC currentNPC;
    
    [SerializeField] private ConversationManager conversation;
    [SerializeField] private GameObject textCanvas;
    [SerializeField] private DialogueBehaviour trigger; 

    private bool flagConv;

    private void OnEnable()
    {
        if (trigger != null)
        {
            trigger.onEventMarkerReceived.AddListener(OnEventMarkerReceived);
        }
    }

    private void OnDisable()
    {
        if (trigger != null)
        {
            trigger.onEventMarkerReceived.RemoveListener(OnEventMarkerReceived);
        }
    }

    // Listens to DialogueTrigger events and routes them only to currentNPC
    private void OnEventMarkerReceived(string eventName, string[] parameters)
    {
        if (currentNPC != null)
        {
            currentNPC.HandleDialogueEvent(eventName, parameters);
        }
    }

    public void EnterDialogue()
    {
        if (!canInteract) return;
        
        ToggleTextCanvas(true);
        ActionMapsManager.SetActiveMaps(DefaultActionMap.Text);
        currentNPC.FindDialogue();
    }

    public void TriggerDialogue(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        EnterDialogue();
    }

    public void SetNewNPC(NPC newNpc)
    {
        currentNPC = newNpc;
        canInteract = true;

        flagConv = false;
    }

    // Called when exiting the NPC's collider
    public void RemoveCurrentNPC(NPC newNpc)
    {
        if (newNpc != null && newNpc != currentNPC) return;
        
        currentNPC = null;
        canInteract = false;
    }

    public void ExitConversation(InputAction.CallbackContext context) // Exit answer-question ui
    {
        if (!context.performed) return;

        trigger.ToggleInConv(false);
        
        conversation.SetUIActive(false);

        currentNPC.ChangeDialogueMode(DialogueMode.Goodbye);
        EnterDialogue();
        ActionMapsManager.SetActiveMaps(DefaultActionMap.Text);
    }

    public void ToggleTextCanvas(bool desiredState)
    {
        if (textCanvas == null) return;
        
        textCanvas.SetActive(desiredState);

        if (desiredState == true) ActionMapsManager.SetActiveMaps(DefaultActionMap.Conversation);

        
        
    }

    public void OnNpcDialogueEnd()
    {
        if (currentNPC == null) return;
        switch (currentNPC.currentMode)
            {
                case DialogueMode.Intro:
                currentNPC.ChangeDialogueMode(currentNPC.afterIntro);

                if(currentNPC.afterIntro == DialogueMode.Answer && !flagConv)
                    {
                        BeginConversation();
                        flagConv = true;
                        trigger.inConv = true;
                        return;

                    }

                if(currentNPC.afterIntro == DialogueMode.Question)
                {
                        //Abrir menú en el que el jugador responde a una pregunta
                }

                //Intro
                


                break;


                case DialogueMode.Monologue:
                case DialogueMode.Goodbye:
                ActionMapsManager.SetActiveMaps(DefaultActionMap.Gameplay);
                RemoveCurrentNPC(currentNPC);
                trigger.inConv = false;
                flagConv = false;

                break;  
            }
            
    }

    public void BeginConversation()
    {
        if (conversation == null)
        {
            Debug.LogError("Conversation reference is missing in DialogueManager!");
        }
        conversation.SetUIActive(true);
        ActionMapsManager.Instance.SetConversationInput();
    }

    

}