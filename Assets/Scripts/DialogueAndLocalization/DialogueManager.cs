using System.Collections;
using CMF;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class DialogueManager : MonoBehaviour
{
    public bool canInteract = false;
    public NPC currentNPC;
    
    [SerializeField] private ConversationManager conversation;
    public GameObject textCanvas;
    [SerializeField] private DialogueBehaviour trigger; 

    private bool flagConv;

    private BubblePosition bubblePosition;
    private ScaleInOut bubbleScale;

    private void Awake()
    {
        if (textCanvas == null) return;

        bubblePosition = textCanvas.GetComponentInChildren<BubblePosition>(true);
        //bubbleScale = textCanvas.GetComponentInChildren<ScaleInOut>(true);

        if (bubblePosition == null) Debug.LogError("DialogueManager: no BubblePosition found in textCanvas.", textCanvas);
        //if (bubbleScale == null) Debug.LogError("DialogueManager: no ScaleInOut found in textCanvas.", textCanvas);
    }

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
    
        // Ensure the canvas object is active before feeding text to TextMeshPro
        if (textCanvas != null)
        {
            textCanvas.SetActive(true);
        }

        MoveBubble(1);
        //ToggleTextCanvas(true);
        ActionMapsManager.SetTextInput();
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
        MoveBubble(1);
        EnterDialogue();
        ActionMapsManager.SetTextInput();
    }

    public void MoveBubble(int position)
    {
        switch (position)
            {
                case 1: bubblePosition.ApplyPositionA(); break;
                case 2: bubblePosition.ApplyPositionB(); break;
            }
    }

    public void ToggleTextCanvas(bool desiredState)
    {
        if (bubbleScale == null) return;

        if (!desiredState)
        {
            bubbleScale.scaleOut();
            return;
        }
            void show()
            {
            bubbleScale.scaleIn();
            }

        // If the bubble is visible or still scaling, hide it first and move it once it's gone
        if (bubbleScale.isVisible || bubbleScale.isAnimating) bubbleScale.scaleOut(show);
        else show();
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
                        MoveBubble(2);
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
                ActionMapsManager.SetPlayerInput();
                RemoveCurrentNPC(currentNPC);
                trigger.inConv = false;
                flagConv = false;

                textCanvas.SetActive(false);
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
        ActionMapsManager.SetConversationInput();
    }
}