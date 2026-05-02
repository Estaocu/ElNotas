using System.Collections;
using System.Collections.Generic;
using CMF;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public CameraManager CameraManager { get; private set; }
    public PauseManager PauseManager { get; private set; }
    public ActionMapsManager ActionMapsManager { get; private set; }
    public RhythmManager RhythmManager { get; private set; }
    public RhythmTimingValidator RhythmTimingValidator { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CameraManager = GetComponent<CameraManager>();
            PauseManager = GetComponent<PauseManager>();
            ActionMapsManager = GetComponent<ActionMapsManager>();
            RhythmManager = GetComponent<RhythmManager>();
            RhythmTimingValidator = GetComponent<RhythmTimingValidator>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
