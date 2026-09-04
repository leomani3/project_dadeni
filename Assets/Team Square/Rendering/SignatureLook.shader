Shader "Hidden/TeamSquare/SignatureLook"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "SignatureLook"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #define SIGNATURE_MAX_PALETTE_COLORS 64

            float4 _PaletteColors[SIGNATURE_MAX_PALETTE_COLORS];
            int _PaletteCount;
            float _PaletteStrength;
            float _DitherStrength;
            float _DitherScale;

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthEdgeStrength;
            float _NormalEdgeStrength;
            float _EdgeThreshold;

            float4 _HazeColor;
            float _HazeStart;
            float _HazeEnd;
            float _HazeStrength;
            float _HazeFalloff;
            float _HazeAffectsSky;

            float2 _PixelGrid;
            float _PixelizeEnabled;

            static const float BayerMatrix8[64] =
            {
                 0.0, 32.0,  8.0, 40.0,  2.0, 34.0, 10.0, 42.0,
                48.0, 16.0, 56.0, 24.0, 50.0, 18.0, 58.0, 26.0,
                12.0, 44.0,  4.0, 36.0, 14.0, 46.0,  6.0, 38.0,
                60.0, 28.0, 52.0, 20.0, 62.0, 30.0, 54.0, 22.0,
                 3.0, 35.0, 11.0, 43.0,  1.0, 33.0,  9.0, 41.0,
                51.0, 19.0, 59.0, 27.0, 49.0, 17.0, 57.0, 25.0,
                15.0, 47.0,  7.0, 39.0, 13.0, 45.0,  5.0, 37.0,
                63.0, 31.0, 55.0, 23.0, 61.0, 29.0, 53.0, 21.0
            };

            float SceneEyeDepth(float rawDepth)
            {
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
                    #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }

                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            bool IsSkyDepth(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                return rawDepth <= 0.000001;
                #else
                return rawDepth >= 0.999999;
                #endif
            }

            float3 NearestPaletteColor(float3 color)
            {
                float bestDistance = 1000000.0;
                float3 bestColor = color;

                for (int i = 0; i < _PaletteCount; i++)
                {
                    float3 delta = (color - _PaletteColors[i].rgb) * float3(0.5, 1.0, 0.35);
                    float squaredDistance = dot(delta, delta);

                    if (squaredDistance < bestDistance)
                    {
                        bestDistance = squaredDistance;
                        bestColor = _PaletteColors[i].rgb;
                    }
                }

                return bestColor;
            }

            float DetectEdge(float2 sampleUv, float2 grid, float centerEyeDepth)
            {
                float2 offset = _OutlineThickness / grid;

                float2 uvTopLeft = sampleUv + float2(-offset.x, -offset.y);
                float2 uvBottomRight = sampleUv + float2(offset.x, offset.y);
                float2 uvTopRight = sampleUv + float2(offset.x, -offset.y);
                float2 uvBottomLeft = sampleUv + float2(-offset.x, offset.y);

                float depthTopLeft = SceneEyeDepth(SampleSceneDepth(uvTopLeft));
                float depthBottomRight = SceneEyeDepth(SampleSceneDepth(uvBottomRight));
                float depthTopRight = SceneEyeDepth(SampleSceneDepth(uvTopRight));
                float depthBottomLeft = SceneEyeDepth(SampleSceneDepth(uvBottomLeft));

                float depthEdge = length(float2(depthTopLeft - depthBottomRight, depthTopRight - depthBottomLeft));
                depthEdge /= max(centerEyeDepth, 0.0001);

                float3 normalTopLeft = SampleSceneNormals(uvTopLeft);
                float3 normalBottomRight = SampleSceneNormals(uvBottomRight);
                float3 normalTopRight = SampleSceneNormals(uvTopRight);
                float3 normalBottomLeft = SampleSceneNormals(uvBottomLeft);

                float3 normalDelta = abs(normalTopLeft - normalBottomRight) + abs(normalTopRight - normalBottomLeft);
                float normalEdge = length(normalDelta);

                float edge = depthEdge * _DepthEdgeStrength + normalEdge * _NormalEdgeStrength;

                return smoothstep(_EdgeThreshold, _EdgeThreshold + 0.05, edge);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                bool pixelize = _PixelizeEnabled > 0.5;
                float2 grid = pixelize ? _PixelGrid : _BlitTextureSize;
                float2 sampleUv = pixelize ? (floor(uv * grid) + 0.5) / grid : uv;

                float3 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, sampleUv, 0).rgb;

                float rawDepth = SampleSceneDepth(sampleUv);
                float eyeDepth = SceneEyeDepth(rawDepth);
                bool isSky = IsSkyDepth(rawDepth);

                if (_OutlineColor.a > 0.0 && !isSky)
                {
                    float edge = DetectEdge(sampleUv, grid, eyeDepth);
                    color = lerp(color, _OutlineColor.rgb, saturate(edge) * _OutlineColor.a);
                }

                if (_HazeStrength > 0.0)
                {
                    float fade = saturate((eyeDepth - _HazeStart) / max(_HazeEnd - _HazeStart, 0.0001));
                    fade = pow(fade, max(_HazeFalloff, 0.0001));
                    fade = isSky ? _HazeAffectsSky : fade;
                    color = lerp(color, _HazeColor.rgb, saturate(fade * _HazeStrength));
                }

                if (_PaletteCount > 0 && _PaletteStrength > 0.0)
                {
                    color = saturate(color);

                    #ifndef UNITY_COLORSPACE_GAMMA
                    color = LinearToSRGB(color);
                    #endif

                    float2 ditherPixel = floor(sampleUv * grid / max(_DitherScale, 1.0));
                    int ditherIndex = (int)(fmod(ditherPixel.y, 8.0) * 8.0 + fmod(ditherPixel.x, 8.0));
                    float threshold = BayerMatrix8[ditherIndex] * (1.0 / 64.0) - 0.5;

                    float3 dithered = saturate(color + threshold * _DitherStrength);
                    color = lerp(color, NearestPaletteColor(dithered), _PaletteStrength);

                    #ifndef UNITY_COLORSPACE_GAMMA
                    color = SRGBToLinear(color);
                    #endif
                }

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
