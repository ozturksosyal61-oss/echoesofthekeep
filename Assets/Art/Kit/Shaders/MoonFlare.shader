// Ay ışını parlaması: kameraya dönük (billboard) dörtgen; yumuşak çekirdek + hafif dört kollu yıldız, nabız atar.
// Quad'ın x ölçeği = boyut. Toplamalı, mobilde ucuz.
Shader "CoopGame/MoonFlare"
{
    Properties
    {
        [MainColor][HDR] _Color("Color", Color) = (1.2, 1.5, 2.2, 1)
        _Pulse("Pulse", Float) = 2.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Pulse;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float size = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
                float3 vs = TransformWorldToView(center);
                vs.z += size * 0.5; // yakın yüzeye gömülmesin
                vs.xy += i.positionOS.xy * size;
                o.positionCS = TransformWViewToHClip(vs);
                o.uv = i.uv * 2.0 - 1.0;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float r = length(i.uv);
                half glow = exp(-r * r * 7.0) + 0.35 * exp(-r * 3.0);
                half star = saturate(1.0 - abs(i.uv.x * i.uv.y) * 60.0) * saturate(1.0 - r);
                half pulse = 0.85 + 0.15 * sin(_Time.y * _Pulse);
                return half4(_Color.rgb * (glow + star * 0.5) * pulse * saturate(1.0 - r), 1);
            }
            ENDHLSL
        }
    }
}
