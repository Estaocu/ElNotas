using UnityEngine;
using UnityEngine.Localization.Components;
using Febucci.TextAnimatorForUnity;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    private LocalizeStringEvent localizeStringEvent;
    private TypewriterComponent typewriter;
    private InputAction noteAction;

    private string[] pages;
    private int currentPage;
    private bool waitingForInput;

    void Awake()
    {
        localizeStringEvent = GetComponent<LocalizeStringEvent>();
        typewriter = GetComponent<TypewriterComponent>();

        if (localizeStringEvent == null)
            Debug.LogError("DialogueTrigger: no se encontró LocalizeStringEvent en " + gameObject.name);
        if (typewriter == null)
            Debug.LogError("DialogueTrigger: no se encontró TypewriterComponent en " + gameObject.name);

        // Obtener la acción "Note2" del PlayerInput
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            noteAction = playerInput.actions["Note2"]; // Asegúrate que el nombre sea exacto
        else
            Debug.LogError("DialogueTrigger: no se encontró PlayerInput en la escena");
    }

    void OnEnable()
    {
        localizeStringEvent.OnUpdateString.AddListener(StartDialogue);
        typewriter.onTextShowed.AddListener(OnPageFinished);
        
        // Suscribirse a la acción "note 2"
        if (noteAction != null)
            noteAction.performed += OnNoteAction;
    }

    void OnDisable()
    {
        localizeStringEvent.OnUpdateString.RemoveListener(StartDialogue);
        typewriter.onTextShowed.RemoveListener(OnPageFinished);
        
        if (noteAction != null)
            noteAction.performed -= OnNoteAction;
    }

    void OnNoteAction(InputAction.CallbackContext context)
    {
        if (!waitingForInput)
            typewriter.SkipTypewriter();
        else
            AdvancePage();
    }

    void StartDialogue(string localizedText)
    {
        pages = localizedText.Split('|');
        currentPage = 0;
        ShowCurrentPage();
    }

    void ShowCurrentPage()
    {
        waitingForInput = false;
        typewriter.ShowText(pages[currentPage].Trim());
    }

    void OnPageFinished()
    {
        waitingForInput = true;
    }

    void AdvancePage()
    {
        currentPage++;

        if (currentPage < pages.Length)
            ShowCurrentPage();
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        waitingForInput = false;
    }
}