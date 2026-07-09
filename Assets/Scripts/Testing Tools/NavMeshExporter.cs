using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavMeshExporter : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Export NavMesh to Mesh")]
    public static void ExportNavMesh()
    {
        NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();

        Mesh mesh = new Mesh();
        mesh.name = "ExportedNavMesh";
        mesh.vertices = triangulatedNavMesh.vertices;
        mesh.triangles = triangulatedNavMesh.indices;
        mesh.RecalculateNormals();

        // Save as a Mesh Asset
        AssetDatabase.CreateAsset(mesh, "Assets/ExportedNavMesh.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("NavMesh exported to Assets/ExportedNavMesh.asset");
    }
#endif
}