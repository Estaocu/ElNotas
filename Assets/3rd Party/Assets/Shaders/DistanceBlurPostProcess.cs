using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class DistanceBlurPostProcess : MonoBehaviour
{
    [SerializeField] private Material postProcessMaterial;

    private void OnEnable()
    {
        Camera mainCamera = GetComponent<Camera>();
        if (mainCamera != null)
        {
            mainCamera.depthTextureMode |= DepthTextureMode.Depth;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (postProcessMaterial != null)
        {
            Graphics.Blit(source, destination, postProcessMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}