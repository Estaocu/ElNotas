using System;
using UnityEngine.Localization.Metadata;

[Serializable]
[Metadata(AllowedTypes = MetadataType.SharedStringTableEntry)]
public class WordCategoryMetadata : IMetadata
{
    public WordCategory category;
}