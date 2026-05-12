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
    public notesEnum[] melodyToCopy = new notesEnum[2];

    private SpitterState state = SpitterState.Idle;
    private SpitterState interruptResumeAfter = SpitterState.Idle;
    private DeflectMode deflectMode = DeflectMode.Homing;
    private HomingProjectile currentEngagement;
    private readonly HashSet<HomingProjectile> incomingCorns = new();

    private bool cornSpawned;
    private bool cornAlreadyRead;

    private NoteSlot[] originalPattern;

    public SpitterState State => state;

    private void Awake()
    {
        if (beatSinger != null && beatSinger.pattern != null)
            originalPattern = (NoteSlot[])beatSinger.pattern.Clone();
        if (currentHp <= 0) currentHp = maxHp;
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
        if (state != SpitterState.Idle) return;
        if (incomingCorns.Count > 0) return;
        if (cornSpawned) return;
        if (playerTrigger == null || playerTrigger.LastDetected == null) return;

        var col = playerTrigger.LastDetected.GetComponent<Collider>();
        if (col == null) return;

        state = SpitterState.Iniciativa;
        LaunchCorn(col);
    }

    public void OnCornDetected()
    {
        if (state == SpitterState.Dead) return;
        if (cornTrigger == null || cornTrigger.LastDetected == null) return;

        var proj = cornTrigger.LastDetected.GetComponent<HomingProjectile>();
        if (proj == null) return;

        if (proj.mobIsOgSender && proj.LaunchesNumber == 0) return;
        if (proj.Enemy != gameObject) return;

        if (currentHp <= 0)
        {
            Die();
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
            beatSinger.Sing();
            return;
        }

        if (ownBounce)
        {
            state = SpitterState.Respuesta;
            deflectMode = DeflectMode.Homing;
            currentEngagement = proj;
            beatSinger.Sing();
            return;
        }

        state = SpitterState.Respuesta;
        deflectMode = DeflectMode.Homing;
        var l = proj.GetComponentInChildren<SingleNotesListener>(true);
        if (l != null) ReadCornMelody(l);
        currentEngagement = proj;
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
        if (state == SpitterState.Dead) return;
        incomingCorns.Remove(proj);

        bool isMine = currentEngagement == proj || state == SpitterState.Iniciativa;
        if (!isMine) return;

        state = SpitterState.Idle;
        currentHp = maxHp;
        cornSpawned = false;
        cornAlreadyRead = false;
        currentEngagement = null;
        interruptResumeAfter = SpitterState.Idle;
        RestorePattern();
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
        myCorn.transform.rotation = Quaternion.identity;

        HomingProjectile script = myCorn.GetComponent<HomingProjectile>();
        if (script != null)
        {
            script.player = collider.gameObject;
            script.mobIsOgSender = true;
            script.enemy = gameObject;
            beatSinger.Sing();
        }
    }

    public void ReadCornMelody(SingleNotesListener corn)
    {
        if (cornAlreadyRead) return;
        melodyToCopy = corn.desiredMelody;
        beatSinger.pattern[0] = (NoteSlot)melodyToCopy[1];
        beatSinger.pattern[1] = NoteSlot.Empty;
        beatSinger.pattern[2] = (NoteSlot)melodyToCopy[0];
        cornAlreadyRead = true;
    }

    public void SingForCorn()
    {
        if (currentHp <= 0) return;
        beatSinger.Sing();
        currentHp--;
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
        state = SpitterState.Dead;
        if (beatSinger != null) beatSinger.StopSinging();
        Destroy(gameObject);
    }
}
