Shader "Hidden/TeamSquare/Pixelize"
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
            Name "PointSample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragNearest
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
