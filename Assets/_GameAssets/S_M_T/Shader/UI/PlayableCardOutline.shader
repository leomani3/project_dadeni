Shader "Deckbuilder/UI/PlayableCardOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _CardSize ("Card Size (px)", Vector) = (244.64, 328.73, 0, 0)
        _GlowMargin ("Glow Margin (px)", Float) = 40
        _CornerRadius ("Corner Radius (px)", Float) = 17

        _PulseSpeed ("Pulse Speed", Float) = 1.4
        _EdgePulseMin ("Edge Pulse Minimum", Range(0, 1)) = 0.75

        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.45
        _GlowFalloff ("Glow Falloff", Range(0.5, 8)) = 2.5

        _WaveColor ("Wave Color", Color) = (1, 1, 0.8, 1)
        _WaveCount ("Waves In Flight", Float) = 1
        _WaveWidth ("Wave Width", Range(0.02, 1)) = 0.35
        _WaveStrength ("Wave Strength", Range(0, 2)) = 0.9
        _WaveFade ("Wave Fade", Range(0.5, 8)) = 1.6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _CardSize;
            float _GlowMargin;
            float _CornerRadius;
            float _PulseSpeed;
            float _EdgePulseMin;
            float _GlowStrength;
            float _GlowFalloff;
            fixed4 _WaveColor;
            float _WaveCount;
            float _WaveWidth;
            float _WaveStrength;
            float _WaveFade;

            float RoundedBoxDistance(float2 position, float2 halfSize, float radius)
            {
                float2 corner = abs(position) - halfSize + radius;
                return length(max(corner, 0.0)) + min(max(corner.x, corner.y), 0.0) - radius;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;

                float margin = max(_GlowMargin, 0.001);
                float2 quadSize = _CardSize.xy + 2.0 * margin;
                float2 position = (input.texcoord - 0.5) * quadSize;
                float distanceToEdge = RoundedBoxDistance(position, _CardSize.xy * 0.5, _CornerRadius);

                float time = _Time.y * _PulseSpeed;
                float outward = saturate(distanceToEdge / margin);
                float remaining = 1.0 - outward;

                float edgeAntialias = saturate(1.0 - distanceToEdge);
                float edgePulse = lerp(_EdgePulseMin, 1.0, pow(1.0 - frac(time - 0.5), 3.0));

                float glow = pow(remaining, _GlowFalloff) * _GlowStrength * edgePulse;

                float wavePhase = frac(time - outward * max(_WaveCount, 1.0));
                float waveShape = saturate(1.0 - abs(wavePhase - 0.5) * 2.0 / _WaveWidth);
                waveShape = waveShape * waveShape * (3.0 - 2.0 * waveShape);
                float wave = waveShape * pow(remaining, _WaveFade) * _WaveStrength * step(0.0, distanceToEdge);

                float alpha = max(edgeAntialias * edgePulse, glow + wave);

                color.rgb = lerp(color.rgb, _WaveColor.rgb, saturate(wave));
                color.a *= saturate(alpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
