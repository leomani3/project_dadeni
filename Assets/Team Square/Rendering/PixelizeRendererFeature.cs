using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class PixelizeRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _pixelizeShader;
    [SerializeField] private RenderPassEvent _injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField, Range(16, 1080)] private int _verticalPixelCount = 216;

    private Material _pixelizeMaterial;
    private PixelizePass _pixelizePass;

    public override void Create()
    {
        if (_pixelizeShader == null)
        {
            _pixelizePass = null;
            return;
        }

        _pixelizeMaterial = CoreUtils.CreateEngineMaterial(_pixelizeShader);
        _pixelizePass = new PixelizePass(_pixelizeMaterial) { renderPassEvent = _injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pixelizePass == null) return;

        _pixelizePass.SetVerticalPixelCount(_verticalPixelCount);
        renderer.EnqueuePass(_pixelizePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_pixelizeMaterial);
        _pixelizeMaterial = null;
        _pixelizePass = null;
    }

    private class PixelizePass : ScriptableRenderPass
    {
        private const string LowResolutionTextureName = "_PixelizedColor";
        private const string DownsamplePassName = "Pixelize Downsample";
        private const string UpscalePassName = "Pixelize Upscale";

        private readonly Material _material;
        private int _verticalPixelCount;

        public PixelizePass(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true;
        }

        public void SetVerticalPixelCount(int verticalPixelCount)
        {
            _verticalPixelCount = verticalPixelCount;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle cameraColor = resourceData.activeColorTexture;
            if (!cameraColor.IsValid()) return;

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            int pixelHeight = Mathf.Clamp(_verticalPixelCount, 1, descriptor.height);
            int pixelWidth = Mathf.Max(1, Mathf.RoundToInt(descriptor.width * (pixelHeight / (float)descriptor.height)));

            descriptor.width = pixelWidth;
            descriptor.height = pixelHeight;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;

            TextureHandle lowResolutionColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, LowResolutionTextureName, false, FilterMode.Point);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(cameraColor, lowResolutionColor, _material, 0), DownsamplePassName);
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(lowResolutionColor, cameraColor, _material, 0), UpscalePassName);
        }
    }
}
