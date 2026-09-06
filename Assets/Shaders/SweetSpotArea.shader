// スイートスポットを「エリア」として見せるための手続き的シェーダー。
//
// テクスチャを一切使わず、オブジェクト空間のXZ半径だけで
//   ・中心から縁へ向かうグラデーションの塗り
//   ・縁を縁取るリング
//   ・中心から外へ広がる波紋(3本)
//   ・ゆっくり回る走査線
// を描く。
//
// ★UVではなくオブジェクト空間の位置を使う理由:
// メッシュはUnity組み込みのCylinderで、キャップのUVマッピングが素直でない。
// また親(pan_main)が非一様スケール(1, 0.5, 1)を持つため、板を回転して
// 貼ると剪断(シアー)が出る。Cylinderは半径0.5・軸Yで固定なので、
// length(objectPos.xz)/0.5 が常に 0(中心)〜1(縁) の綺麗な座標になる。
Shader "Custom/SweetSpotArea"
{
    Properties
    {
        [HDR] _BaseColor   ("塗りの色", Color) = (0.35, 1.0, 0.45, 0.35)
        [HDR] _RingColor   ("リング・波紋の色", Color) = (0.6, 1.0, 0.7, 1.0)
        _Intensity         ("全体の明るさ(スクリプトが変化させる)", Range(0, 4)) = 1

        _FillAlpha         ("塗りの濃さ", Range(0, 1)) = 0.35
        _FillPower         ("塗りのグラデーション(大きいほど縁に寄る)", Range(0.2, 6)) = 1.6

        _EdgeWidth         ("外周リングの太さ", Range(0.01, 0.5)) = 0.12
        _EdgeAlpha         ("外周リングの強さ", Range(0, 4)) = 1.4

        _RippleSpeed       ("波紋が広がる速さ", Range(0, 3)) = 0.5
        _RippleWidth       ("波紋の太さ", Range(0.01, 0.5)) = 0.09
        _RippleAlpha       ("波紋の強さ", Range(0, 4)) = 1.0

        _ScanSpeed         ("走査線の回転速度", Range(0, 3)) = 0.35
        _ScanAlpha         ("走査線の強さ", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SweetSpotArea"
            // 加算合成。パンの上で「光っている」ように見せる
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RingColor;
                float  _Intensity;
                float  _FillAlpha;
                float  _FillPower;
                float  _EdgeWidth;
                float  _EdgeAlpha;
                float  _RippleSpeed;
                float  _RippleWidth;
                float  _RippleAlpha;
                float  _ScanSpeed;
                float  _ScanAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            // 中心rに幅wの線を1本引く(0〜1)
            float Band(float r, float center, float width)
            {
                return saturate(1.0 - abs(r - center) / max(width, 1e-4));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Cylinderは半径0.5・軸Y。0(中心)〜1(縁)へ正規化する
                float r = length(IN.positionOS.xz) / 0.5;
                clip(1.0 - r);   // 円の外は描かない

                // 1. 塗り: 中心から縁へ向かって濃くなるグラデーション
                float fill = pow(saturate(r), _FillPower) * _FillAlpha;

                // 2. 外周リング
                float ringCenter = 1.0 - _EdgeWidth * 0.5;
                float ring = Band(r, ringCenter, _EdgeWidth * 0.5) * _EdgeAlpha;

                // 3. 波紋: 中心から外へ広がる3本。位相をずらして連続的に出す
                float t = _Time.y * _RippleSpeed;
                float ripple = 0.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float phase = frac(t + i / 3.0);
                    // 広がりきる手前で消えるようにフェードさせる
                    ripple += Band(r, phase, _RippleWidth) * (1.0 - phase);
                }
                ripple *= _RippleAlpha;

                // 4. 走査線: ゆっくり回るレーダー状の明るみ
                float ang = atan2(IN.positionOS.z, IN.positionOS.x);
                float scan = saturate(cos(ang - _Time.y * _ScanSpeed * 6.2831853)) ;
                scan = pow(scan, 8.0) * _ScanAlpha * (1.0 - r * 0.3);

                float3 col = _BaseColor.rgb * fill
                           + _RingColor.rgb * (ring + ripple + scan);
                float  a   = (fill * _BaseColor.a)
                           + (ring + ripple + scan) * _RingColor.a;

                col *= _Intensity;
                a    = saturate(a * _Intensity);

                return half4(col, a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
