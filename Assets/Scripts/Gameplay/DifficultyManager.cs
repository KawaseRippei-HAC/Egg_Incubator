using Proto.Core;
using UnityEngine;

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

        // NOTE(2025/08/29): Assets/Scripts/Input/EggController.cs にも同名の
        // グローバル(名前空間なし)な DifficultyParams 構造体が存在し、こちらの方が
        // 名前解決で優先されてしまうため、Proto.Core.DifficultyParams を明示的に
        // 完全修飾する。EggController.cs 側は変更していません。
        public Proto.Core.DifficultyParams GetDifficultyParams(float progress)
        {
            progress = Mathf.Clamp01(progress);

            Proto.Core.DifficultyParams parameters = new Proto.Core.DifficultyParams();
            
            // progressに応じて各値を連続補間(Lerp)する
            parameters.forceMultiplier = Mathf.Lerp(minForceMultiplier, maxForceMultiplier, progress);
            // Intervalのみ、時間が進むにつれて値が小さく（間隔が短く）なる点に注意
            parameters.forceInterval = Mathf.Lerp(maxForceInterval, minForceInterval, progress); 
            parameters.directionVariance = Mathf.Lerp(minDirectionVariance, maxDirectionVariance, progress);
            parameters.maxLinearSpeed = Mathf.Lerp(minMaxLinearSpeed, maxMaxLinearSpeed, progress);
            parameters.maxAngularSpeed = Mathf.Lerp(minMaxAngularSpeed, maxMaxAngularSpeed, progress);
            
            return parameters;
        }

        public float CalculateDanger(float normalizedDistance)
        {
            return Mathf.Clamp01(normalizedDistance);
        }
    }
    
}