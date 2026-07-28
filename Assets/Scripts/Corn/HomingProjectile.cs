using System.Collections;
using UnityEngine;

public enum Mode { Forward, Homing }
public enum SenderType { Player, Enemy}

public class HomingProjectile : MonoBehaviour, IControllableProjectile, IPoolable
{
    private Rigidbody rb;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    public Transform latestNoteTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float impactThreshold = 5f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float lifeTime = 10f;
    private Coroutine _suicideCoroutine;
    
    public GameObject sender;
    public GameObject Sender => sender;
    public GameObject Enemy => enemy; 
    public bool mobIsOgSender = false;
    
    private GameObject _target; 
    private Transform _targetAnchor;
    private Transform spawnPoint;

    public bool movingForward = false;
    public bool followingTarget = false;
    public bool destinationAlreadySet = false;
    
    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask occlusionLayer; 
    [SerializeField] private float detectionRadius = 50f;
    private readonly Collider[] detectionResults = new Collider[10];
    
    public GameObject player;
    public GameObject enemy;
    [SerializeField] private int launchesNumber = 0;
    public int LaunchesNumber => launchesNumber;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
    }

    public void OnSpawn()
    {
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; 
            rb.ResetInertiaTensor(); 
        }

        player = null;
        enemy = null;
        sender = null;
        _target = null;
        _targetAnchor = null;
        movingForward = false;
        followingTarget = false;
        destinationAlreadySet = false;
        launchesNumber = 0;
        mobIsOgSender = false;
        latestNoteTransform = null;
        moveDirection = Vector3.zero;

        if (_suicideCoroutine != null)
        {
            StopCoroutine(_suicideCoroutine);
            _suicideCoroutine = null;
        }
    }

    void FixedUpdate()
    {
        if (movingForward)
        {
            MoveForward();
            return;
        }

        if (followingTarget && _targetAnchor != null)
        {
            MoveTowardsTarget();
        }
        else if (followingTarget && _targetAnchor == null)
        {
            SetMovementMode(Mode.Forward, sender);
        }
    }

    public void MoveForward()
    {
        if (destinationAlreadySet) return;
        if (latestNoteTransform == null) return;
        moveDirection = (transform.position - latestNoteTransform.position).normalized;
        rb.velocity = moveDirection * speed;

        transform.forward = moveDirection;
        StartSuicideTimer();
        destinationAlreadySet = true;
    }

    public void MoveTowardsTarget()
    {
        if (_targetAnchor == null) return;

        moveDirection = (_targetAnchor.position - transform.position).normalized;
        rb.velocity = moveDirection * speed;

        gameObject.transform.forward = moveDirection;
        CancelSuicide();
    }

    public void SetTarget(GameObject newTarget)
    {
        _target = newTarget;
        
        if (_target != null)
        {
            if (_target.TryGetComponent(out TargetForCorn anchorProvider))
            {
                _targetAnchor = anchorProvider.AimTransform;
            }
            else
            {
                Debug.LogWarning($"Target {_target.name} has no TargetForCorn component. Falling back to root transform.");
                _targetAnchor = _target.transform;
            }
        }
        else
        {
            _targetAnchor = null;
        }
    }

    public void SetMovementMode(Mode mode, GameObject remitente)
    {
        sender = remitente;
        destinationAlreadySet = false;

        switch (mode)
        {
            case Mode.Forward:
                ResetRBVelocity();
                followingTarget = false;
                movingForward = true;
                break;

            case Mode.Homing:
                ResetRBVelocity();
                movingForward = false;
                followingTarget = true;
                break;
        }
    }

    public void SaveNotePosition(Transform noteTransform)
    {
        latestNoteTransform = noteTransform;
    }

    public void Launch(GameObject whoSends)
    {
        launchesNumber++;
        
        if (whoSends.layer == 6) // Player
        {
            SetTarget(enemy); 
            SetMovementMode(Mode.Homing, whoSends);
        }
        else if (whoSends.layer == 14) // Enemy
        {
            sender = whoSends;
            SetTarget(player);
            SetMovementMode(Mode.Homing, whoSends);
        }
    }

    public void LaunchLinear(GameObject whoSends)
    {
        launchesNumber++;
        sender = whoSends;
        SetMovementMode(Mode.Forward, whoSends);
        StartSuicideTimer();
    }

    public void ResetRBVelocity()
    {
        if (rb != null) rb.velocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sender != null && collision.transform.root.gameObject == sender) return;

        // Check if the direct hit is on the targeted enemy
        if (enemy != null && collision.transform.root.gameObject == enemy)
        {
            var ai = enemy.GetComponent<CornSpitterAI>();
            if (ai != null)
            {
                ai.TakeDirectHit();
            }
        }

        Explode();
    }

    public void Explode()
    {
        if (!gameObject.activeSelf) return; 

        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (enemy != null)
        {
            var ai = enemy.GetComponent<CornSpitterAI>();
            if (ai != null)
            {
                ai.UnregisterIncomingCorn(this);
                ai.OnEngagementCornExploded(this);
            }
        }

        CancelSuicide(); 
        CancelInvoke();
        
        if (TryGetComponent(out PoolMember member)) member.ReturnToPool();
        gameObject.SetActive(false);
    }

    private GameObject GetBestTargetByScreenCenter()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return null;

        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectionResults, enemyLayer);
        GameObject bestTarget = null;
        float closestDistanceToCenter = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            GameObject potentialEnemy = detectionResults[i].gameObject;

            var ai = potentialEnemy.GetComponent<CornSpitterAI>();
            if (ai != null && ai.State != SpitterState.Idle) continue;

            Vector3 screenPoint = mainCam.WorldToViewportPoint(potentialEnemy.transform.position);

            if (IsInsideViewport(screenPoint))
            {
                if (HasLineOfSight(mainCam.transform.position, potentialEnemy.transform.position))
                {
                    float distanceToCenter = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), new Vector2(0.5f, 0.5f));
                    if (distanceToCenter < closestDistanceToCenter)
                    {
                        closestDistanceToCenter = distanceToCenter;
                        bestTarget = potentialEnemy;
                    }
                }
            }
        }
        return bestTarget;
    }

    private bool IsInsideViewport(Vector3 screenPoint) => screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    private bool HasLineOfSight(Vector3 camPos, Vector3 targetPos) => !Physics.Linecast(camPos, targetPos, occlusionLayer);

    private IEnumerator SuicideRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Explode();
    }

    public void StartSuicideTimer()
    {
        if (_suicideCoroutine != null) StopCoroutine(_suicideCoroutine);
        _suicideCoroutine = StartCoroutine(SuicideRoutine());
    }

    public void CancelSuicide()
    {
        if (_suicideCoroutine != null)
        {
            StopCoroutine(_suicideCoroutine); 
            _suicideCoroutine = null;
        }
    }
}