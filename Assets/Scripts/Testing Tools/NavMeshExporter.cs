using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class NavMeshExporter : MonoBehaviour
{
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
}