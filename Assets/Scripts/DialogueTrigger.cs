using UnityEngine;
using UnityEngine.Localization;
using Febucci.TextAnimatorForUnity;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [HideInInspector]
    [SerializeField] private LocalizedString localizedString;
    private TypewriterComponent typewriter;

    private string[] pages;
    private int currentPage;
    private bool waitingForInput;

    public TextAnimator_TMP tAnimator;

    public UnityEvent finishDialogue;

    public TextMeshProUGUI tmp;

    void Awake()
    {
        typewriter = GetComponent<TypewriterComponent>();

        if (typewriter == null)
            Debug.LogError("DialogueTrigger: no se encontró TypewriterComponent en " + gameObject.name);
    }

    void OnEnable()
    {
        if (localizedString != null)
            localizedString.StringChanged += StartDialogue;

        if (typewriter != null)
            typewriter.onTextShowed.AddListener(OnPageFinished);
    }

    void OnDisable()
    {
        if (localizedString != null)
            localizedString.StringChanged -= StartDialogue;

        if (typewriter != null)
            typewriter.onTextShowed.RemoveListener(OnPageFinished);
    }

    public void OnAcceptAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Detected Accept key");
            if (pages == null) return;

            if (!waitingForInput)
            {
                typewriter.SkipTypewriter();
                Debug.Log("Skip Typewriter");
            }
            else
            {
                AdvancePage();
                Debug.Log("Advance Page");
            }
        }
    }

    public void StartDialogue(string localizedText)
    {
        if (string.IsNullOrEmpty(localizedText)) return;

        pages = localizedText.Split('|');
        currentPage = 0;
        ShowCurrentPage();    
    }

    void ShowCurrentPage()
    {
        if (pages == null || currentPage >= pages.Length) return;

        waitingForInput = false;
        typewriter.ShowText(pages[currentPage].Trim());
        tAnimator.SetText(pages[currentPage].Trim());
        typewriter.StartShowingText(true);
    }

    void OnPageFinished()
    {
        waitingForInput = true;
    }

    public void AdvancePage()
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
        tAnimator.SetText(tmp.text);          // re-aplica el texto al TextAnimator
        typewriter.StartShowingText(true);    // true = empezar desde el principio
    }

    void EndDialogue()
    {
        waitingForInput = true;
        finishDialogue.Invoke(); 
        Debug.Log("End of dialogue");
    }
}