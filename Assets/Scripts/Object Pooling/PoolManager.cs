using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    [SerializeField] private List<PoolConfig> poolConfigs;
    public static PoolManager Instance { get; private set; }

    private Dictionary<GameObject, IObjectPool<GameObject>> _pools = new Dictionary<GameObject, IObjectPool<GameObject>>();

    private static readonly List<IPoolable> _poolableBuffer = new List<IPoolable>(4);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupPools();
    }

    private void SetupPools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab == null) continue;

            IObjectPool<GameObject> pool = null;

            pool = new ObjectPool<GameObject>(
                createFunc: () => {
                    GameObject obj = Instantiate(config.prefab);

                    if (!obj.TryGetComponent(out PoolMember member))
                    {
                        member = obj.AddComponent<PoolMember>();
                    }
                    member.SetOriginPool(pool);

                    return obj;
                },
                actionOnGet: (obj) => {
                    obj.SetActive(true);
                    InvokeOnSpawn(obj);
                },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: config.defaultCapacity,
                maxSize: config.maxSize
            );

            _pools.Add(config.prefab, pool);
        }
    }

    public GameObject GetObject(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out IObjectPool<GameObject> pool))
        {
            Debug.LogError($"Pool for prefab {prefab.name} not found! Check your PoolConfigs in the inspector.");
            return null;
        }

        try
        {
            return pool.Get();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Pool '{prefab.name}' could not deliver an object (maxSize reached or internal error): {e.Message}");
            return null;
        }
    }

    public void ReleaseObject(GameObject prefab, GameObject obj)
    {
        if (_pools.TryGetValue(prefab, out IObjectPool<GameObject> pool))
        {
            pool.Release(obj);
        }
    }

    private static void InvokeOnSpawn(GameObject obj)
    {
        obj.GetComponentsInChildren(true, _poolableBuffer);
        for (int i = 0; i < _poolableBuffer.Count; i++)
        {
            _poolableBuffer[i].OnSpawn();
        }
        _poolableBuffer.Clear();
    }
}
