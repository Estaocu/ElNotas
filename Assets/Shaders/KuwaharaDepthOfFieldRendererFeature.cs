using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaDepthOfFieldRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Shader")]
        public Shader shader;

        [Header("Effect")]
        [Range(0f, 1f)]
        public float intensity = 1f;

        [Range(1f, 20f)]
        public float radius = 4f;

        [Header("Depth Of Field")]
        [Range(0f, 5f)]
        public float depthStrength = 1f;

        [Min(0f)]
        public float focusDistance = 10f;

        [Range(0.01f, 50f)]
        public float focusRange = 5f;

        [Range(0f, 20f)]
        public float maxBlurRadius = 6f;

        [Range(0.1f, 10f)]
        public float depthFalloff = 2f;

        [Range(0f, 1f)]
        public float depthBias = 0f;

        [Header("Brush")]
        [Range(0.25f, 4f)]
        public float brushScale = 1f;

        [Range(0f, 1f)]
        public float brushAnisotropy = 0.5f;

        [Range(0f, 6.283185f)]
        public float brushRotation = 0f;

        [Header("Rendering")]
        public RenderPassEvent renderPassEvent =
            RenderPassEvent.AfterRenderingPostProcessing;
    }

    [SerializeField]
    private Settings settings = new Settings();

    private Material material;
    private KuwaharaRenderPass renderPass;

    public override void Create()
    {
        if (settings.shader == null)
        {
            settings.shader = Shader.Find(
                "Hidden/Universal Render Pipeline/Kuwahara Depth Of Field"
            );
        }

        if (settings.shader == null)
        {
            Debug.LogError(
                "Kuwahara Depth Of Field shader could not be found."
            );

            return;
        }

        material = CoreUtils.CreateEngineMaterial(
            settings.shader
        );

        renderPass = new KuwaharaRenderPass(
            material,
            settings
        );

        renderPass.renderPassEvent =
            settings.renderPassEvent;
    }

    public override void SetupRenderPasses(
        ScriptableRenderer renderer,
        in RenderingData renderingData
    )
    {
        if (renderPass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        renderPass.SetTarget(
            renderer.cameraColorTargetHandle
        );
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (renderPass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        renderPass.ConfigureInput(
            ScriptableRenderPassInput.Depth
        );

        renderer.EnqueuePass(
            renderPass
        );
    }

    protected override void Dispose(bool disposing)
    {
        renderPass?.Dispose();

        CoreUtils.Destroy(material);
        material = null;
    }

    private class KuwaharaRenderPass : ScriptableRenderPass
    {
        private readonly Material material;
        private readonly Settings settings;

        private RTHandle temporaryTexture;
        private RTHandle cameraColorTarget;

        private static readonly int intensityId =
            Shader.PropertyToID("_Intensity");

        private static readonly int radiusId =
            Shader.PropertyToID("_Radius");

        private static readonly int depthStrengthId =
            Shader.PropertyToID("_DepthStrength");

        private static readonly int focusDistanceId =
            Shader.PropertyToID("_FocusDistance");

        private static readonly int focusRangeId =
            Shader.PropertyToID("_FocusRange");

        private static readonly int maxBlurRadiusId =
            Shader.PropertyToID("_MaxBlurRadius");

        private static readonly int brushScaleId =
            Shader.PropertyToID("_BrushScale");

        private static readonly int brushAnisotropyId =
            Shader.PropertyToID("_BrushAnisotropy");

        private static readonly int brushRotationId =
            Shader.PropertyToID("_BrushRotation");

        private static readonly int depthFalloffId =
            Shader.PropertyToID("_DepthFalloff");

        private static readonly int depthBiasId =
            Shader.PropertyToID("_DepthBias");

        public KuwaharaRenderPass(
            Material material,
            Settings settings
        )
        {
            this.material = material;
            this.settings = settings;

            profilingSampler =
                new ProfilingSampler(
                    "Kuwahara Depth Of Field"
                );
        }

        public void SetTarget(
    RTHandle cameraColorTarget
)
{
    this.cameraColorTarget = cameraColorTarget;
}

        public override void Configure(
            CommandBuffer cmd,
            RenderTextureDescriptor cameraTextureDescriptor
        )
        {
            cameraTextureDescriptor.depthBufferBits = 0;
            cameraTextureDescriptor.msaaSamples = 1;

            if (temporaryTexture != null)
            {
                temporaryTexture.Release();
                temporaryTexture = null;
            }

            temporaryTexture = RTHandles.Alloc(
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height,
                colorFormat: cameraTextureDescriptor.graphicsFormat,
                depthBufferBits: DepthBits.None,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                name: "_KuwaharaTemporaryTexture"
            );
        }

        private void UpdateMaterial()
        {
            material.SetFloat(
                intensityId,
                settings.intensity
            );

            material.SetFloat(
                radiusId,
                settings.radius
            );

            material.SetFloat(
                depthStrengthId,
                settings.depthStrength
            );

            material.SetFloat(
                focusDistanceId,
                settings.focusDistance
            );

            material.SetFloat(
                focusRangeId,
                settings.focusRange
            );

            material.SetFloat(
                maxBlurRadiusId,
                settings.maxBlurRadius
            );

            material.SetFloat(
                brushScaleId,
                settings.brushScale
            );

            material.SetFloat(
                brushAnisotropyId,
                settings.brushAnisotropy
            );

            material.SetFloat(
                brushRotationId,
                settings.brushRotation
            );

            material.SetFloat(
                depthFalloffId,
                settings.depthFalloff
            );

            material.SetFloat(
                depthBiasId,
                settings.depthBias
            );
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData
        )
        {
            if (material == null)
                return;

            if (cameraColorTarget == null)
                return;

            if (temporaryTexture == null)
                return;

            CommandBuffer commandBuffer =
                CommandBufferPool.Get(
                    "Kuwahara Depth Of Field"
                );

            using (
                new ProfilingScope(
                    commandBuffer,
                    profilingSampler
                )
            )
            {
                UpdateMaterial();

                Blitter.BlitCameraTexture(
                    commandBuffer,
                    cameraColorTarget,
                    temporaryTexture,
                    material,
                    0
                );

                Blitter.BlitCameraTexture(
                    commandBuffer,
                    temporaryTexture,
                    cameraColorTarget
                );
            }

            context.ExecuteCommandBuffer(
                commandBuffer
            );

            CommandBufferPool.Release(
                commandBuffer
            );
        }

        public void Dispose()
        {
            if (temporaryTexture != null)
            {
                temporaryTexture.Release();
                temporaryTexture = null;
            }
        }
    }
}