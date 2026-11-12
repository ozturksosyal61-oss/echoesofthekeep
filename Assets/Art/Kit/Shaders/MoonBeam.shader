// Ay huzmesi (bulmaca 6): kapaksız tüp üzerinde toplamalı hacim taklidi. Kenarlara doğru yumuşak söner (n·v),
// ortada ince parlak çekirdek, boyunca akan dalgalanma ve toz zerreleri, uçlarda yumuşak geçiş. Köşe+piksel başı ucuz.
Shader "CoopGame/MoonBeam"
{
    Properties
    {
        [MainColor][HDR] _Color("Halo", Color) = (0.35, 0.5, 0.85, 1)
        [HDR] _Core("Core", Color) = (1.6, 1.9, 2.6, 1)
        _Flow("Flow speed", Float) = 1.2
        _Density("Wave density", Float) = 0.9
        _Specks("Dust amount", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _Core;
                half _Flow, _Density, _Specks;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 data : TEXCOORD0; half fade : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                float3 n = normalize(TransformObjectToWorldNormal(i.normalOS));
                float3 toCam = GetCameraPositionWS() - ws;
                float dist = length(toCam);
                float len = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)) * 2.0; // tüp y: -1..1
                o.data = float4(saturate(abs(dot(n, toCam / max(dist, 0.001)))), i.uv.y * len, i.uv.x, len);
                o.fade = saturate((dist - 0.3) / 1.2);
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            half4 frag(Varyings i) : SV_Target
            {
                half ndv = i.data.x;
                float along = i.data.y;
                half halo = ndv * ndv;
                half core = pow(ndv, 18.0);
                float s = along * _Density - _Time.y * _Flow;
                half wave = 0.7 + 0.3 * sin(s * 1.7 + sin(s * 0.53) * 2.2);
                float2 g = float2(i.data.z * 10.0, along * 2.2 - _Time.y * _Flow * 0.8);
                float2 f = frac(g) - 0.5;
                half speck = step(1.0 - _Specks * 0.12, Hash(floor(g))) * saturate(1.0 - length(f) * 3.5) * ndv;
                half ends = smoothstep(0.0, 0.6, along) * smoothstep(0.0, 0.25, i.data.w - along);
                half3 c = _Color.rgb * halo * wave + _Core.rgb * core + _Core.rgb * speck * 0.8;
                return half4(c * ends * i.fade, 1);
            }
            ENDHLSL
        }
    }
}
