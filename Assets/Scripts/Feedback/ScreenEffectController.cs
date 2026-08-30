using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Feedback
{
    public class ScreenEffectController : MonoBehaviour
    {
        [Header("ポストプロセス設定")]
        [Tooltip("Volume コンポーネントをアタッチ")]
        [SerializeField] private Volume _globalVolume = null;
        
        [Header("色の設定")]
        [SerializeField] private Color _dangerColor = Color.red;
        [SerializeField] private Color _sweetSpotColor = Color.green;
        
        [Tooltip("エフェクトの最大の濃さ")]
        [SerializeField, Range(0f, 1f)] private float _maxIntensity = 0.5f;

        private Vignette _vignette;
        private float _targetDangerLevel = 0f;
        private bool _isSweetSpotInside = false;

        void Start()
        {
            if (_globalVolume != null && _globalVolume.profile.TryGet(out _vignette))
            {
                _vignette.intensity.value = 0f;
            }
        }

        public void UpdateEffectState(float dangerLevel, bool isSweetSpot)
        {
            _targetDangerLevel = dangerLevel;
            _isSweetSpotInside = isSweetSpot;
        }

        void Update()
        {
            if (_vignette == null) return;

            Color targetColor = _vignette.color.value;
            float targetIntensity = 0f;

            // 1. 【緑優先】サークル内にいる場合（危険度に関わらず緑にする）
            if (_isSweetSpotInside)
            {
                targetColor = _sweetSpotColor;
                targetIntensity = _maxIntensity * 0.8f;
            }
            // 2. サークル外であり、かつフライパンの端（危険度がある）場合
            else if (_targetDangerLevel > 0.05f) 
            {
                targetColor = _dangerColor;
                targetIntensity = _targetDangerLevel * _maxIntensity;
            }

            // 3. 滑らかに色と強さを変化させる
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * 10f);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetIntensity, Time.deltaTime * 5f);
        }
    }
}