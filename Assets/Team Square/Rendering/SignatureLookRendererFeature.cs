using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Utils.Rendering
{
    public class SignatureLookRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _signatureShader;
        [SerializeField] private RenderPassEvent _injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        [SerializeField] private PaletteAsset _palette;

        [SerializeField] private bool _outlineEnabled = true;
        [SerializeField] private Color _outlineColor = new Color(0.106f, 0.078f, 0.125f, 1f);
        [SerializeField, Range(0.5f, 6f)] private float _outlineThickness = 1f;
        [SerializeField, Range(0f, 4f)] private float _depthEdgeStrength = 1.2f;
        [SerializeField, Range(0f, 4f)] private float _normalEdgeStrength = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _edgeThreshold = 0.25f;

        [SerializeField] private bool _hazeEnabled = true;
        [SerializeField] private Color _hazeColor = new Color(0.361f, 0.502f, 0.639f, 1f);
        [SerializeField] private float _hazeStartDistance = 14f;
        [SerializeField] private float _hazeEndDistance = 55f;
        [SerializeField, Range(0f, 1f)] private float _hazeStrength = 0.35f;
        [SerializeField, Range(0.1f, 4f)] private float _hazeFalloff = 1.5f;
        [SerializeField] private bool _hazeAffectsSky = true;

        [SerializeField] private bool _pixelizeEnabled;
        [SerializeField, Range(64, 1080)] private int _verticalPixelCount = 270;

        [SerializeField] private bool _paletteEnabled = true;
        [SerializeField, Range(0f, 1f)] private float _paletteStrength = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _ditherStrength = 0.35f;
        [SerializeField, Range(1, 8)] private int _ditherScale = 1;

        private static readonly int PaletteColorsId = Shader.PropertyToID("_PaletteColors");
        private static readonly int PaletteCountId = Shader.PropertyToID("_PaletteCount");
        private static readonly int PaletteStrengthId = Shader.PropertyToID("_PaletteStrength");
        private static readonly int DitherStrengthId = Shader.PropertyToID("_DitherStrength");
        private static readonly int DitherScaleId = Shader.PropertyToID("_DitherScale");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
        private static readonly int DepthEdgeStrengthId = Shader.PropertyToID("_DepthEdgeStrength");
        private static readonly int NormalEdgeStrengthId = Shader.PropertyToID("_NormalEdgeStrength");
        private static readonly int EdgeThresholdId = Shader.PropertyToID("_EdgeThreshold");
        private static readonly int HazeColorId = Shader.PropertyToID("_HazeColor");
        private static readonly int HazeStartId = Shader.PropertyToID("_HazeStart");
        private static readonly int HazeEndId = Shader.PropertyToID("_HazeEnd");
        private static readonly int HazeStrengthId = Shader.PropertyToID("_HazeStrength");
        private static readonly int HazeFalloffId = Shader.PropertyToID("_HazeFalloff");
        private static readonly int HazeAffectsSkyId = Shader.PropertyToID("_HazeAffectsSky");
        private static readonly int PixelGridId = Shader.PropertyToID("_PixelGrid");
        private static readonly int PixelizeEnabledId = Shader.PropertyToID("_PixelizeEnabled");

        private readonly Vector4[] _paletteBuffer = new Vector4[PaletteAsset.MaxColors];

        private Material _signatureMaterial;
        private SignatureLookPass _signaturePass;

        public override void Create()
        {
            if (_signatureShader == null)
            {
                _signaturePass = null;
                return;
            }

            _signatureMaterial = CoreUtils.CreateEngineMaterial(_signatureShader);
            _signaturePass = new SignatureLookPass(_signatureMaterial) { renderPassEvent = _injectionPoint };
        }

        public override void AddRenderPasses(ScriptableRenderer _renderer, ref RenderingData _renderingData)
        {
            if (_signaturePass == null || _signatureMaterial == null)
                return;

            _signaturePass.renderPassEvent = _injectionPoint;

            UpdateMaterial(_renderingData.cameraData.cameraTargetDescriptor);
            _renderer.EnqueuePass(_signaturePass);
        }

        private void UpdateMaterial(RenderTextureDescriptor _targetDescriptor)
        {
            _signatureMaterial.SetColor(OutlineColorId, GetOutlineColor());
            _signatureMaterial.SetFloat(OutlineThicknessId, _outlineThickness);
            _signatureMaterial.SetFloat(DepthEdgeStrengthId, _depthEdgeStrength);
            _signatureMaterial.SetFloat(NormalEdgeStrengthId, _normalEdgeStrength);
            _signatureMaterial.SetFloat(EdgeThresholdId, _edgeThreshold);

            _signatureMaterial.SetColor(HazeColorId, _hazeColor);
            _signatureMaterial.SetFloat(HazeStartId, _hazeStartDistance);
            _signatureMaterial.SetFloat(HazeEndId, Mathf.Max(_hazeEndDistance, _hazeStartDistance + 0.01f));
            _signatureMaterial.SetFloat(HazeStrengthId, _hazeEnabled ? _hazeStrength : 0f);
            _signatureMaterial.SetFloat(HazeFalloffId, _hazeFalloff);
            _signatureMaterial.SetFloat(HazeAffectsSkyId, _hazeAffectsSky ? 1f : 0f);

            _signatureMaterial.SetFloat(PixelizeEnabledId, _pixelizeEnabled ? 1f : 0f);
            _signatureMaterial.SetVector(PixelGridId, GetPixelGrid(_targetDescriptor));

            _signatureMaterial.SetFloat(PaletteStrengthId, _paletteEnabled ? _paletteStrength : 0f);
            _signatureMaterial.SetFloat(DitherStrengthId, _ditherStrength);
            _signatureMaterial.SetFloat(DitherScaleId, _ditherScale);
            _signatureMaterial.SetInteger(PaletteCountId, GetPaletteColors());
            _signatureMaterial.SetVectorArray(PaletteColorsId, _paletteBuffer);
        }

        private Color GetOutlineColor()
        {
            Color color = _outlineColor;
            color.a = _outlineEnabled ? _outlineColor.a : 0f;
            return color;
        }

        private Vector2 GetPixelGrid(RenderTextureDescriptor _targetDescriptor)
        {
            int height = Mathf.Clamp(_verticalPixelCount, 1, Mathf.Max(_targetDescriptor.height, 1));
            float aspect = _targetDescriptor.height > 0 ? _targetDescriptor.width / (float)_targetDescriptor.height : 1.7777f;
            int width = Mathf.Max(1, Mathf.RoundToInt(height * aspect));

            return new Vector2(width, height);
        }

        private int GetPaletteColors()
        {
            if (!_paletteEnabled || _palette == null)
                return 0;

            return _palette.FillShaderColors(_paletteBuffer);
        }

        protected override void Dispose(bool _disposing)
        {
            CoreUtils.Destroy(_signatureMaterial);
            _signatureMaterial = null;
            _signaturePass = null;
        }

        private class SignatureLookPass : ScriptableRenderPass
        {
            private const string PassName = "Signature Look";
            private const string CopyPassName = "Signature Look Copy";
            private const string SourceTextureName = "_SignatureLookSource";

            private readonly Material _material;

            public SignatureLookPass(Material _passMaterial)
            {
                _material = _passMaterial;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            private class PassData
            {
                public Material material;
                public TextureHandle source;
            }

            public override void RecordRenderGraph(RenderGraph _renderGraph, ContextContainer _frameData)
            {
                UniversalCameraData cameraData = _frameData.Get<UniversalCameraData>();
                if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                    return;

                UniversalResourceData resourceData = _frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle activeColor = resourceData.activeColorTexture;
                if (!activeColor.IsValid())
                    return;

                TextureDesc sourceDescriptor = _renderGraph.GetTextureDesc(activeColor);
                sourceDescriptor.name = SourceTextureName;
                sourceDescriptor.clearBuffer = false;

                TextureHandle source = _renderGraph.CreateTexture(sourceDescriptor);
                _renderGraph.AddBlitPass(activeColor, source, Vector2.one, Vector2.zero, passName: CopyPassName);

                using (IRasterRenderGraphBuilder builder = _renderGraph.AddRasterRenderPass(PassName, out PassData passData))
                {
                    passData.material = _material;
                    passData.source = source;

                    builder.UseTexture(source, AccessFlags.Read);

                    if (resourceData.cameraDepthTexture.IsValid())
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                    if (resourceData.cameraNormalsTexture.IsValid())
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData _data, RasterGraphContext _context) =>
                    {
                        Blitter.BlitTexture(_context.cmd, _data.source, new Vector4(1f, 1f, 0f, 0f), _data.material, 0);
                    });
                }
            }
        }
    }
}
