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
                float dangerIntensity = _targetDangerLevel > 0.05f
                    ? _targetDangerLevel * _maxIntensity
                    : 0f;

                if (_isSweetSpotInside)
                {
                    // ★以前は「サークル内なら危険度に関わらず緑」だった。
                    // スイートスポットは端寄りに出現し、滞在中は卵の暴れも強くなる
                    // ハイリスク・ハイリターンの場所なので、危険を緑で塗り潰すと
                    // 「おいしいが危ない」という肝心の情報が消えてしまう。
                    // 緑(報酬)と赤(危険)をブレンドし、強度は両者の大きい方を採る。
                    targetColor = Color.Lerp(_sweetSpotColor, _dangerColor, _targetDangerLevel);
                    targetIntensity = Mathf.Max(_maxIntensity * 0.8f, dangerIntensity);
                }
                else if (dangerIntensity > 0f)
                {
                    targetColor = _dangerColor;
                    targetIntensity = dangerIntensity;
                }
            }

            // 3. 滑らかに色と強さを変化させる
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * 10f);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetIntensity, Time.deltaTime * 5f);
        }
    }
}