using UnityEngine;
using UnityEngine.Localization;
using Febucci.TextAnimatorForUnity;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private LocalizedString localizedString;
    private TypewriterComponent typewriter;
    [SerializeField] private InputAction acceptAction;

    private string[] pages;
    private int currentPage;
    private bool waitingForInput;

    public UnityEvent finishDialogue;

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
        
        if (acceptAction != null)
        {
            acceptAction.Enable();
            acceptAction.performed += OnAcceptAction;
        }
    }

    void OnDisable()
    {
        if (localizedString != null)
            localizedString.StringChanged -= StartDialogue;

        if (typewriter != null)
            typewriter.onTextShowed.RemoveListener(OnPageFinished);
        
        if (acceptAction != null)
        {
            acceptAction.performed -= OnAcceptAction;
            acceptAction.Disable();
        }
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

    void EndDialogue()
    {
        waitingForInput = true;
        finishDialogue.Invoke(); 
        Debug.Log("finished dialogue");
    }
}