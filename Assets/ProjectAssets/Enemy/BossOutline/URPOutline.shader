Shader "Custom/URPOutline_WebGL_Fixed"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (1, 0, 1, 1)
        _OutlineThickness("Outline Thickness", Range(0, 0.1)) = 0.02
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        // 改回 Geometry+1 確保它在主模型之後渲染，但我們微調 Z 軸緩衝
        "Queue" = "Geometry+1"
    }

    Pass
    {
        Name "Outline"
        Cull Front
        ZWrite On

        // 關鍵：利用 Offset 讓描邊的深度稍微往後推，徹底解決 WebGL 16-bit 深度的 Z-Fighting 鋸齒破碎感
        Offset 1, 1

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        float4 _OutlineColor;
        float _OutlineThickness;

        Varyings vert(Attributes input)
        {
            Varyings output;
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            positionWS += normalWS * _OutlineThickness;
            output.positionCS = TransformWorldToHClip(positionWS);
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            return _OutlineColor;
        }
        ENDHLSL
    }
    }
}