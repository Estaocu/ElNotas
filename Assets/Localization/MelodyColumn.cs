using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine.Localization.Tables;

public class MelodyColumn : KeyMetadataColumn<MelodyMetadata>
{
    public override PushFields PushFields =>
        PushFields.Value;

    public override void PushHeader(
        StringTableCollection collection,
        out string header,
        out string headerNote)
    {
        header = "Melody";
        headerNote = null;
    }

    public override void PullMetadata(
        SharedTableData.SharedTableEntry keyEntry,
        MelodyMetadata metadata,
        string cellValue,
        string cellNote)
    {
        if (string.IsNullOrEmpty(cellValue))
        {
            if (metadata != null)
                keyEntry.Metadata.RemoveMetadata(metadata);

            return;
        }

        if (metadata == null)
        {
            metadata = new MelodyMetadata();
            keyEntry.Metadata.AddMetadata(metadata);
        }

        metadata.notes =
            MelodyMetadata.ParseNotes(cellValue);
    }

    public override void PushMetadata(
        MelodyMetadata metadata,
        out string value,
        out string note)
    {
        value =
            metadata != null
                ? metadata.ToDigitString()
                : string.Empty;

        note = null;
    }
}