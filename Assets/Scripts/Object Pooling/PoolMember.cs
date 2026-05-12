using UnityEngine;
using UnityEngine.Pool;

public class PoolMember : MonoBehaviour
{
    private IObjectPool<GameObject> _originPool;

    public void SetOriginPool(IObjectPool<GameObject> pool)
    {
        _originPool = pool;
    }

    public void ReturnToPool()
    {
        if (_originPool != null)
        {
            _originPool.Release(gameObject);
        }
        else
        {
            // Fallback in case the object was instantiated normally
            Destroy(gameObject);
        }
    }
}