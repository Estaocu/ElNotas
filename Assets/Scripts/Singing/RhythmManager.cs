using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;


public enum BeatNote{Quarter, Eighth};

public class RhythmManager : MonoBehaviour
{
    [Header("Beat Settings")]
    public float bpm = 96;
    [HideInInspector] public float negWin = 0.15f; // Percentage
    [HideInInspector] public float posWin;         // Percentage
    //[HorizontalLine(color: EColor.Red)]

    private bool playerNoteToNextBeat = false;
    private double t; //Time between beats
    private double nextBeatTime;

    [Header("Current Rhythm Info")]
    public int current8 = 0; //Corchea actual de las 8 que conforman la var
    public BeatNote beatType; 

    [Header("References")]
    public BeatCountUIImage beatImage;
    public RectTransform needle;
    public TextMeshProUGUI text8;

    public static event Action<int> OnBeatChanged; //CLAVE, una campanada que emite una int de info (nº de beat) cada vez que suena

     // Pause vars
    private bool isPaused = false;
    private double pauseStartDspTime;
    public static RhythmManager Instance { get; private set; }

    // Timing expuesto para que otros scripts (ej. Singer NPC) puedan programar
    // notas con lookahead respecto al próximo subbeat.
    public double SubBeatDuration => t;
    public double NextSubBeatDspTime => nextBeatTime;
    public int CurrentSubBeat => current8;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        posWin = negWin/2;
    }

    void Start()
    {
        current8 = 0;
        beatType = BeatNote.Quarter;
        text8.SetText(current8.ToString());
        
        t = 30/bpm;
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
    }

    void StartNextSubBeat()
    {
        current8++;
        if (current8 >= 8) current8 = 0;
        

        DetermineBeatType();
        OnBeatChanged?.Invoke(current8); //Emit signal to every subscriber with eight note index
        text8.SetText(current8.ToString());
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
        
    void OnDestroy()
    {
        if (Instance == this)
        {
            RhythmBeatWaiter.CancelAll();
            Instance = null;
        }
    }


    private void DetermineBeatType()
    {
        int n = current8 + 1;
        if (n % 2 == 0) {beatType = BeatNote.Eighth; return;}
        else beatType = BeatNote.Quarter;
        
    }
}
