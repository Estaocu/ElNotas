using UnityEngine;

public class DebugLearnWordsUI : MonoBehaviour
{
    [SerializeField] private TotalWordList totalWordList;
    [SerializeField] private LearnedWords learnedWords;
    [SerializeField] private DebugWord buttonPrefab;
    [Tooltip("Parent transform under which the buttons will be spawned. If null, this transform is used.")]
    [SerializeField] private RectTransform buttonsContainer;

    [Tooltip("Clear existing children of the container before spawning, useful when re-generating at runtime.")]
    [SerializeField] private bool clearContainerOnStart = true;

    private void Start()
    {
        SpawnButtons();
    }

    [ContextMenu("Spawn Buttons")]
    public void SpawnButtons()
    {
        if (totalWordList == null)
        {
            Debug.LogError("[DebugLearnWordsUI] TotalWordList reference is missing.");
            return;
        }
        if (learnedWords == null)
        {
            Debug.LogError("[DebugLearnWordsUI] LearnedWords reference is missing.");
            return;
        }
        if (buttonPrefab == null)
        {
            Debug.LogError("[DebugLearnWordsUI] Button prefab is missing.");
            return;
        }

        Transform parent = buttonsContainer != null ? buttonsContainer : transform;

        if (clearContainerOnStart)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        foreach (Word word in totalWordList.TotalWords)
        {
            if (word == null) continue;

            DebugWord btn = Instantiate(buttonPrefab, parent);
            btn.name = $"DebugWord_{word.wordID}";
            btn.Init(word, learnedWords);
        }
    }
}
