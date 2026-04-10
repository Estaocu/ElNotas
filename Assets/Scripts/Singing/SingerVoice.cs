using UnityEngine;

public class SingerVoice : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [Header("Note Sounds")]
    [SerializeField] private AudioClip note1;
    [SerializeField] private AudioClip note2;
    [SerializeField] private AudioClip note3;
    [SerializeField] private AudioClip note4;

    public void PlayNote(notesEnum note)
    {
        AudioClip clip = GetClip(note);
        if (clip == null || audioSource == null) return;

        audioSource.PlayOneShot(clip);
    }

    private AudioClip GetClip(notesEnum note)
    {
        return note switch
        {
            notesEnum.Note1 => note1,
            notesEnum.Note2 => note2,
            notesEnum.Note3 => note3,
            notesEnum.Note4 => note4,
            _ => null
        };
    }
}
