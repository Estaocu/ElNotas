using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Full-screen anisotropic Kuwahara filter ("painted"/brush-stroke look),
// masked by scene depth to behave like a Depth of Field effect:
// the focus plane stays sharp while near/far regions get progressively
// stronger brush-stroke blur.
public class KuwaharaDOFFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        public Shader shader;

        [Header("Kuwahara")]
        [Tooltip("Kernel radius in cell steps. Higher = larger filter area and higher cost.")]
        [Range(1, 16)] public int radius = 4;

        [Tooltip("Size of each Kuwahara cell in pixels. Higher values create larger, more visible brush strokes without increasing the sample count.")]
        [Range(1, 8)] public int cellSize = 3;

        [Tooltip("Sector hardness (q). Higher = crisper stroke edges, closer to the classic look.")]
        [Range(1f, 20f)] public float sharpness = 8f;

        [Tooltip("Anisotropy strength (alpha). Lower = strokes stretch more along edges.")]
        [Range(0.1f, 4f)] public float eccentricity = 1f;

        [Tooltip("Extra smoothing passes applied to the structure tensor before eigen-analysis.")]
        [Range(0, 3)] public int tensorBlurIterations = 1;

        [Header("Depth of Field")]
        [Tooltip("World-space distance from the camera that stays perfectly sharp.")]
        public float focusDistance = 10f;

        [Tooltip("Width of the fully sharp zone, centered on Focus Distance.")]
        [Min(0f)] public float focusRange = 4f;

        [Tooltip("Distance (world units) over which the blur ramps up in front of the focus zone.")]
        [Min(0.01f)] public float nearTransitionRange = 5f;

        [Tooltip("Distance (world units) over which the blur ramps up behind the focus zone.")]
        [Min(0.01f)] public float farTransitionRange = 10f;

        [Header("Performance")]
        [Tooltip("Process the effect at half resolution and upscale. Big perf win, softer result.")]
        public bool halfResolution = false;

        [Header("Debug")]
        [Tooltip("Replace the output with the focus mask (black = sharp, white = fully blurred).")]
        public bool debugShowFocusMask = false;
    }

    public Settings settings = new Settings();

    Material material;
    KuwaharaDOFPass pass;

    public override void Create()
    {
        if (settings.shader == null)
        {
            material = null;
            pass = null;
            return;
        }

        material = CoreUtils.CreateEngineMaterial(settings.shader);

        pass = new KuwaharaDOFPass(material, settings)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (material == null || pass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();

#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
#else
        Destroy(material);
#endif
    }
}

class KuwaharaDOFPass : ScriptableRenderPass
{
    static class ShaderIDs
    {
        public static readonly int radius =
            Shader.PropertyToID("_Radius");

        public static readonly int cellSize =
            Shader.PropertyToID("_CellSize");

        public static readonly int sharpness =
            Shader.PropertyToID("_Sharpness");

        public static readonly int eccentricity =
            Shader.PropertyToID("_Eccentricity");

        public static readonly int focusDistance =
            Shader.PropertyToID("_FocusDistance");

        public static readonly int focusRange =
            Shader.PropertyToID("_FocusRange");

        public static readonly int nearTransitionRange =
            Shader.PropertyToID("_NearTransitionRange");

        public static readonly int farTransitionRange =
            Shader.PropertyToID("_FarTransitionRange");

        public static readonly int structureTensorTex =
            Shader.PropertyToID("_StructureTensorTex");
    }

    const string profilerTag = "Kuwahara DOF";
    const string debugKeyword = "_DEBUG_COC";

    readonly Material material;
    readonly KuwaharaDOFFeature.Settings settings;
    readonly ProfilingSampler profilingSampler =
        new ProfilingSampler(profilerTag);

    RenderTextureDescriptor colorDescriptor;
    RenderTextureDescriptor tensorDescriptor;

    RTHandle sourceHandle;
    RTHandle tensorHandleA;
    RTHandle tensorHandleB;
    RTHandle tempHandle;

