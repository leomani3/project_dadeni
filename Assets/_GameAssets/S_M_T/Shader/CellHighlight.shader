Shader "Deckbuilder/CellHighlight"
{
    Properties
    {
        _Mode ("Mode", Float) = 0
        _FillColor ("Fill Color", Color) = (0, 1, 0.2, 0.35)
        _CenterColor ("Center Color", Color) = (0.6, 1, 0.7, 0.75)
        _BorderColor ("Border Color", Color) = (0.8, 1, 0.85, 0.9)
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.08
        _GradientPower ("Gradient Power", Range(0.1, 8)) = 1.4
        _GlyphThickness ("Glyph Thickness", Range(0.01, 0.5)) = 0.12
        _GlyphRadius ("Glyph Radius", Range(0.05, 1)) = 0.6
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
                float2 uv : TEXCOORD0;
                float2 glyph : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 glyph : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                half4 _FillColor;
                half4 _CenterColor;
                half4 _BorderColor;
                float _BorderWidth;
                float _GradientPower;
                float _GlyphThickness;
                float _GlyphRadius;
                float _HighlightCoef;
            CBUFFER_END

            Varyings Vertex(Attributes _input)
            {
                Varyings _output;
                _output.positionCS = TransformObjectToHClip(_input.positionOS.xyz);
                _output.uv = _input.uv;
                _output.glyph = _input.glyph;

                return _output;
            }

            half4 SampleCell(float2 _uv)
            {
                float2 _offset = _uv - 0.5;
                float _radial = saturate(length(_offset) * 2);
                half4 _color = lerp(_CenterColor, _FillColor, pow(_radial, _GradientPower));

                float _square = max(abs(_offset.x), abs(_offset.y)) * 2;
                float _aa = fwidth(_square) + 1e-5;
                float _border = smoothstep(1 - _BorderWidth - _aa, 1 - _BorderWidth, _square);

                return lerp(_color, _BorderColor, _border);
            }

            float SegmentDistance(float2 _point, float2 _from, float2 _to)
            {
                float2 _toPoint = _point - _from;
                float2 _segment = _to - _from;
                float _projection = saturate(dot(_toPoint, _segment) / dot(_segment, _segment));

                return length(_toPoint - _segment * _projection);
            }

            half4 SampleGlyph(float2 _uv, float _isDot)
            {
                float2 _offset = _uv - 0.5;

                float2 _folded = float2(abs(_offset.x), _offset.y);
                float2 _tip = float2(0, _GlyphRadius);
                float2 _arm = float2(_GlyphRadius * 1.4, -_GlyphRadius);
                float _chevronDistance = SegmentDistance(_folded, _tip, _arm) - _GlyphThickness;

                float _dotDistance = length(_offset) - 0.4;

                float _distance = lerp(_chevronDistance, _dotDistance, _isDot);
                float _aa = fwidth(_distance) + 1e-5;

                half4 _color = _BorderColor;
                _color.a *= 1 - smoothstep(-_aa, _aa, _distance - _BorderWidth);

                return lerp(_color, _FillColor, 1 - smoothstep(-_aa, _aa, _distance));
            }

            half4 SampleReticle(float2 _uv)
            {
                float2 _offset = _uv - 0.5;
                float _radius = length(_offset) * 2;
                float _aa = fwidth(_radius) + 1e-5;

                float _outer = 1 - smoothstep(_GlyphRadius, _GlyphRadius + _aa, _radius);
                float _inner = smoothstep(_GlyphRadius - _GlyphThickness - _aa, _GlyphRadius - _GlyphThickness, _radius);
                float _ring = _outer * _inner;

                float _dot = 1 - smoothstep(_GlyphThickness, _GlyphThickness + _aa, _radius);

                float _axis = min(abs(_offset.x), abs(_offset.y)) * 2;
                float _tickWidth = _GlyphThickness * 0.5;
                float _tick = (1 - smoothstep(_tickWidth, _tickWidth + _aa, _axis))
                    * smoothstep(_GlyphRadius, _GlyphRadius + _aa, _radius)
                    * (1 - smoothstep(1 - _aa, 1, _radius));

                half4 _color = _FillColor;
                _color.a *= saturate(_ring + _dot + _tick);

                return _color;
            }

            half4 Fragment(Varyings _input) : SV_Target
            {
                half4 _color;

                if (_Mode < 0.5)
                    _color = SampleCell(_input.uv);
                else if (_Mode < 1.5)
                    _color = SampleGlyph(_input.uv, _input.glyph.x);
                else
                    _color = SampleReticle(_input.uv);

                _color.a = saturate(_color.a * _HighlightCoef);
                _color.rgb *= _color.a;

                return _color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
