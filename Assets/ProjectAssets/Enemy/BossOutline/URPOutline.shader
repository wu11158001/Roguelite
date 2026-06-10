Shader "Custom/URPOutline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineThickness("Outline Thickness", Range(0, 0.1)) = 0.02
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Name "Outline"
            // 關鍵：只渲染背面，把正面留給原本的模型
            Cull Front
            ZWrite On

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

                // 將頂點座標與法線轉換到世界空間
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // 沿著法線方向外擴模型
                positionWS += normalWS * _OutlineThickness;

                // 轉換到裁剪空間 (Screen Space)
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 回傳發光顏色
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}