    public KuwaharaDOFPass(
        Material material,
        KuwaharaDOFFeature.Settings settings)
    {
        this.material = material;
        this.settings = settings;

        // Make sure the camera depth texture is ready before this pass runs.
        ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void Configure(
        CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor)
    {
        int scale = settings.halfResolution ? 2 : 1;

        colorDescriptor = cameraTextureDescriptor;
        colorDescriptor.depthBufferBits = 0;
        colorDescriptor.msaaSamples = 1;
        colorDescriptor.width =
            Mathf.Max(1, cameraTextureDescriptor.width / scale);
        colorDescriptor.height =
            Mathf.Max(1, cameraTextureDescriptor.height / scale);

        tensorDescriptor = colorDescriptor;
        tensorDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;

        RenderingUtils.ReAllocateIfNeeded(
            ref sourceHandle,
            colorDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_KuwaharaSource");

        RenderingUtils.ReAllocateIfNeeded(
            ref tensorHandleA,
            tensorDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_KuwaharaTensorA");

        RenderingUtils.ReAllocateIfNeeded(
            ref tensorHandleB,
            tensorDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_KuwaharaTensorB");

        RenderingUtils.ReAllocateIfNeeded(
            ref tempHandle,
            colorDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_KuwaharaTemp");
    }

    void PushMaterialSettings()
    {
        material.SetFloat(
            ShaderIDs.radius,
            settings.radius);

        material.SetFloat(
            ShaderIDs.cellSize,
            settings.cellSize);

        material.SetFloat(
            ShaderIDs.sharpness,
            settings.sharpness);

        material.SetFloat(
            ShaderIDs.eccentricity,
            settings.eccentricity);

        material.SetFloat(
            ShaderIDs.focusDistance,
            settings.focusDistance);

        material.SetFloat(
            ShaderIDs.focusRange,
            settings.focusRange);

        material.SetFloat(
            ShaderIDs.nearTransitionRange,
            settings.nearTransitionRange);

        material.SetFloat(
            ShaderIDs.farTransitionRange,
            settings.farTransitionRange);

        if (settings.debugShowFocusMask)
        {
            material.EnableKeyword(debugKeyword);
        }
        else
        {
            material.DisableKeyword(debugKeyword);
        }
    }

    public override void Execute(
        ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        if (material == null)
            return;

        CommandBuffer cmd =
            CommandBufferPool.Get(profilerTag);

        using (new ProfilingScope(cmd, profilingSampler))
        {
            RTHandle cameraTarget =
                renderingData.cameraData.renderer.cameraColorTargetHandle;

            PushMaterialSettings();

            // 0. Grab the current scene color
            // and handle the half-resolution downsample.
            Blit(
                cmd,
                cameraTarget,
                sourceHandle);

            // 1. Structure tensor from Sobel gradients.
            Blit(
                cmd,
                sourceHandle,
                tensorHandleA,
                material,
                0);

            // 2. Smooth the tensor N times.
            // Ping-pong between A and B.
            RTHandle tensorIn = tensorHandleA;
            RTHandle tensorOut = tensorHandleB;

            for (int i = 0;
                 i < settings.tensorBlurIterations;
                 i++)
            {
                Blit(
                    cmd,
                    tensorIn,
                    tensorOut,
                    material,
                    1);

                (tensorIn, tensorOut) =
                    (tensorOut, tensorIn);
            }

            material.SetTexture(
                ShaderIDs.structureTensorTex,
                tensorIn);

            // 3. Anisotropic Kuwahara
            // plus depth-based DOF mask.
            Blit(
                cmd,
                sourceHandle,
                tempHandle,
                material,
                2);

            // 4. Copy the result back onto the camera target.
            // This also upscales when half-resolution is enabled.
            Blit(
                cmd,
                tempHandle,
                cameraTarget);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        sourceHandle?.Release();
        tensorHandleA?.Release();
        tensorHandleB?.Release();
        tempHandle?.Release();
    }
}