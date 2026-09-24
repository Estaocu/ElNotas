using System;
using UnityEngine.Localization.Metadata;

[Serializable]
public class MelodyMetadata : IMetadata
{
    public notesEnum[] notes;

    public static notesEnum[] ParseNotes(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<notesEnum>();

        string normalizedValue = value.Trim();

        notesEnum[] result =
            new notesEnum[normalizedValue.Length];

        for (int i = 0; i < normalizedValue.Length; i++)
        {
            char character = normalizedValue[i];

            switch (character)
            {
                case '1':
                    result[i] = notesEnum.Note1;
                    break;

                case '2':
                    result[i] = notesEnum.Note2;
                    break;

                case '3':
                    result[i] = notesEnum.Note3;
                    break;

                case '4':
                    result[i] = notesEnum.Note4;
                    break;

                default:
                    throw new FormatException(
                        $"Invalid melody note '{character}'. Expected digits 1-4."
                    );
            }
        }

        return result;
    }

    public string ToDigitString()
    {
        if (notes == null || notes.Length == 0)
            return string.Empty;

        char[] result = new char[notes.Length];

        for (int i = 0; i < notes.Length; i++)
        {
            result[i] = notes[i] switch
            {
                notesEnum.Note1 => '1',
                notesEnum.Note2 => '2',
                notesEnum.Note3 => '3',
                notesEnum.Note4 => '4',
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return new string(result);
    }
}