Shader "Custom/ProceduralSkybox_URP"
{
    Properties
    {
        [Header(Day and Night)]
        _DayColorTop("Day Color Top", Color) = (0.4, 0.7, 1.0, 1.0)
        _DayColorBottom("Day Color Bottom", Color) = (0.8, 0.9, 1.0, 1.0)
        _NightColorTop("Night Color Top", Color) = (0.02, 0.02, 0.1, 1.0)
        _NightColorBottom("Night Color Bottom", Color) = (0.1, 0.1, 0.2, 1.0)
        _HorizonColor("Horizon Glow", Color) = (0.9, 0.95, 1.0, 1.0)

        [Header(Sun)]
        _SunColor("Sun Color", Color) = (1.0, 0.9, 0.7, 1.0)
        _SunDirection("Sun Direction", Vector) = (0.0, 0.8, 0.6, 0.0)
        _SunSize("Sun Size", Range(0.0, 0.2)) = 0.05
        _SunIntensity("Sun Intensity", Range(0.0, 3.0)) = 1.0

        [Header(Moon)]
        _MoonColor("Moon Color", Color) = (0.9, 0.9, 1.0, 1.0)
        _MoonDirection("Moon Direction", Vector) = (0.0, -0.6, -0.8, 0.0)
        _MoonSize("Moon Size", Range(0.0, 0.2)) = 0.04
        _MoonIntensity("Moon Intensity", Range(0.0, 2.0)) = 0.8
        _MoonPhase("Moon Phase", Range(-1.0, 1.0)) = 0.0

        [Header(Stars)]
        _StarsIntensity("Stars Intensity", Range(0.0, 1.0)) = 0.5
        _StarsTwinkleSpeed("Stars Twinkle Speed", Range(0.0, 10.0)) = 2.0
        _StarsSeed("Stars Seed", Float) = 0.0

        [Header(Volumetric Clouds)]
        _CloudTex("Cloud Texture (2D)", 2D) = "white" {}
        _CloudColor("Cloud Color", Color) = (1.0, 1.0, 1.0, 0.5)
        _CloudDensity("Cloud Density", Range(0.0, 2.0)) = 0.8
        _CloudScale("Cloud Scale", Range(0.1, 5.0)) = 2.0
        _CloudSpeed("Cloud Speed", Vector) = (0.1, 0.05, 0.0, 0.0)
        _CloudHeight("Cloud Height (Y offset)", Range(-1.0, 1.0)) = 0.3

        [Header(Time Control)]
        _DayNightBlend("Day/Night Blend", Range(0.0, 1.0)) = 1.0
        _AutoBlend("Auto Blend (Sun Y)", Float) = 1.0
    }

    SubShader
    {
        Tags{ "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ CLOUD_TEXTURE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 属性映射
            CBUFFER_START(UnityPerMaterial)
                float4 _DayColorTop, _DayColorBottom, _NightColorTop, _NightColorBottom, _HorizonColor;
                float4 _SunColor, _SunDirection;
                float _SunSize, _SunIntensity;
                float4 _MoonColor, _MoonDirection;
                float _MoonSize, _MoonIntensity, _MoonPhase;
                float _StarsIntensity, _StarsTwinkleSpeed, _StarsSeed;
                float _DayNightBlend, _AutoBlend;
                float4 _CloudColor;
                float _CloudDensity, _CloudScale;
                float2 _CloudSpeed;
                float _CloudHeight;
            CBUFFER_END

            // 纹理资源
            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);
            float4 _CloudTex_ST;

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir : TEXCOORD0;
                float2 uv_cloud : TEXCOORD1;
            };

            // 快速随机函数（星星用）
            float fastRand(float3 seed)
            {
                return frac(sin(dot(seed, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            // 过程噪声（无纹理模式）
            float simpleNoise(float3 p)
            {
                return frac(sin(p.x * 121.4 + p.y * 245.1 + p.z * 354.3) * 437.6);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                // URP 顶点变换
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.worldDir = TransformObjectToWorld(v.vertex.xyz);

                // 云 UV：球面映射
                float3 dir = normalize(o.worldDir);
                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.1591 + 0.5,
                    dir.y * 0.5 + 0.5
                );
                o.uv_cloud = uv * _CloudScale + _CloudSpeed * _Time.y;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);
                float up = dir.y;

                // 1. 垂直渐变
                float t = saturate(up * 0.5 + 0.5);

                // 2. 昼夜混合因子
                float blend = _DayNightBlend;
                if (_AutoBlend > 0.5)
                    blend = saturate(_SunDirection.y * 0.8 + 0.3);

                // 3. 基础天空颜色
                float3 dayColor = lerp(_DayColorBottom.rgb, _DayColorTop.rgb, t);
                float3 nightColor = lerp(_NightColorBottom.rgb, _NightColorTop.rgb, t);
                float3 skyColor = lerp(nightColor, dayColor, blend);

                // 4. 地平线辉光
                float horizonFactor = 1.0 - abs(up);
                horizonFactor = saturate(horizonFactor * 1.2);
                skyColor = lerp(skyColor, _HorizonColor.rgb, horizonFactor * 0.3);

                // 5. 太阳
                if (blend > 0.1)
                {
                    float3 sunDir = normalize(_SunDirection.xyz);
                    float sunDot = dot(dir, sunDir);
                    float sunDisk = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                    float sunGlow = pow(max(0, sunDot), 50.0) * 0.2;
                    skyColor += (sunDisk + sunGlow) * _SunColor.rgb * _SunIntensity;
                }

                // 6. 月亮
                float moonVisibility = 1.0 - blend;
                if (moonVisibility > 0.01)
                {
                    float3 moonDir = normalize(_MoonDirection.xyz);
                    float moonDot = dot(dir, moonDir);
                    float moonDisk = smoothstep(1.0 - _MoonSize, 1.0, moonDot);
                    float phaseMask = saturate(moonDot * 0.5 + 0.5);
                    float phase = lerp(phaseMask, 1.0, _MoonPhase * 0.5 + 0.5);
                    float moonGlow = pow(max(0, moonDot), 20.0) * 0.1;
                    float3 moonLight = (moonDisk * phase + moonGlow) * _MoonColor.rgb * _MoonIntensity * moonVisibility;
                    skyColor += moonLight;
                }

                // 7. 星星
                if (_StarsIntensity > 0.0 && moonVisibility > 0.1)
                {
                    float starVis = moonVisibility * _StarsIntensity;
                    float3 seed = dir * 1000.0 + _StarsSeed;
                    float starNoise = fastRand(seed);
                    float twinkle = sin(seed.x * 20.0 + _Time.y * _StarsTwinkleSpeed) * 0.5 + 0.5;
                    float starThreshold = 0.995 + (twinkle * 0.003);
                    float star = step(starThreshold, starNoise);
                    skyColor += star * starVis * (0.5 + twinkle * 0.5);
                }

                // 8. 体积云（修复作用域问题）
                if (_CloudDensity > 0.0)
                {
                    float heightWeight = 1.0 - abs(up - _CloudHeight);
                    heightWeight = saturate(heightWeight * 3.0);

                    // 先声明 cloud 变量
                    float cloud = 0.0;

                    #if defined(CLOUD_TEXTURE)
                        cloud = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, i.uv_cloud).r;
                    #else
                        float3 p = dir * 2.0 + _Time.y * float3(_CloudSpeed.x, _CloudSpeed.y, 0);
                        cloud = simpleNoise(p);
                        cloud = cloud * 0.6 + simpleNoise(p * 2.0 + 1.0) * 0.4;
                    #endif

                    float cloudIntensity = cloud * _CloudDensity * heightWeight;
                    float3 cloudColor = _CloudColor.rgb * lerp(0.3, 1.0, blend);
                    skyColor = lerp(skyColor, cloudColor, cloudIntensity * 0.5);
                }

                return half4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
