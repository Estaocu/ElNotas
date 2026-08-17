using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorCore.Typing;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;

public class DialogueBehaviour : MonoBehaviour
{
    [HideInInspector]
    [SerializeField] private LocalizedString localizedString;
    private TypewriterComponent typewriter;

    private string[] pages;
    private int currentPage;
    private bool waitingForInput;

    public TextAnimator_TMP tAnimator;
    public TextMeshProUGUI tmp;

    public UnityEvent finishDialogue;
    
    // UnityEvent to forward Text Animator messages to external scripts
    public UnityEvent<string, string[]> onEventMarkerReceived;

    // Queue of tags to be appended to the last page of the next dialogue string
    private readonly List<string> pendingEndTags = new List<string>();
    
    // Structure to store pending end actions for guaranteed execution on skip
    private readonly List<(string name, string[] parameters)> pendingEndEvents = new List<(string, string[])>();

    // Queue of callbacks to be executed precisely when the player closes the last dialogue page
    private readonly List<Action> pendingCloseCallbacks = new List<Action>();

    private void Awake()
    {
        typewriter = GetComponent<TypewriterComponent>();

        if (typewriter == null)
            Debug.LogError($"DialogueTrigger: TypewriterComponent not found on {gameObject.name}");
    }

    private void OnEnable()
    {
        if (localizedString != null)
            localizedString.StringChanged += StartDialogue;

        if (typewriter != null)
        {
            typewriter.onTextShowed.AddListener(OnPageFinished);
            typewriter.onMessage.AddListener(OnMessageReceived);
        }
    }

    private void OnDisable()
    {
        if (localizedString != null)
            localizedString.StringChanged -= StartDialogue;

        if (typewriter != null)
        {
            typewriter.onTextShowed.RemoveListener(OnPageFinished);
            typewriter.onMessage.RemoveListener(OnMessageReceived);
        }
    }

    public void AddEndTagToNextDialogue(string eventName, params string[] parameters)
    {
        string tag = TextAnimatorTagUtility.BuildEventTag(eventName, parameters);
        if (!string.IsNullOrEmpty(tag))
        {
            pendingEndTags.Add(tag);
            pendingEndEvents.Add((eventName, parameters));
        }
    }

    // Register an action to be executed when the dialogue is closed by the player
    public void AddCloseCallbackToNextDialogue(Action callback)
    {
        if (callback != null)
        {
            pendingCloseCallbacks.Add(callback);
        }
    }

    public void OnAcceptAction(InputAction.CallbackContext context)
    {
        if (!context.performed || pages == null) return;

        if (!waitingForInput)
        {
            typewriter.SkipTypewriter();
            // Ensures end events are triggered even if TextAnimator skips the marker
            ExecutePendingEndEvents();
        }
        else
        {
            AdvancePage();
        }
    }

    public void StartDialogue(string localizedText)
    {
        if (string.IsNullOrEmpty(localizedText)) return;

        pages = localizedText.Split('|');

        if (pendingEndTags.Count > 0 && pages.Length > 0)
        {
            int lastPageIndex = pages.Length - 1;
            foreach (string tag in pendingEndTags)
            {
                pages[lastPageIndex] += tag;
            }
            pendingEndTags.Clear();
        }

        currentPage = 0;
        ShowCurrentPage();    
    }

    private void ShowCurrentPage()
    {
        if (pages == null || currentPage >= pages.Length) return;

        waitingForInput = false;
        string formattedText = pages[currentPage].Trim();
        
        typewriter.ShowText(formattedText);
        tAnimator.SetText(formattedText);
        typewriter.StartShowingText(true);
    }

    private void OnPageFinished()
    {
        waitingForInput = true;
        // If we reached the last page and it finished naturally, flush any pending end events
        if (pages != null && currentPage == pages.Length - 1)
        {
            ExecutePendingEndEvents();
        }
    }

    private void OnMessageReceived(EventMarker marker)
    {
        // Remove from pending list if received naturally through TextAnimator
        pendingEndEvents.RemoveAll(e => e.name.Equals(marker.name, StringComparison.OrdinalIgnoreCase));
        onEventMarkerReceived?.Invoke(marker.name, marker.parameters);
    }

    private void ExecutePendingEndEvents()
    {
        // Executes any end event that hasn't been triggered yet (e.g. during skip)
        if (pages != null && currentPage == pages.Length - 1 && pendingEndEvents.Count > 0)
        {
            for (int i = pendingEndEvents.Count - 1; i >= 0; i--)
            {
                var evt = pendingEndEvents[i];
                onEventMarkerReceived?.Invoke(evt.name, evt.parameters);
            }
            pendingEndEvents.Clear();
        }
    }

    private void AdvancePage()
    {
        if (pages == null || pages.Length == 0) return;

        currentPage++;

        if (currentPage < pages.Length)
            ShowCurrentPage();
        else
            EndDialogue();
    }

    public void RestartText()
    {   
        tAnimator.SetText(tmp.text);
        typewriter.StartShowingText(true);
    }

    private void EndDialogue()
    {
        ExecutePendingEndEvents();

        // Execute all registered close callbacks and clear the queue
        foreach (Action callback in pendingCloseCallbacks)
        {
            callback?.Invoke();
        }
        pendingCloseCallbacks.Clear();

        waitingForInput = true;
        finishDialogue?.Invoke(); 
    }

    
    
}