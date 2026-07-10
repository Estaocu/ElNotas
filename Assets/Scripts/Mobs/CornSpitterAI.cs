using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpitterState { Idle, Iniciativa, Respuesta, Dead }
public enum DeflectMode { Homing, Linear }

public class CornSpitterAI : MonoBehaviour
{
    private IControllableProjectile projectile;
    [SerializeField] private int maxHp;
    public int currentHp;
    [SerializeField] private AddNotesOnBeat beatSinger;
    [SerializeField] private GameObject cornPrefab;
    [SerializeField] private Transform cornSpawnpoint;
    [SerializeField] private TriggerDetector cornTrigger;
    [SerializeField] private TriggerDetector playerTrigger;

    [Header("Raycast Confirmation Settings")]
    [SerializeField] private Transform raycastStartPoint;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float confirmationDuration = 1.5f;

    public notesEnum[] melodyToCopy = new notesEnum[2];

    private SpitterState state = SpitterState.Idle;
    private SpitterState interruptResumeAfter = SpitterState.Idle;
    private DeflectMode deflectMode = DeflectMode.Homing;
    private HomingProjectile currentEngagement;
    private readonly HashSet<HomingProjectile> incomingCorns = new();

    private bool cornSpawned;
    private bool cornAlreadyRead;

    private NoteSlot[] originalPattern;
    private Coroutine detectionRoutine;
    private bool isCurrentlyTrackingPlayer = false;

    public SpitterState State => state;

    private void Awake()
    {
        if (beatSinger != null && beatSinger.pattern != null)
            originalPattern = (NoteSlot[])beatSinger.pattern.Clone();
        if (currentHp <= 0) currentHp = maxHp;
    }

    private void Update()
    {
        if (detectionRoutine != null && playerTrigger != null && playerTrigger.LastDetected != null)
        {
            var col = playerTrigger.LastDetected.GetComponent<Collider>();
            if (col != null)
            {
                Vector3 startPos = raycastStartPoint != null ? raycastStartPoint.position : transform.position;
                Vector3 targetPos = col.bounds.center;
                Debug.DrawLine(startPos, targetPos, Color.red);
            }
        }
    }

    public void RegisterIncomingCorn(HomingProjectile proj)
    {
        if (proj != null) incomingCorns.Add(proj);
    }

    public void UnregisterIncomingCorn(HomingProjectile proj)
    {
        incomingCorns.Remove(proj);
    }

    public void OnPlayerDetected()
    {
        incomingCorns.RemoveWhere(item => item == null);

        if (state != SpitterState.Idle) return;
        if (incomingCorns.Count > 0) return;
        if (cornSpawned) return;
        if (playerTrigger == null || playerTrigger.LastDetected == null) return;
        if (isCurrentlyTrackingPlayer) return;

        var col = playerTrigger.LastDetected.GetComponent<Collider>();
        if (col == null) return;

        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
        }

