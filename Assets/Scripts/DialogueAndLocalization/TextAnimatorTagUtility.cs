using System.Text;

public static class TextAnimatorTagUtility
{
    // Generates a Febucci Text Animator event tag string
    public static string BuildEventTag(string eventName, params string[] parameters)
    {
        if (string.IsNullOrEmpty(eventName)) return string.Empty;

        if (parameters == null || parameters.Length == 0)
        {
            return $"<?{eventName}>";
        }

        return $"<?{eventName}={string.Join(",", parameters)}>";
    }

    // Appends an event tag to the end of a given string
    public static string AppendEventTag(this string text, string eventName, params string[] parameters)
    {
        string tag = BuildEventTag(eventName, parameters);
        return string.Concat(text, tag);
    }
}