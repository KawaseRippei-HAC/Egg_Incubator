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

        public DifficultyParams GetDifficultyParams(float progress)
        {
            progress = Mathf.Clamp01(progress);

            DifficultyParams parameters = new DifficultyParams();
            
            parameters.forceMultiplier = Mathf.Lerp(minForceMultiplier, maxForceMultiplier, progress);
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