        detectionRoutine = StartCoroutine(TrackAndConfirmPlayerRoutine(col));
    }

    public void OnPlayerLost()
    {
        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }
        isCurrentlyTrackingPlayer = false;
    }

    private IEnumerator TrackAndConfirmPlayerRoutine(Collider targetCollider)
    {
        isCurrentlyTrackingPlayer = true;
        float elapsed = 0f;

        while (true)
        {
            if (targetCollider == null)
            {
                isCurrentlyTrackingPlayer = false;
                yield break;
            }

            Vector3 startPos = raycastStartPoint != null ? raycastStartPoint.position : transform.position;
            Vector3 targetPos = targetCollider.bounds.center;
            Vector3 direction = targetPos - startPos;
            float distance = direction.magnitude;

            bool isLineOfSightBlocked = false;

            if (Physics.Raycast(startPos, direction.normalized, out RaycastHit hit, distance, obstacleLayerMask))
            {
                if (hit.collider.transform.root != targetCollider.transform.root)
                {
                    isLineOfSightBlocked = true;
                }
            }

            if (isLineOfSightBlocked)
            {
                elapsed = 0f;
            }
            else
            {
                elapsed += Time.deltaTime;

                if (elapsed >= confirmationDuration)
                {
                    break;
                }
            }

            if (targetCollider != null)
            {
                Debug.DrawLine(startPos, targetCollider.bounds.center, Color.red);
            }

            yield return null;
        }

        if (state == SpitterState.Idle && !cornSpawned && incomingCorns.Count == 0)
        {
            state = SpitterState.Iniciativa;
            LaunchCorn(targetCollider);
        }

        isCurrentlyTrackingPlayer = false;
        detectionRoutine = null;
    }

    public void OnCornDetected()
    {
        if (state == SpitterState.Dead) return;
        if (cornTrigger == null || cornTrigger.LastDetected == null) return;

        var proj = cornTrigger.LastDetected.GetComponent<HomingProjectile>();
        if (proj == null) return;

        if (proj.mobIsOgSender && proj.LaunchesNumber == 0) return;
        if (proj.Enemy != gameObject) return;

        // BUGFIX: Si viene un proyectil enemigo y ya gastó toda su vida (hp <= 0), NO CANTA.
        // Se queda quieto para recibir el impacto físico directo del proyectil.
        if (currentHp <= 0)
        {
            if (beatSinger != null) beatSinger.StopSinging();
            return;
        }

        bool ownBounce = proj.mobIsOgSender;

        if (state == SpitterState.Iniciativa && !ownBounce)
        {
            interruptResumeAfter = SpitterState.Iniciativa;
            state = SpitterState.Respuesta;
            deflectMode = DeflectMode.Linear;
            var listener = proj.GetComponentInChildren<SingleNotesListener>(true);
            if (listener != null) ReadCornMelody(listener);
            currentEngagement = proj;
            
            // Consume 1 HP al cantar en respuesta lineal
            currentHp--;
            beatSinger.Sing();
            return;
        }

        if (ownBounce)
        {
            state = SpitterState.Respuesta;
            deflectMode = DeflectMode.Homing;
            currentEngagement = proj;
            
            // Consume 1 HP al cantar para devolver su propio rebote
            currentHp--;
            beatSinger.Sing();
            return;
        }

        state = SpitterState.Respuesta;
        deflectMode = DeflectMode.Homing;
        var l = proj.GetComponentInChildren<SingleNotesListener>(true);
        if (l != null) ReadCornMelody(l);
        currentEngagement = proj;
        
        // SingForCorn ya descuenta vida internamente
        SingForCorn();
    }

    public void OnDeflectionConfirmed(HomingProjectile proj)
    {
        if (state != SpitterState.Respuesta) return;
        if (currentEngagement != proj || proj == null) return;

        if (deflectMode == DeflectMode.Linear)
            proj.LaunchLinear(gameObject);
        else
            proj.Launch(gameObject);

        currentEngagement = null;

        if (interruptResumeAfter == SpitterState.Iniciativa)
        {
            RestorePattern();
            interruptResumeAfter = SpitterState.Idle;
            state = SpitterState.Iniciativa;
        }
        else
        {
            state = SpitterState.Idle;
        }
    }

    public void OnEngagementCornExploded(HomingProjectile proj)
    {
        // BUGFIX alternativo: Si el proyectil explotó contra el enemigo (porque hp era 0 y no cantó),
        // este es el lugar idóneo donde muere formalmente tras procesar el impacto.
        if (currentHp <= 0)
        {
            Die();
            return;
        }

        if (state == SpitterState.Dead) return;
        incomingCorns.Remove(proj);

        state = SpitterState.Idle;
        currentHp = maxHp;
        cornSpawned = false;
        cornAlreadyRead = false;
        currentEngagement = null;
        interruptResumeAfter = SpitterState.Idle;
        RestorePattern();

        if (playerTrigger != null && playerTrigger.LastDetected != null)
        {
            OnPlayerDetected();
        }
    }

    public void LaunchCorn(Collider collider)
    {
        if (cornSpawned) return;
        cornSpawned = true;

        GameObject myCorn = PoolManager.Instance.GetObject(cornPrefab);
        if (myCorn == null)
        {
            cornSpawned = false;
            return;
        }

        myCorn.transform.position = cornSpawnpoint.position;
        myCorn.transform.rotation = cornSpawnpoint.rotation; 

        HomingProjectile script = myCorn.GetComponent<HomingProjectile>();
        if (script != null)
        {
            script.player = collider.gameObject;
            script.mobIsOgSender = true;
            script.enemy = gameObject; 
            
            RegisterIncomingCorn(script); 
            
            RandomizePattern();
            
            beatSinger.Sing();
        }
    }

    private void RandomizePattern()
    {
        if (beatSinger == null || beatSinger.pattern == null || beatSinger.pattern.Length < 16) return;

        System.Array values = System.Enum.GetValues(typeof(NoteSlot));
        
        for (int i = 0; i < beatSinger.pattern.Length; i++)
        {
            beatSinger.pattern[i] = NoteSlot.Empty;
        }

        NoteSlot firstNote = NoteSlot.Empty;
        while (firstNote == NoteSlot.Empty)
        {
            firstNote = (NoteSlot)values.GetValue(Random.Range(0, values.Length));
        }

        NoteSlot secondNote = NoteSlot.Empty;
        while (secondNote == NoteSlot.Empty || secondNote == firstNote)
        {
            secondNote = (NoteSlot)values.GetValue(Random.Range(0, values.Length));
        }

        beatSinger.pattern[0] = firstNote;
        beatSinger.pattern[4] = secondNote;

        originalPattern = (NoteSlot[])beatSinger.pattern.Clone();
    }

    public void ReadCornMelody(SingleNotesListener corn)
    {
        if (cornAlreadyRead) return;
        melodyToCopy = corn.desiredMelody;
        
        for (int i = 0; i < beatSinger.pattern.Length; i++)
        {
            beatSinger.pattern[i] = NoteSlot.Empty;
        }

        beatSinger.pattern[0] = (NoteSlot)melodyToCopy[1];
        beatSinger.pattern[4] = (NoteSlot)melodyToCopy[0];

        cornAlreadyRead = true;
    }

    public void SingForCorn()
    {
        if (currentHp <= 0) return;
        currentHp--;
        beatSinger.Sing();
    }

    private void RestorePattern()
    {
        if (originalPattern == null || beatSinger == null || beatSinger.pattern == null) return;
        int n = Mathf.Min(originalPattern.Length, beatSinger.pattern.Length);
        System.Array.Copy(originalPattern, beatSinger.pattern, n);
        cornAlreadyRead = false;
    }

    private void Die()
    {
        if (detectionRoutine != null) StopCoroutine(detectionRoutine);
        state = SpitterState.Dead;
        if (beatSinger != null) beatSinger.StopSinging();
        Destroy(gameObject);
    }
}