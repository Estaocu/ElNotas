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

    // NUEVO:
    // Guarda exactamente qué objeto inició la detección.
    // No modificamos TriggerDetector; simplemente mantenemos
    // nuestro propio estado para validar la coroutine.
    private GameObject trackedPlayer;

    public SpitterState State => state;

    private void Awake()
    {
        if (beatSinger != null && beatSinger.pattern != null)
            originalPattern = (NoteSlot[])beatSinger.pattern.Clone();

        if (currentHp <= 0)
            currentHp = maxHp;
    }

    private void Update()
    {
        /*
         * Esto solamente dibuja la línea de debug.
         * No realiza ningún Raycast.
         *
         * La comprobación de que el player sigue detectado
         * también se hace aquí para evitar dibujar una línea
         * después de haber perdido el objetivo.
         */
        if (!isCurrentlyTrackingPlayer)
            return;

        if (detectionRoutine == null)
            return;

        if (playerTrigger == null)
            return;

        if (playerTrigger.LastDetected == null)
            return;

        if (trackedPlayer == null)
            return;

        if (playerTrigger.LastDetected != trackedPlayer)
            return;

        Collider col = trackedPlayer.GetComponent<Collider>();

        if (col == null)
            return;

        Vector3 startPos = raycastStartPoint != null
            ? raycastStartPoint.position
            : transform.position;

        Vector3 targetPos = col.bounds.center;

        Debug.DrawLine(startPos, targetPos, Color.red);
    }

    public void RegisterIncomingCorn(HomingProjectile proj)
    {
        if (proj != null)
            incomingCorns.Add(proj);
    }

    public void UnregisterIncomingCorn(HomingProjectile proj)
    {
        incomingCorns.Remove(proj);
    }

    public void OnPlayerDetected()
    {
        incomingCorns.RemoveWhere(item => item == null);

        if (state != SpitterState.Idle)
            return;

        if (incomingCorns.Count > 0)
            return;

        if (cornSpawned)
            return;

        if (playerTrigger == null)
            return;

        if (playerTrigger.LastDetected == null)
            return;

        if (isCurrentlyTrackingPlayer)
            return;

        /*
         * Guardamos el objeto concreto que provocó la detección.
         *
         * TriggerDetector ya se encarga de mantener LastDetected
         * correctamente. No necesitamos modificarlo.
         */
        trackedPlayer = playerTrigger.LastDetected;

        Collider col = trackedPlayer.GetComponent<Collider>();

        if (col == null)
        {
            trackedPlayer = null;
            return;
        }

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
        /*
         * IMPORTANTE:
         *
         * Invalidamos primero el objetivo.
         * Así, incluso si la coroutine estuviera en proceso
         * de reanudarse, su siguiente comprobación fallará.
         */
        trackedPlayer = null;

        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }

        isCurrentlyTrackingPlayer = false;
    }

    private IEnumerator TrackAndConfirmPlayerRoutine(
        Collider targetCollider,
        GameObject targetPlayer)
    {
        isCurrentlyTrackingPlayer = true;

        float elapsed = 0f;

        while (true)
        {
            /*
             * =====================================================
             * VALIDACIÓN PRINCIPAL
             * =====================================================
             *
             * Si el player salió del TriggerDetector:
             *
             * TriggerDetector:
             *     LastDetected = null
             *
             * Entonces esta condición falla y la coroutine termina.
             *
             * Esto es una segunda capa de seguridad además de
             * OnPlayerLost() -> StopCoroutine().
             */
            if (playerTrigger == null ||
                playerTrigger.LastDetected == null ||
                playerTrigger.LastDetected != targetPlayer ||
                trackedPlayer != targetPlayer)
            {
                isCurrentlyTrackingPlayer = false;
                detectionRoutine = null;

                if (trackedPlayer == targetPlayer)
                    trackedPlayer = null;

                yield break;
            }

            /*
             * El collider puede haber sido destruido/desactivado
             * mientras el player estaba siendo seguido.
             */
            if (targetCollider == null)
            {
                isCurrentlyTrackingPlayer = false;
                detectionRoutine = null;

                if (trackedPlayer == targetPlayer)
                    trackedPlayer = null;

                yield break;
            }

            Vector3 startPos = raycastStartPoint != null
                ? raycastStartPoint.position
                : transform.position;

            Vector3 targetPos = targetCollider.bounds.center;
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
                if (hit.collider.transform.root != targetCollider.transform.root)
                {
                    isLineOfSightBlocked = true;
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

                if (elapsed >= confirmationDuration)
                {
                    break;
                }
            }

            /*
             * Debug de la línea utilizada por el Raycast.
             */
            Debug.DrawLine(
                startPos,
                targetCollider.bounds.center,
                Color.red
            );

            yield return null;
        }

        /*
         * =====================================================
         * VALIDACIÓN FINAL
         * =====================================================
         *
         * Es importante volver a comprobar que el player sigue
         * dentro del trigger antes de lanzar la corn.
         *
         * De esta forma no puede ocurrir:
         *
         * 1. Player entra.
         * 2. Empieza la confirmación.
         * 3. Pasa el tiempo de confirmación.
         * 4. Player sale justo antes de LaunchCorn().
         * 5. Se lanza igualmente.
         */
        if (playerTrigger != null &&
            playerTrigger.LastDetected == targetPlayer &&
            trackedPlayer == targetPlayer &&
            state == SpitterState.Idle &&
            !cornSpawned &&
            incomingCorns.Count == 0)
        {
            state = SpitterState.Iniciativa;
            LaunchCorn(targetCollider);
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

        if (cornTrigger == null || cornTrigger.LastDetected == null)
            return;

        var proj = cornTrigger.LastDetected.GetComponent<HomingProjectile>();

        if (proj == null)
            return;

        if (proj.mobIsOgSender && proj.LaunchesNumber == 0)
            return;

        if (proj.Enemy != gameObject)
            return;

        // If HP is depleted, cannot sing to deflect anymore
        if (currentHp <= 0)
        {
            if (beatSinger != null)
                beatSinger.StopSinging();

            return;
        }

        bool ownBounce = proj.mobIsOgSender;

        if (state == SpitterState.Iniciativa && !ownBounce)
        {
            interruptResumeAfter = SpitterState.Iniciativa;
            state = SpitterState.Respuesta;
            deflectMode = DeflectMode.Linear;

            var listener = proj.GetComponentInChildren<SingleNotesListener>(true);

            if (listener != null)
                ReadCornMelody(listener);

            currentEngagement = proj;

            SingForCorn();
            return;
        }

        if (ownBounce)
        {
            state = SpitterState.Respuesta;
            deflectMode = DeflectMode.Homing;
            currentEngagement = proj;

            SingForCorn();
            return;
        }

        state = SpitterState.Respuesta;
        deflectMode = DeflectMode.Homing;

        var l = proj.GetComponentInChildren<SingleNotesListener>(true);

        if (l != null)
            ReadCornMelody(l);

        currentEngagement = proj;

        SingForCorn();
    }

    public void OnDeflectionConfirmed(HomingProjectile proj)
    {
        if (state != SpitterState.Respuesta)
            return;

        if (currentEngagement != proj || proj == null)
            return;

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
        if (state == SpitterState.Dead)
            return;

        incomingCorns.Remove(proj);

        state = SpitterState.Idle;

        // HP is no longer restored when projectile explodes elsewhere
        cornSpawned = false;
        cornAlreadyRead = false;
        currentEngagement = null;
        interruptResumeAfter = SpitterState.Idle;

        RestorePattern();

        /*
         * Solo intentamos volver a detectar si TriggerDetector
         * todavía tiene realmente un objeto detectado.
         *
         * Si el player salió del trigger, LastDetected será null
         * y no se reiniciará la coroutine.
         */
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

    public void LaunchCorn(Collider collider)
    {
        if (cornSpawned)
            return;

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
        if (beatSinger == null ||
            beatSinger.pattern == null ||
            beatSinger.pattern.Length < 16)
            return;

        System.Array values = System.Enum.GetValues(typeof(NoteSlot));

        for (int i = 0; i < beatSinger.pattern.Length; i++)
        {
            beatSinger.pattern[i] = NoteSlot.Empty;
        }

        NoteSlot firstNote = NoteSlot.Empty;

        while (firstNote == NoteSlot.Empty)
        {
            firstNote = (NoteSlot)values.GetValue(
                Random.Range(0, values.Length)
            );
        }

        NoteSlot secondNote = NoteSlot.Empty;

        while (secondNote == NoteSlot.Empty ||
               secondNote == firstNote)
        {
            secondNote = (NoteSlot)values.GetValue(
                Random.Range(0, values.Length)
            );
        }

        beatSinger.pattern[0] = firstNote;
        beatSinger.pattern[4] = secondNote;

        originalPattern = (NoteSlot[])beatSinger.pattern.Clone();
    }

    public void ReadCornMelody(SingleNotesListener corn)
    {
        if (cornAlreadyRead)
            return;

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
        if (currentHp <= 0)
            return;

        currentHp--;

        beatSinger.Sing();
    }

    private void RestorePattern()
    {
        if (originalPattern == null ||
            beatSinger == null ||
            beatSinger.pattern == null)
            return;

        int n = Mathf.Min(
            originalPattern.Length,
            beatSinger.pattern.Length
        );

        System.Array.Copy(
            originalPattern,
            beatSinger.pattern,
            n
        );

        cornAlreadyRead = false;
    }

    private void Die()
    {
        /*
         * Al morir, cortamos inmediatamente cualquier detección.
         */
        trackedPlayer = null;

        if (detectionRoutine != null)
        {
            StopCoroutine(detectionRoutine);
            detectionRoutine = null;
        }

        isCurrentlyTrackingPlayer = false;

        state = SpitterState.Dead;

        if (beatSinger != null)
            beatSinger.StopSinging();

        Destroy(gameObject);
    }
}