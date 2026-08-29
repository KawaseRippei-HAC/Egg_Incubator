Shader "Custom/EggRadialBurst3D"
{
    // 3Dメッシュ(LightBurst_Mesh)による、神秘的で透明感のある放射光漏れ。
    // 頂点カラーR = 根本(0)〜先端(1)の正規化距離。これでソフトなグラデーション減衰を作る。
    // UV(0)のx = このスパイクが出現する危険度の閾値(0〜1)。段階的な非線形出現に使う。
    Properties
    {
        _RayColor ("Ray Color (発光色)", Color) = (0.85, 0.9, 1.0, 1)
        _RayIntensity ("Ray Intensity", Range(0, 10)) = 1.4
        _FalloffPower ("Falloff Power (先端に向かう透明化の滑らかさ)", Range(0.3, 6)) = 1.8
        _RootBoost ("Root Boost (根本の明るさ)", Range(0, 5)) = 1.6
        _FresnelPower ("Fresnel Power (視線角度による透明感)", Range(0.2, 6)) = 2.0
        _FresnelMin ("Fresnel Min (最低限残す透明度)", Range(0, 1)) = 0.15

        [Header(StagedReveal)]
        _RevealSoftness ("Reveal Softness", Range(0.005, 0.5)) = 0.07
        _SparkStrength ("Spark Strength", Range(0, 5)) = 1.2
        _SparkWidth ("Spark Width", Range(0.001, 0.1)) = 0.025

        _CrackAmount ("Crack Amount (Danger Level 0-1)", Range(0, 1)) = 0.0
        _MinAmountToShow ("Min Amount To Show", Range(0, 1)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend One One // 加算合成(透明感のある光の重なりを表現)

        Pass
        {
            Name "LightBurst3DAdditive"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float falloffT     : TEXCOORD2; // 頂点カラーRから
                float threshold    : TEXCOORD3; // UV0.xから(スパイクごとの出現閾値)
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RayColor;
                float _RayIntensity;
                float _FalloffPower;
                float _RootBoost;
                float _FresnelPower;
                float _FresnelMin;
                float _RevealSoftness;
                float _SparkStrength;
                float _SparkWidth;
                float _CrackAmount;
                float _MinAmountToShow;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.falloffT = IN.color.r;   // 0=根本, 1=先端
                OUT.threshold = IN.uv.x;     // このスパイクの出現閾値
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float amount = smoothstep(_MinAmountToShow, 1.0, _CrackAmount);
                if (amount <= 0.0001)
                {
                    discard;
                }

                // 先端に向かうほど滑らかに透明・減衰する(神秘的なグラデーション)
                float falloff = pow(saturate(1.0 - IN.falloffT), _FalloffPower);
                float rootGlow = pow(saturate(1.0 - IN.falloffT), 4.0) * _RootBoost;

                // フレネル: 視線に対して斜めの面ほど強く見せ、透明なガラスのような質感を出す
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 normalWS = normalize(IN.normalWS);
                float fresnel = pow(1.0 - saturate(abs(dot(normalWS, viewDirWS))), _FresnelPower);
                fresnel = lerp(_FresnelMin, 1.0, fresnel);

                // === 段階的(非線形)な出現 ===
                float reveal = smoothstep(IN.threshold - _RevealSoftness, IN.threshold + _RevealSoftness, amount);
                float distFromThreshold = amount - IN.threshold;
                float spark = smoothstep(_SparkWidth, 0.0, abs(distFromThreshold)) * step(0.0, distFromThreshold);

                float shape = (falloff + rootGlow) * fresnel;
                float finalGlow = shape * reveal;

                half3 color = _RayColor.rgb * (_RayIntensity * finalGlow);
                color += _RayColor.rgb * spark * _SparkStrength * falloff;

                return half4(color, 1.0); // 加算合成なのでアルファは実質未使用
            }
            ENDHLSL
        }
    }
    FallBack Off
}
