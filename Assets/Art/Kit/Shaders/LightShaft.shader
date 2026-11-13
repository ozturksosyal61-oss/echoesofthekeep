// Ay ışığı huzmesi kartı: toplamalı, çift taraflı. Karta yandan bakınca (ince parlak çizgi olmasın diye) ve
// kameraya çok yaklaşınca söner. Sönme piksel başına (köşe başında büyük kartta yandan bakınca ince çizgi kalıyordu).
Shader "CoopGame/LightShaft"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 0.2)
        _Fade("Edge fade power", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = i.uv;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionWS = ws;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 toCam = GetCameraPositionWS() - i.positionWS;
                float dist = length(toCam);
                half ndv = abs(dot(normalize(i.normalWS), toCam / max(dist, 0.001)));
                half fade = pow(smoothstep(0.12, 0.6, ndv), _Fade) * saturate((dist - 0.4) / 1.6);
                half4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                return half4(_BaseColor.rgb * t.rgb, t.a * _BaseColor.a * fade);
            }
            ENDHLSL
        }
    }
}
