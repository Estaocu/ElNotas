using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem.iOS;
using UnityEngine.UI;

public class RhythmManager : MonoBehaviour
{
    public float bpm = 96f;
    public float negWindowPercent = 0.15f;
    public float posWindowPercent;
    private bool playerNoteToNextBeat = false;
    private double t; //Time between beats
    private double nextBeatTime;
    public int currentBeat;
    public int beatType; //0 = whole; 1 = Whole; 2 = quarter.
    private int beatOf16; //Beat real de los 16 que conforman una bar
    public BeatCountUIImage beatImage;
    public RectTransform needle;
    

     public static event Action<int> OnBeatChanged; //CLAVE, una campanada que emite una int de info (nº de beat) cada vez que suena

     // Pause vars
    private bool isPaused = false;
    private double pauseStartDspTime;
    public static RhythmManager Instance { get; private set; }

    // Timing expuesto para que otros scripts (ej. Singer NPC) puedan programar
    // notas con lookahead respecto al próximo subbeat.
    public double SubBeatDuration => t;
    public double NextSubBeatDspTime => nextBeatTime;
    public int CurrentSubBeat => beatOf16;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        posWindowPercent = negWindowPercent/2;
    }

    void Start()
    {
        currentBeat = 0;
        beatOf16 = 0;
        beatType = 0;
        t = 60/(bpm*4); //60 (BPM to BPS) * 4 (subBeats) / bpm
        nextBeatTime = AudioSettings.dspTime;
    }
    void Update()
    {
        if (isPaused) return;

        if (AudioSettings.dspTime >= nextBeatTime) //Si has acabado de contar hasta t
        {
            StartNextSubBeat();
            nextBeatTime += t;
        }

        if (needle != null)
    {
        // 1. Calculamos cuánto tiempo ha pasado desde el inicio del sub-beat actual (0.0 a 1.0)
        float subBeatProgress = (float)((AudioSettings.dspTime - (nextBeatTime - t)) / t);

        // 2. Calculamos el progreso total dentro del ciclo de 16 sub-beats (0.0 a 16.0)
        // Usamos (beatOf16 - 1) para que empiece en 0 al inicio del ciclo
        float totalProgress = (beatOf16 - 1) + subBeatProgress;

        // 3. Mapeamos ese progreso (0-16) a grados (0-360)
        // La fórmula es: (Progreso Actual / Total de Sub-beats) * 360
        float angle = (totalProgress / 16f) * 360f;

        // 4. Aplicamos la rotación (usamos ángulo negativo para rotación horaria)
        needle.localRotation = Quaternion.Euler(0, 0, -angle);
    }
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        pauseStartDspTime = AudioSettings.dspTime;
    }

    public void Resume()
    {
        if (!isPaused) return;
        double pausedDuration = AudioSettings.dspTime - pauseStartDspTime;
        nextBeatTime += pausedDuration;
        isPaused = false;
    }
        void StartNextSubBeat()
    {
        beatOf16++;
        if (beatOf16 >= 17) beatOf16 = 1;
        currentBeat = (beatOf16 - 1) / 4;

        DetermineBeatType(beatOf16);

        // if (beatImage != null) beatImage.SwapSubBeatImage(beatOf16);

        if (beatType == 0 && beatImage != null) beatImage.SwapImage(currentBeat);
        int subNoteIndex = (beatOf16 - 1) % 4;
        if (beatImage != null) beatImage.SwapSubNoteImage(subNoteIndex);

        // Debug.Log("BEAT " + currentBeat + " |  Sub: " + beatOf16);

        OnBeatChanged?.Invoke(beatOf16); //Emit signal to every subscriber with subbeat number
    }

    void DetermineBeatType(int beatNumber)
    {
        int n = beatNumber - 1;

        if (n % 4 == 0) beatType = 0; // Whole

        else if (n % 2 == 0) beatType = 1; // Half
    
        else beatType = 2; // Quarter
    }
}
