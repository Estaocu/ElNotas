using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine.Localization.Tables;

public class WordCategoryColumn : KeyMetadataColumn<WordCategoryMetadata>
{
    public override PushFields PushFields =>
        PushFields.Value;

    public override void PushHeader(
        StringTableCollection collection,
        out string header,
        out string headerNote)
    {
        header = "WordCategory";
        headerNote = null;
    }

    public override void PullMetadata(
        SharedTableData.SharedTableEntry keyEntry,
        WordCategoryMetadata metadata,
        string cellValue,
        string cellNote)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            if (metadata != null)
                keyEntry.Metadata.RemoveMetadata(metadata);

            return;
        }

        if (metadata == null)
        {
            metadata = new WordCategoryMetadata();
            keyEntry.Metadata.AddMetadata(metadata);
        }

        if (System.Enum.TryParse(
            cellValue.Trim(),
            true,
            out WordCategory category))
        {
            metadata.category = category;
        }
    }

    public override void PushMetadata(
        WordCategoryMetadata metadata,
        out string value,
        out string note)
    {
        value =
            metadata != null
                ? metadata.category.ToString()
                : string.Empty;

        note = null;
    }
}