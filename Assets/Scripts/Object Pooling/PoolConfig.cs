using UnityEngine;

// This attribute allows you to create instances of this config from the Project window (Right Click > Pooling > Pool Config)
[CreateAssetMenu(fileName = "NewPoolConfig", menuName = "Pooling/Pool Config")]
public class PoolConfig : ScriptableObject
{
    public GameObject prefab;
    public int defaultCapacity = 10; // Normal ingame amount
    public int maxSize = 50; //Max quantity 
}