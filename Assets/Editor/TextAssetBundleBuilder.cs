using System.IO;
using UnityEditor;

/// <summary>
/// Simple AssetBundle builder. Place this script inside an "Editor" folder.
/// </summary>
public static class TextAssetBundleBuilder
{
    [MenuItem("Vbntool/AssetsBundlePackage")]
    private static void BuildAllAssetBundles()
    {
        const string folderName = "AssetsBundlePackage";

        // Ensure the output folder exists
        if (!Directory.Exists(folderName))
            Directory.CreateDirectory(folderName);

        // Build the bundle for Windows Standalone (adjust BuildTarget if needed)
        BuildPipeline.BuildAssetBundles(
            folderName,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows);
    }
}