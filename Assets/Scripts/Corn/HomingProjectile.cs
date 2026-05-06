using System.Collections;
using UnityEngine;

public enum Mode { Forward, Homing }
public enum SenderType { Player, Enemy}

public class HomingProjectile : MonoBehaviour, IControllableProjectile
{
    private Rigidbody rb;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    public Transform latestNoteTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float impactThreshold = 5f;
    [SerializeField] private GameObject explosionPrefab;
    
    public GameObject sender;
    public GameObject Sender => sender; 
    
    private GameObject _target; 
    private Transform _targetAnchor; // Este es el que manda ahora

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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (movingForward)
        {
            MoveForward();
            return;
        }

        // CAMBIO: Validamos _targetAnchor en lugar de _target
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
        moveDirection = (transform.position - latestNoteTransform.position).normalized;
        rb.velocity = moveDirection * speed;

        transform.forward = moveDirection;
        destinationAlreadySet = true;
    }

    public void MoveTowardsTarget()
    {
        if (_targetAnchor == null) return;

        moveDirection = (_targetAnchor.position - transform.position).normalized;
        rb.velocity = moveDirection * speed;

        gameObject.transform.forward = moveDirection;
    }

    public void SetTarget(GameObject newTarget)
    {
        _target = newTarget;
        
        if (_target != null)
        {
            // Intentamos obtener el punto de anclaje
            if (_target.TryGetComponent(out TargetForCorn anchorProvider))
            {
                _targetAnchor = anchorProvider.AimTransform;
            }
            else
            {
                // Si el objetivo no tiene el script, avisamos para depurar
                Debug.LogWarning($"El objetivo {_target.name} no tiene TargetForCorn. Usando root.");
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
            if (launchesNumber == 1)
            {
                player = whoSends;
                enemy = GetBestTargetByScreenCenter();
            }
            SetTarget(enemy);
            SetMovementMode(Mode.Homing, whoSends);
        }
        else if (whoSends.layer == 14) // Enemy
        {
            if (launchesNumber == 1)
            {
                enemy = whoSends;
            }
            SetTarget(player);
            SetMovementMode(Mode.Homing, whoSends);
        }
    }

    public void ResetRBVelocity()
    {
        if (rb != null) rb.velocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sender != null && collision.transform.root.gameObject == sender) return;
        Explode();
    }

    public void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        
        Destroy(gameObject);
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
}