using UnityEngine;
using Proto.Core;

namespace Gameplay
{
    public class DifficultyManager :  MonoBehaviour
    {
        [Header("Force Multiplier (力の強さ")]
        [SerializeField] private float minForceMultiplier = 1f;
        [SerializeField] private float maxForceMultiplier = 5f;
        
        [Header("Force Interval (力を加える間隔)")]
        [SerializeField] private float maxForceInterval = 2.0f;
        [SerializeField] private float minForceInterval = 0.5f;

        [Header("Direction Variance (方向のばらつき)")]
        [SerializeField] private float minDirectionVariance = 0.1f;
        [SerializeField] private float maxDirectionVariance = 1.0f;

        [Header("Max Linear Speed (最高移動速度)")]
        [SerializeField] private float minMaxLinearSpeed = 2f;
        [SerializeField] private float maxMaxLinearSpeed = 8f;

        [Header("Max Angular Speed (最高回転速度)")]
        [SerializeField] private float minMaxAngularSpeed = 5f;
        [SerializeField] private float maxMaxAngularSpeed = 20f;

        [Header("難易度カーブ")]
        [Tooltip("進行度(0〜1)を、上の各パラメータの補間位置へ変換するカーブ。\n" +
                 "★直線のままだと難易度が一定の割合で上がり続けるだけで、山場も谷も作れない。\n" +
                 "このカーブで『静かな序盤 → 一気に荒れる山場 → 最後の追い込み』といった\n" +
                 "緩急をInspectorだけで作れる。5パラメータ共通(個別に持つと調整が破綻するため)。\n" +
                 "直線(0,0)-(1,1)にすれば従来と数値レベルで完全に一致する")]
        [SerializeField]
        private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("危険度カーブ")]
        [Tooltip("パン中心からの正規化距離(0〜1)を危険度へ変換するカーブ。\n" +
                 "直線だと『縁に近づくほど一定の割合で危険』になるが、実際は\n" +
                 "端の手前から一気に緊張が高まるほうが伝わりやすい")]
        [SerializeField]
        private AnimationCurve dangerCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // NOTE(2025/08/29): 以前ここに「EggController.cs にも同名のグローバルな
        // DifficultyParams があるため完全修飾する」というNOTEがあったが、
        // 現在は EggController.cs も Proto.Core.DifficultyParams を使うよう統一済み。
        public Proto.Core.DifficultyParams GetDifficultyParams(float progress)
        {
            progress = Mathf.Clamp01(progress);
            // カーブを通した値を補間位置として使う
            float t = Mathf.Clamp01(difficultyCurve != null && difficultyCurve.length > 0
                ? difficultyCurve.Evaluate(progress)
                : progress);

            Proto.Core.DifficultyParams parameters = new Proto.Core.DifficultyParams();

            parameters.forceMultiplier = Mathf.Lerp(minForceMultiplier, maxForceMultiplier, t);
            parameters.forceInterval = Mathf.Lerp(maxForceInterval, minForceInterval, t);
            parameters.directionVariance = Mathf.Lerp(minDirectionVariance, maxDirectionVariance, t);
            parameters.maxLinearSpeed = Mathf.Lerp(minMaxLinearSpeed, maxMaxLinearSpeed, t);
            parameters.maxAngularSpeed = Mathf.Lerp(minMaxAngularSpeed, maxMaxAngularSpeed, t);
            return parameters;
        }

        public float CalculateDanger(float normalizedDistance)
        {
            float d = Mathf.Clamp01(normalizedDistance);
            return Mathf.Clamp01(dangerCurve != null && dangerCurve.length > 0
                ? dangerCurve.Evaluate(d)
                : d);
        }
    }
    
}