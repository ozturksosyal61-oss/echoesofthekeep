// Mobil su (sarnıç, hazne kuyusu): dokusuz yordamsal dalga normali (dünya XZ, 4 yönlü sinüs), Fresnel ile
// reflection probe'dan yansıma taklidi, ışıklardan parlama, köşe rengi (R) ile kenar köpüğü. Opak, derinlik dokusu gerekmez.
Shader "CoopGame/Water"
{
    Properties
    {
        [MainColor] _Color("Deep", Color) = (0.03, 0.08, 0.09, 1)
        _Shallow("Shallow (grazing)", Color) = (0.08, 0.16, 0.16, 1)
        _FoamColor("Foam", Color) = (0.55, 0.58, 0.55, 1)
        _WaveScale("Wave scale", Float) = 1.3
        _WaveSpeed("Wave speed", Float) = 0.35
        _WaveStrength("Wave strength", Range(0, 1)) = 0.22
        _Reflect("Reflection", Range(0, 2)) = 0.85
        _Gloss("Gloss", Range(8, 512)) = 160
        _Spec("Specular", Range(0, 4)) = 1.6
        _FoamWidth("Foam width (vertex R)", Range(0.05, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _Shallow, _FoamColor;
                half _WaveScale, _WaveSpeed, _WaveStrength, _Reflect, _Gloss, _Spec, _FoamWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 sh : TEXCOORD1;   // xyz ortam (probe), w köpük
                half fog : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.sh = half4(SampleSH(half3(0, 1, 0)), i.color.r);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // 4 yönlü sinüs dalgasının eğimi (dx, dz) + köpük için ham yükseklik
            float3 Waves(float2 p, float t)
            {
                const float2 d0 = float2(0.8, 0.6), d1 = float2(-0.55, 0.83), d2 = float2(0.2, -0.98), d3 = float2(-0.93, -0.36);
                float a0 = dot(p, d0) * 1.0 + t * 1.1, a1 = dot(p, d1) * 1.7 + t * 1.5;
                float a2 = dot(p, d2) * 2.9 + t * 2.1, a3 = dot(p, d3) * 4.3 + t * 2.7;
                float2 g = d0 * cos(a0) * 1.0 + d1 * cos(a1) * 0.6 + d2 * cos(a2) * 0.35 + d3 * cos(a3) * 0.2;
                float h = sin(a0) + sin(a1) * 0.6 + sin(a2) * 0.35 + sin(a3) * 0.2;
                return float3(g, h);
            }

            half3 Spec(Light l, half3 n, half3 v, half gloss)
            {
                half3 hv = SafeNormalize(l.direction + v);
                return l.color * (l.distanceAttenuation * pow(saturate(dot(n, hv)), gloss));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 p = i.positionWS.xz * _WaveScale;
                float3 w = Waves(p, t) + Waves(p * 2.3 + 7.1, t * 1.3) * 0.5;
                half3 n = normalize(half3(-w.x * _WaveStrength, 1, -w.y * _WaveStrength));

                half3 v = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                half ndv = saturate(dot(n, v));
                half fres = 0.04 + 0.96 * pow(1 - ndv, 5);

                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), 0.08h, 1.0h) * _Reflect;
                half3 body = lerp(_Color.rgb, _Shallow.rgb, saturate(1 - ndv)) * (i.sh.rgb + 0.15);
                half3 col = lerp(body, refl, saturate(fres));

                half3 spec = Spec(GetMainLight(), n, v, _Gloss);
                #if defined(_ADDITIONAL_LIGHTS)
                uint cnt = GetAdditionalLightsCount();
                for (uint li = 0u; li < cnt; ++li)
                    spec += Spec(GetAdditionalLight(li, i.positionWS), n, v, _Gloss);
                #endif
                col += spec * _Spec;

                // Kenar köpüğü: köşe rengi R (kenarda 1), dalga yüksekliğiyle bozulan ve kayan bant
                half edge = i.sh.w;
                float fn = w.z * 0.18 + sin(i.positionWS.x * 6.1 + t * 2.0) * sin(i.positionWS.z * 5.3 - t * 1.6) * 0.25;
                half foam = saturate((edge - (1 - _FoamWidth) + fn * 0.3) / max(_FoamWidth, 0.01));
                foam *= saturate(0.55 + 0.45 * sin(edge * 14.0 - t * 3.0 + fn * 4.0));
                col = lerp(col, _FoamColor.rgb * (i.sh.rgb + 0.25), foam * 0.75);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
