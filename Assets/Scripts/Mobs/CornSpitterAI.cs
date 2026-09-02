using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpitterState
{
    Idle,
    Iniciativa,
    Respuesta,
    Dead
}

public enum DeflectMode
{
    Homing,
    Linear
}

public enum SpitterNoteResolution
{
    Eighth,
    Quarter,
    Both
}

public class CornSpitterAI : MonoBehaviour
{
    private IControllableProjectile projectile;

    [Header("Health")]
    [SerializeField] private int maxHp;
    public int currentHp;

    [Header("References")]
    [SerializeField] private AddNotesOnBeat beatSinger;
    [SerializeField] private GameObject cornPrefab;
    [SerializeField] private Transform cornSpawnpoint;
    [SerializeField] private TriggerDetector cornTrigger;
    [SerializeField] private TriggerDetector playerTrigger;
    [SerializeField] private RhythmClock rhythmClock;

    [Header("Singing Rhythm")]
    [SerializeField]
    private SpitterNoteResolution noteResolution =
        SpitterNoteResolution.Both;

    [SerializeField, Min(0)]
    private int silenceBetweenNotes = 0;

    [Header("Raycast Confirmation Settings")]
    [SerializeField] private Transform raycastStartPoint;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float confirmationDuration = 1.5f;
    private bool isPlayerInsideTrigger;

    public notesEnum[] melodyToCopy =
        new notesEnum[2];

    private SpitterState state =
        SpitterState.Idle;

    private SpitterState interruptResumeAfter =
        SpitterState.Idle;

    private DeflectMode deflectMode =
        DeflectMode.Homing;

    private HomingProjectile currentEngagement;

    private readonly HashSet<HomingProjectile>
        incomingCorns = new();

    private bool cornSpawned;
    private bool cornAlreadyRead;

    private NoteSlot[] originalPattern;

    private Coroutine detectionRoutine;

    private bool isCurrentlyTrackingPlayer;

    // The melody chosen for the currently active corn.
    private notesEnum[] currentCombatMelody =
        new notesEnum[2];

    // True while the current corn combat owns a melody.
    private bool hasCombatMelody;

    public SpitterState State =>
        state;

    private void Awake()
    {
        if (rhythmClock == null)
        {
            rhythmClock =
                FindFirstObjectByType<RhythmClock>();
        }

        if (beatSinger != null &&
            beatSinger.pattern != null)
        {
            originalPattern =
                (NoteSlot[])beatSinger.pattern.Clone();
        }

        if (currentHp <= 0)
        {
            currentHp = maxHp;
        }
    }

    public void RegisterIncomingCorn(
        HomingProjectile proj)
    {
        if (proj != null)
        {
            incomingCorns.Add(proj);
        }
    }

    public void UnregisterIncomingCorn(
        HomingProjectile proj)
    {
        incomingCorns.Remove(proj);
    }

    public void OnPlayerDetected()
    {

        incomingCorns.RemoveWhere(
            item => item == null
        );

        if (state != SpitterState.Idle)
            return;

        if (incomingCorns.Count > 0)
            return;

        if (cornSpawned)
            return;

        if (playerTrigger == null ||
            playerTrigger.LastDetected == null)
        {
            return;
        }

        if (isCurrentlyTrackingPlayer)
            return;

        Collider col =
            playerTrigger.LastDetected
                .GetComponent<Collider>();

        if (col == null)
            return;

        isPlayerInsideTrigger = true;

        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }

        detectionRoutine = StartCoroutine(
            TrackAndConfirmPlayerRoutine(col, trackedPlayer)
        );
    }

    public void OnPlayerLost()
    {
        isPlayerInsideTrigger = false;

        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }

        isCurrentlyTrackingPlayer = false;
    }

    private IEnumerator TrackAndConfirmPlayerRoutine(Collider playerCol)
    {
        isCurrentlyTrackingPlayer = true;

        float elapsed = 0f;

        // Evaluamos si el jugador sigue en el trigger y si el collider es válido
        while (isPlayerInsideTrigger && playerCol != null)
        {
            Vector3 startPos = raycastStartPoint != null
                ? raycastStartPoint.position
                : transform.position;

            Vector3 targetPos = playerCol.bounds.center;
            Vector3 direction = targetPos - startPos;
            float distance = direction.magnitude;

            /*
             * Si por alguna razón el objetivo está exactamente
             * en el punto de origen, no intentamos normalizar
             * un vector de longitud cero.
             */
            if (distance <= Mathf.Epsilon)
            {
                elapsed += Time.deltaTime;

                if (elapsed >= confirmationDuration)
                    break;

                yield return null;
                continue;
            }

            bool isLineOfSightBlocked = false;

            /*
             * RAYCAST
             *
             * Este es el único sitio donde se ejecuta el Raycast
             * de confirmación.
             */
            if (Physics.Raycast(
                startPos,
                direction.normalized,
                out RaycastHit hit,
                distance,
                obstacleLayerMask))
            {
                if (hit.collider.transform.root != playerCol.transform.root)
                {
                    isLineOfSightBlocked = true;

                    Debug.DrawLine(startPos, playerCol.bounds.center, Color.red);
                }
            }

            if (isLineOfSightBlocked)
            {
                /*
                 * Si hay un obstáculo, la confirmación empieza
                 * de nuevo desde cero.
                 */
                elapsed = 0f;
            }
            else
            {
                elapsed += Time.deltaTime;

                Debug.DrawLine(startPos, playerCol.bounds.center, Color.green);

                if (elapsed >= confirmationDuration)
                {
                    Debug.Log("DONE!");
                    break;
                }
            }

            yield return null;
        }

        // Si ha salido del bucle porque la línea de visión falló o el jugador salió del trigger
        if (isPlayerInsideTrigger && state == SpitterState.Idle && !cornSpawned && incomingCorns.Count == 0)
        {
            state = SpitterState.Iniciativa;
            LaunchCorn(playerCol);
        }

        isCurrentlyTrackingPlayer = false;
        detectionRoutine = null;

        if (trackedPlayer == targetPlayer)
            trackedPlayer = null;
    }

    public void OnCornDetected()
    {
        if (state == SpitterState.Dead)
            return;

        if (cornTrigger == null ||
            cornTrigger.LastDetected == null)
        {
            return;
        }

        HomingProjectile proj =
            cornTrigger.LastDetected
                .GetComponent<HomingProjectile>();

        if (proj == null)
            return;

        if (proj.mobIsOgSender &&
            proj.LaunchesNumber == 0)
        {
            return;
        }

        if (proj.Enemy != gameObject)
            return;

        if (currentHp <= 0)
        {
            if (beatSinger != null)
            {
                beatSinger.StopSinging();
            }

            return;
        }

        bool ownBounce =
            proj.mobIsOgSender;

        if (state == SpitterState.Iniciativa &&
            !ownBounce)
        {
            interruptResumeAfter =
                SpitterState.Iniciativa;

            state =
                SpitterState.Respuesta;

            deflectMode =
                DeflectMode.Linear;

            SingleNotesListener listener =
                proj.GetComponentInChildren<
                    SingleNotesListener>(true);

            if (listener != null)
            {
                ReadCornMelody(listener);
            }

            currentEngagement = proj;

            SingForCorn();

            return;
        }

        if (ownBounce)
        {
            state =
                SpitterState.Respuesta;

            deflectMode =
                DeflectMode.Homing;

            currentEngagement = proj;

            SingForCorn();

            return;
        }

        state =
            SpitterState.Respuesta;

        deflectMode =
            DeflectMode.Homing;

        SingleNotesListener cornListener =
            proj.GetComponentInChildren<
                SingleNotesListener>(true);

        if (cornListener != null)
        {
            ReadCornMelody(cornListener);
        }

        currentEngagement = proj;

        SingForCorn();
    }

    public void OnDeflectionConfirmed(
        HomingProjectile proj)
    {
        if (state != SpitterState.Respuesta)
            return;

        if (currentEngagement != proj ||
            proj == null)
        {
            return;
        }

        if (deflectMode == DeflectMode.Linear)
        {
            proj.LaunchLinear(gameObject);
        }
        else
        {
            proj.Launch(gameObject);
        }

        currentEngagement = null;

        if (interruptResumeAfter ==
            SpitterState.Iniciativa)
        {
            RestorePattern();

            interruptResumeAfter =
                SpitterState.Idle;

            state =
                SpitterState.Iniciativa;
        }
        else
        {
            state =
                SpitterState.Idle;
        }
    }

    public void OnEngagementCornExploded(
        HomingProjectile proj)
    {
        if (state == SpitterState.Dead)
            return;

        incomingCorns.Remove(proj);

        currentEngagement = null;

        cornSpawned = false;
        cornAlreadyRead = false;

        // The melody belongs to the old corn.
        // It must disappear only when that corn is gone.
        hasCombatMelody = false;

        interruptResumeAfter =
            SpitterState.Idle;

        state =
            SpitterState.Idle;

        RestorePattern();

        if (playerTrigger != null &&
            playerTrigger.LastDetected != null)
        {
            OnPlayerDetected();
        }
    }

    public void TakeDirectHit()
    {
        Die();
    }

    public void LaunchCorn(
        Collider collider)
    {
        if (cornSpawned)
            return;

        cornSpawned = true;

        GameObject myCorn =
            PoolManager.Instance.GetObject(
                cornPrefab
            );

        if (myCorn == null)
        {
            cornSpawned = false;
            return;
        }

        myCorn.transform.position =
            cornSpawnpoint.position;

        myCorn.transform.rotation =
            cornSpawnpoint.rotation;

        HomingProjectile script =
            myCorn.GetComponent<HomingProjectile>();

        if (script == null)
        {
            cornSpawned = false;
            return;
        }

        script.player =
            collider.gameObject;

        script.mobIsOgSender = true;
        script.enemy = gameObject;

        // A new melody is generated only when a new corn is created.
        GenerateNewCombatMelody();

        // The corn is initially quiet.
        // These two notes are scheduled independently.
        ScheduleCombatMelody();
    }

    private void GenerateNewCombatMelody()
    {
        notesEnum firstNote =
            GetRandomNote();

        notesEnum secondNote =
            GetRandomDifferentNote(
                firstNote
            );

        currentCombatMelody[0] =
            firstNote;

        currentCombatMelody[1] =
            secondNote;

        hasCombatMelody = true;
    }

    private void ScheduleCombatMelody()
    {
        if (!hasCombatMelody)
            return;

        if (beatSinger == null ||
            rhythmClock == null ||
            !rhythmClock.IsRunning ||
            rhythmClock.IsPaused)
        {
            return;
        }

        long firstAbsoluteSubBeat =
            GetNextValidStartSubBeat();

        long secondAbsoluteSubBeat =
            firstAbsoluteSubBeat +
            silenceBetweenNotes +
            1;

        beatSinger.ScheduleTwoNotesAtAbsoluteSubBeats(
            currentCombatMelody[0],
            firstAbsoluteSubBeat,
            currentCombatMelody[1],
            secondAbsoluteSubBeat
        );
    }

    private long GetNextValidStartSubBeat()
    {
        if (rhythmClock == null)
            return 0;

        double elapsed =
            AudioSettings.dspTime -
            rhythmClock.StartDspTime;

        if (elapsed < 0.0)
            elapsed = 0.0;

        double subBeatDuration =
            rhythmClock.SubBeatDuration;

        if (subBeatDuration <= 0.0)
            return 0;

        long candidate =
            (long)System.Math.Ceiling(
                elapsed /
                subBeatDuration
            );

        while (!IsValidStartSubBeat(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private bool IsValidStartSubBeat(
        long absoluteSubBeat)
    {
        int localSubBeat =
            (int)(
                absoluteSubBeat %
                RhythmClock.SubBeatsPerBar
            );

        switch (noteResolution)
        {
            case SpitterNoteResolution.Eighth:
                return true;

            case SpitterNoteResolution.Quarter:
                return
                    localSubBeat %
                    RhythmClock.SubBeatsPerBeat == 0;

            case SpitterNoteResolution.Both:
                return true;
        }

        return false;
    }

    private notesEnum GetRandomNote()
    {
        return
            (notesEnum)Random.Range(
                0,
                4
            );
    }

    private notesEnum GetRandomDifferentNote(
        notesEnum firstNote)
    {
        notesEnum secondNote;

        do
        {
            secondNote =
                (notesEnum)Random.Range(
                    0,
                    4
                );

        } while (
            secondNote == firstNote
        );

        return secondNote;
    }

    public void ReadCornMelody(
        SingleNotesListener corn)
    {
        if (corn == null)
            return;

        if (corn.desiredMelody == null ||
            corn.desiredMelody.Length < 2)
        {
            return;
        }

        if (cornAlreadyRead)
            return;

        melodyToCopy =
            (notesEnum[])corn.desiredMelody.Clone();

        cornAlreadyRead = true;
    }

    public void SingForCorn()
    {
        if (currentHp <= 0)
            return;

        currentHp--;

        if (!hasCombatMelody)
            return;

        ScheduleCombatMelody();
    }

    private void RestorePattern()
    {
        if (originalPattern == null ||
            beatSinger == null ||
            beatSinger.pattern == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                originalPattern.Length,
                beatSinger.pattern.Length
            );

        System.Array.Copy(
            originalPattern,
            beatSinger.pattern,
            count
        );

        cornAlreadyRead = false;
    }

    private void Die()
    {
        if (detectionRoutine != null)
        {
            StopCoroutine(
                detectionRoutine
            );

            detectionRoutine = null;
        }

        state =
            SpitterState.Dead;

        hasCombatMelody = false;

        if (beatSinger != null)
        {
            beatSinger.StopSinging();
        }

        Destroy(gameObject);
    }
}