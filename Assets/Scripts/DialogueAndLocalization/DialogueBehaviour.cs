using UnityEngine;
using UnityEngine.Localization;
using Febucci.TextAnimatorForUnity;
using UnityEngine.InputSystem;
using CMF;

public class DialogueBehaviour : MonoBehaviour
{
    [SerializeField] private LocalizedString dialogueLine;
    [SerializeField] private TypewriterComponent typewriter;
    [SerializeField] private GameObject textCanvas;


    private string[] pages;
    private int currentPage;
    private bool waitingForInput;

    public void SelectDialogue(LocalizedString dialogue)
    {
        dialogueLine = dialogue;
    }

    void Start()
    {
        // Conectar LocalizeString al sistema
        dialogueLine.StringChanged += StartDialogue;
    }

    void OnDestroy()
    {
        dialogueLine.StringChanged -= StartDialogue;
    }

    void StartDialogue(string localizedText)
    {
        // Separar por | en páginas
        pages = localizedText.Split('|');
        currentPage = 0;
        ShowCurrentPage();
    }

    void ShowCurrentPage()
    {
        waitingForInput = false;
        typewriter.onTextShowed.AddListener(OnPageFinished);
        typewriter.ShowText(pages[currentPage].Trim());
    }

    void OnPageFinished()
    {
        typewriter.onTextShowed.RemoveListener(OnPageFinished);
        waitingForInput = true;
    }

    // void Update()
    // {
    //     if (!waitingForInput) return;

    //     // Cambia esto al input system que uses
    //     if (Keyboard.current.spaceKey.wasPressedThisFrame || 
    //         Keyboard.current.enterKey.wasPressedThisFrame)
    //     {
    //         AdvancePage();
    //     }
    // }

    public void AdvancePage()
    {
        if (!waitingForInput) return;

        currentPage++;

        if (currentPage < pages.Length)
        {
            ShowCurrentPage();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        waitingForInput = false;
        // Aquí: ocultar UI, notificar al sistema de juego, etc.
    }

    
}