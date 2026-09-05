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

        // PLAYING中だけtrue。UpdateEffectState()がPLAYING中しか呼ばれないのに対し、
        // Update()は常に回り続けるため、このフラグが無いとPLAYINGを抜けた瞬間の
        // dangerLevelが残ったまま、FALLING/RESULT/TITLEでもビネットが出続けてしまう。
        private bool _isActive = false;

        void Start()
        {
            if (_globalVolume != null && _globalVolume.profile.TryGet(out _vignette))
            {
                _vignette.intensity.value = 0f;
            }
        }

        /// <summary>PLAYING中にGameManagerから毎フレーム呼ばれる。</summary>
        public void UpdateEffectState(float dangerLevel, bool isSweetSpot)
        {
            _isActive = true;
            _targetDangerLevel = dangerLevel;
            _isSweetSpotInside = isSweetSpot;
        }

        /// <summary>
        /// PLAYING以外の状態へ遷移したときにGameManagerから呼ぶ。ビネットを消す。
        /// immediate=falseなら通常のLerpで滑らかに消え、trueなら即座に0にする(リトライ時など)。
        /// </summary>
        public void ClearEffect(bool immediate = false)
        {
            _isActive = false;
            _targetDangerLevel = 0f;
            _isSweetSpotInside = false;

            if (immediate && _vignette != null)
            {
                _vignette.intensity.value = 0f;
            }
        }

        void Update()
        {
            if (_vignette == null) return;

            Color targetColor = _vignette.color.value;
            float targetIntensity = 0f;

            // PLAYING中以外はtargetIntensity=0のまま、下のLerpでビネットが消えていく
            if (_isActive)
            {
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
            }

            // 3. 滑らかに色と強さを変化させる
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * 10f);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetIntensity, Time.deltaTime * 5f);
        }
    }
}