Shader "Custom/EggCrackGlow"
{
    // ひび割れ発光シェーダー(プロシージャル・非線形進行版)
    // ・UVテクスチャを使わず、オブジェクト空間の3D Voronoiパターンで亀裂網を生成 → 継ぎ目なし
    // ・危険度(_CrackAmount)の上昇に対して、亀裂は滑らかにフェードインするのではなく
    //   不規則なタイミングで「ビビッ」と閾値を超えて出現し(ジャギー+スパーク)、
    //   卵全体に散らばりながら本数を増やしていく
    Properties
    {
        [MainTexture] _BaseMap ("Base Color (Shell)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color Tint", Color) = (1,1,1,1)

        [Header(Crack Pattern)]
        _CrackScale ("Crack Density (Voronoiセルの細かさ)", Range(1, 20)) = 7.0
        _CrackEdgeWidth ("Crack Line Width (亀裂の太さ)", Range(0.005, 0.3)) = 0.045
        _GrowthSoftness ("Growth Edge Softness (出現の滲み幅、小さいほどビビッと出る)", Range(0.005, 0.5)) = 0.05
        _LocalVariation ("Local Variation (亀裂内のムラの強さ)", Range(0, 1)) = 0.55
        _Jaggedness ("Jaggedness (不規則な進行の歪み)", Range(0, 1)) = 0.35
        _JagFrequency ("Jag Frequency (歪みの細かさ)", Range(1, 30)) = 11.0

        [Header(Spark)]
        _SparkStrength ("Spark Strength (出現の瞬間の閃光の強さ)", Range(0, 5)) = 2.0
        _SparkWidth ("Spark Width (閃光が続く危険度の幅)", Range(0.001, 0.1)) = 0.02

        [Header(Emission)]
        _EmissionColor ("Emission Color", Color) = (1.0, 0.55, 0.15, 1)
        _EmissionIntensity ("Emission Intensity (HDR boost)", Range(0, 10)) = 3.0

        // C# から material.SetFloat("_CrackAmount", dangerLevel) で制御する
        _CrackAmount ("Crack Amount (Danger Level 0-1)", Range(0, 1)) = 0.0

        [Header(Groove)]
        // ★視認性のために追加。
        // 卵の地色はクリーム色(0.95,0.93,0.85)でほぼ白なので、そこへ発光を「加算」しても
        // 白に飽和するだけで亀裂が見えない(_EmissionIntensityをいくら上げても変わらない)。
        // 実際のヒビは「暗い溝の奥が光っている」ものなので、亀裂部分の下地を暗くして
        // コントラストを作る。
        _CrackDarkness ("亀裂の溝の暗さ (0=真っ黒 / 1=暗くしない)", Range(0, 1)) = 0.12
        _GrooveWiden ("溝を発光より太くする量", Range(1, 4)) = 1.8

        _Smoothness ("Smoothness", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float3 positionOS  : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _CrackAmount;
                float _CrackDarkness;
                float _GrooveWiden;
                float _Smoothness;
                float _CrackScale;
                float _CrackEdgeWidth;
                float _GrowthSoftness;
                float _LocalVariation;
                float _Jaggedness;
                float _JagFrequency;
                float _SparkStrength;
                float _SparkWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            // ===== プロシージャル亀裂パターン(3D Voronoi, UV不使用=継ぎ目なし) =====

            float3 Hash3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                            dot(p, float3(269.5, 183.3, 246.1)),
                            dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float Hash1(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453123);
            }

            // 3D Voronoi: f1(最近セルまでの距離), f2(次に近いセルまでの距離), cellId(セルの整数座標)
            void Voronoi3D(float3 p, out float f1, out float f2, out float3 cellId)
            {
                float3 baseCell = floor(p);
                float3 fracPos = frac(p);
                f1 = 8.0; f2 = 8.0; cellId = 0;

                [unroll]
                for (int z = -1; z <= 1; z++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 offset = float3(x, y, z);
                            float3 cell = baseCell + offset;
                            float3 h = Hash3(cell);
                            float3 r = offset + h - fracPos;
                            float d = dot(r, r);
                            if (d < f1) { f2 = f1; f1 = d; cellId = cell; }
                            else if (d < f2) { f2 = d; }
                        }
                    }
                }
                f1 = sqrt(f1);
                f2 = sqrt(f2);
            }

            // 亀裂マスク: 0=発光なし, 1=完全に発光した亀裂
            // growthFactor(危険度 0〜1)が上がるほど、卵全体に散らばった亀裂セルの"本数"が増えていく。
            // 各セル・各点の「出現タイミング」に不規則な歪み(ジャギー)を加えることで、
            // 滑らかなフェードインではなく「ビビッ」と閾値を超えて出現する非線形な進行に見せる。
            float CrackMask(float3 objectPos, float growthFactor, out float sparkMask, out float grooveMask)
            {
                float3 p = objectPos * _CrackScale;
                float f1, f2;
                float3 cellId;
                Voronoi3D(p, f1, f2, cellId);

                // セル境界(Voronoiエッジ)= 亀裂の線そのもの
                float edge = 1.0 - smoothstep(0.0, _CrackEdgeWidth, f2 - f1);
                // 溝は発光より少し太くする。光る芯の周りに暗い縁ができて立体的に見える
                float wide = 1.0 - smoothstep(0.0, _CrackEdgeWidth * _GrooveWiden, f2 - f1);

                // 基本の出現タイミング(セル単位 + 局所的なムラ)
                float cellThreshold = Hash1(cellId);
                float localThreshold = Hash1(floor(p * 3.0) + cellId * 0.37);
                float threshold = lerp(cellThreshold, localThreshold, _LocalVariation);

                // ジャギー: 高周波ノイズで閾値そのものを歪ませ、
                // 「一部は早く割れ、一部は遅れて割れる」不規則な進行(=線が走る感じ)を作る
                float jag = (Hash1(floor(p * _JagFrequency)) - 0.5) * 2.0 * _Jaggedness * 0.5;
                threshold = saturate(threshold + jag);

                // シャープな閾値切り替え = 「ビビッ」と一瞬で出現する非線形な進行
                float reveal = smoothstep(threshold - _GrowthSoftness, threshold + _GrowthSoftness, growthFactor);

                // スパーク: 閾値を超えた直後だけ一瞬明るく閃く(亀裂が走った瞬間の閃光)
                float distFromThreshold = growthFactor - threshold;
                float spark = smoothstep(_SparkWidth, 0.0, abs(distFromThreshold)) * step(0.0, distFromThreshold);

                sparkMask = spark * edge;
                grooveMask = wide * reveal;
                return edge * reveal;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lightColor = mainLight.color * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half NdotL = saturate(dot(normalWS, mainLight.direction));

                half3 ambient = SampleSH(normalWS) * baseColor;
                half3 diffuse = baseColor * lightColor * NdotL;

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                half spec = pow(saturate(dot(normalWS, halfDir)), lerp(8, 64, _Smoothness)) * _Smoothness;

                half3 litColor = ambient + diffuse + spec * lightColor;

                // === ひび割れ発光(プロシージャル・非線形進行・全方位対応) ===
                float sparkMask;
                float grooveMask;
                float crackMask = CrackMask(IN.positionOS, _CrackAmount, sparkMask, grooveMask);

                // ★まず下地を暗くする。
                // 卵はほぼ白なので、発光を加算するだけでは白に飽和して亀裂が見えない。
                // 溝を暗く落としてから発光を乗せることで、はっきりしたヒビになる。
                litColor *= lerp(1.0, _CrackDarkness, saturate(grooveMask));

                half3 emission = crackMask * _EmissionColor.rgb * _EmissionIntensity;
                // スパーク: 出現の瞬間だけ白っぽく強い閃光を上乗せ
                emission += sparkMask * _SparkStrength * lerp(_EmissionColor.rgb, half3(1,1,1), 0.6);

                half3 finalColor = litColor + emission;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                OUT.positionHCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
