using UnityEngine;

public class Singer : MonoBehaviour
{
    [SerializeField] private MelodyDatabase database;
    [SerializeField] private GameObject soundwavePrefab;
    [SerializeField] private Transform soundwaveSpawnpoint;

    // Opcional: solo el jugador lo necesita. NPCs dejan este campo vacío.
    [SerializeField] private Instrument instrument;

    // Opcional: si tiene voz, suena al cantar cada nota.
    [SerializeField] private SingerVoice voice;

    [SerializeField] private float cooldown = 0.5f;

    private float lastSingTime = -999f;

    private void OnEnable()
    {
        if (instrument != null)
            instrument.OnNoteAdded += OnNoteAdded;
    }

    private void OnDisable()
    {
        if (instrument != null)
            instrument.OnNoteAdded -= OnNoteAdded;
    }

    // Llamado por Instrument via evento (jugador) o directamente por lógica NPC.
    public void AddNote(notesEnum note)
    {
        if (instrument != null) return; // El jugador usa OnNoteAdded, no este método.
        // NPCs mantienen su propio buffer si usan este método directamente.
        // Por ahora los NPCs deberían implementar su propia lógica de buffer
        // y llamar a SpawnSoundwave directamente cuando corresponda.
    }

    private void OnNoteAdded(notesEnum[] sequence, int notesPlayed)
    {
        // Reproducir el sonido de la nota individual.
        if (voice != null)
            voice.PlayNote(sequence[3]);

        if (database == null || soundwavePrefab == null) return;

        foreach (var melody in database.melodies)
        {
            if (melody == null || melody.notes == null || melody.notes.Length == 0) continue;

            int n = melody.notes.Length;
            if (notesPlayed < n) continue;

            if (SequenceEndMatches(sequence, melody.notes))
            {
                TrySpawnSoundwave(melody);
                return; // Una melodía por pulsación
            }
        }
    }

    // Compara las últimas n notas del buffer circular con el array de la melodía.
    private bool SequenceEndMatches(notesEnum[] sequence, notesEnum[] melodyNotes)
    {
        int n = melodyNotes.Length;
        int startIndex = 4 - n;

        for (int i = 0; i < n; i++)
        {
            if (sequence[startIndex + i] != melodyNotes[i])
                return false;
        }
        return true;
    }

    // Llamar directamente desde lógica NPC con la melodía ya decidida.
    public void SpawnSoundwave(Melody melody, notesEnum? note = null)
    {
        if (note.HasValue && voice != null)
            voice.PlayNote(note.Value);

        TrySpawnSoundwave(melody);
    }

    private void TrySpawnSoundwave(Melody melody)
    {
        if (Time.time - lastSingTime < cooldown) return;
        lastSingTime = Time.time;

        Vector3 spawnPos = soundwaveSpawnpoint != null
            ? soundwaveSpawnpoint.position
            : transform.position;
        
        Debug.Log($"[Singer] Melodía detectada: {melody?.melodyName ?? "DEBUG"} | Spawn pos: {spawnPos}");

        var instance = Instantiate(soundwavePrefab, spawnPos, Quaternion.identity);
        var soundwave = instance.GetComponent<Soundwave>();
        if (soundwave != null)
            soundwave.myMelody = melody;
    }
}
