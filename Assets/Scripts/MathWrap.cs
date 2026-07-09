using UnityEngine;

// This class must be static and not inherit from MonoBehaviour
public static class MathExtensions
{
    // The 'this' keyword makes it an Extension Method for the 'int' type
    public static int Wrap(this int value, int min, int max)
    {
        int range = max - min + 1;
        return min + (((value - min) % range + range) % range);
    }
}