namespace Feedback
{
    using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // ★URP用の参照

namespace Gameplay
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
            // ★変更: TryGet を使用して Vignette を取得
            if (_globalVolume != null && _globalVolume.profile.TryGet(out _vignette))
            {
                _vignette.intensity.value = 0f;
            }
        }

        // GameManagerから毎フレーム呼ばれる
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

            // 1. 【赤優先】フライパンの端にいて少しでも危険度がある場合
            if (_targetDangerLevel > 0.05f) 
            {
                targetColor = _dangerColor;
                targetIntensity = _targetDangerLevel * _maxIntensity;
            }
            // 2. 危険ではない（安全圏にいる）かつ、サークル内にいる場合
            else if (_isSweetSpotInside)
            {
                targetColor = _sweetSpotColor;
                targetIntensity = _maxIntensity * 0.8f; // 緑色の濃さを指定
            }

            // 3. 滑らかに色と強さを変化させる
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * 10f);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetIntensity, Time.deltaTime * 5f);
        }
    }
}
}