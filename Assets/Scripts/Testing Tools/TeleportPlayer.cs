using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerOverride;

    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private TeleportPoint[] _points;

    private void Start()
    {
        ResolvePlayer();
        RefreshPoints();
    }

    public void RefreshPoints()
    {
        _points = FindObjectsOfType<TeleportPoint>();
        Debug.Log($"[TeleportPlayer] {_points.Length} puntos cacheados.");
    }

    private void ResolvePlayer()
    {
        Transform source = playerOverride != null
            ? playerOverride
            : GameObject.Find("ThirdPersonWalker_B")?.transform;

        if (source == null)
        {
            Debug.LogError("[TeleportPlayer] Player no encontrado. Asigna 'Player Override' o comprueba que 'ThirdPersonWalker_B' está en escena.");
            return;
        }

        _playerTransform = source;
        _playerRigidbody = source.GetComponent<Rigidbody>();
        Debug.Log($"[TeleportPlayer] Player resuelto: {source.name}");
    }

    private void Update()
    {
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad0 + i))
                TeleportTo(i);
        }
    }

    private void TeleportTo(int number)
    {
        if (_playerTransform == null)
        {
            Debug.LogError("[TeleportPlayer] Player no asignado.");
            return;
        }

        TeleportPoint point = FindPoint(number);
        if (point == null)
        {
            Debug.LogWarning($"[TeleportPlayer] No hay TeleportPoint con número {number}.");
            return;
        }

        _playerTransform.position = point.transform.position;
        _playerTransform.rotation = point.transform.rotation;

        if (_playerRigidbody != null)
        {
            _playerRigidbody.velocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[TeleportPlayer] → punto {number} en {point.transform.position}");
    }

    private TeleportPoint FindPoint(int number)
    {
        foreach (TeleportPoint point in _points)
        {
            if (point != null && point.Number == number)
                return point;
        }
        return null;
    }
}
