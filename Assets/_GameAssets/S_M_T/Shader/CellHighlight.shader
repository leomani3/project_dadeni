Shader "Deckbuilder/CellHighlight"
{
    Properties
    {
        _HighlightCoef ("Highlight Coefficient", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "CellHighlight"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float _HighlightCoef;
            CBUFFER_END

            Varyings Vertex(Attributes _input)
            {
                Varyings _output;
                _output.positionCS = TransformObjectToHClip(_input.positionOS.xyz);
                _output.color = _input.color;

                return _output;
            }

            half4 Fragment(Varyings _input) : SV_Target
            {
                half4 _color = _input.color;
                _color.a = saturate(_color.a * _HighlightCoef);
                _color.rgb *= _color.a;

                return _color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
