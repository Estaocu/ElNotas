using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TotalWordList", menuName = "ScriptableObjects/Total Word List", order = 2)]
public class TotalWordList : ScriptableObject
{
    public List<Word> wordList = new List<Word>();

    [ListToGroup("category")]
    public List<CategoryGroup> categories = new List<CategoryGroup>();

    // Public getters to access data without exposing raw lists to accidental inspector modifications
    public IReadOnlyList<Word> TotalWords => wordList;
    public IReadOnlyList<CategoryGroup> CategorizedWords => categories;

    [Serializable]
    public class CategoryGroup
    {
        public WordCategory category;
        public List<Word> words = new List<Word>();

        public CategoryGroup(WordCategory category)
        {
            this.category = category;
            this.words = new List<Word>();
        }
    }

    private void OnValidate()
    {
        // 1. Clean and sort main list in-place (avoids allocating new lists if not needed)
        wordList.RemoveAll(word => word == null);
        wordList = wordList.OrderBy(word => word.wordID).ToList();

        // 2. Sync category groups without destroying existing instances
        SyncCategories();
    }

    private void SyncCategories()
    {
        // Detect current active categories in the main list
        var activeCategories = wordList.Select(w => w.category).Distinct().ToList();

        // Remove categories that are no longer present
        categories.RemoveAll(group => !activeCategories.Contains(group.category));

        // Update or add categories
        foreach (var category in activeCategories)
        {
            var group = categories.FirstOrDefault(g => g.category == category);
            if (group == null)
            {
                group = new CategoryGroup(category);
                categories.Add(group);
            }

            // Get and sort words for this specific category
            group.words = wordList
                .Where(w => w.category == category)
                .OrderBy(w => w.wordID)
                .ToList();
        }

        // Sort the outer category list by enum order
        categories = categories.OrderBy(g => g.category).ToList();
    }

    public int GetCategoryLength(WordCategory category)
    {
        var group = categories.FirstOrDefault(g => g.category == category);
        return group != null ? group.words.Count : 0;
    }

    public IReadOnlyList<Word> GetWordsByCategory(WordCategory category)
    {
        var group = categories.FirstOrDefault(g => g.category == category);
        if (group == null) return new List<Word>();

        return group.words
            .OrderBy(w => w.displayName.GetLocalizedString(), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}





public class ListToGroupAttribute : PropertyAttribute
{
    public string propertyName;
    public ListToGroupAttribute(string propertyName) => this.propertyName = propertyName;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ListToGroupAttribute))]
public class ListToGroupDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        try
        {
            ListToGroupAttribute listToGroup = (ListToGroupAttribute)attribute;
            SerializedProperty path = property.FindPropertyRelative(listToGroup.propertyName);
            
            if (path != null)
            {
                label.text = path.enumDisplayNames[path.enumValueIndex];
            }
        }
        catch
        {
            // Fallback
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif