using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay
{
    /// <summary>
    /// UIボタンを押したときの手応え(音・押し込み・光)をまとめて出す。
    /// ボタンと同じGameObjectに付けるだけで動く。
    ///
    /// アニメーションはDOTweenで行う。
    /// ★ヒットストップ(ImpactFxControllerがTime.timeScaleを落とす)の影響を受けないよう、
    /// 全てのTweenに SetUpdate(true) を付けて実時間で動かす。
    /// ★SetLink(gameObject) を付けて、オブジェクト破棄時にTweenも一緒に消えるようにする。
    ///
    /// ★色はボタン本体のImageではなく、専用の白いオーバーレイで光らせる。
    /// DifficultyButtonsViewが選択状態に応じてImageの色を書き換えるので、
    /// 本体の色を触ると復元時に取り合いになるため。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler
    {
        [Header("押し込み")]
        [Tooltip("押した瞬間に縮む倍率")]
        public float pressScale = 0.90f;
        [Tooltip("戻るときに一度だけ膨らむ倍率")]
        public float overshootScale = 1.06f;
        [Tooltip("縮むまでの時間(実時間・秒)")]
        public float pressDuration = 0.06f;
        [Tooltip("膨らんで元へ戻るまでの時間(実時間・秒)")]
        public float releaseDuration = 0.18f;

        [Header("発光")]
        [Tooltip("押した瞬間に重ねる白い光の濃さ。0で無効")]
        [Range(0f, 1f)] public float flashAlpha = 0.45f;
        [Tooltip("光が消えるまでの時間(実時間・秒)")]
        public float flashDuration = 0.22f;

        [Header("音")]
        [Tooltip("押したときにタップSEを鳴らす")]
        public bool playSound = true;

        private RectTransform _rt;
        private Vector3 _baseScale;
        private Image _flash;
        private AudioController _audio;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _baseScale = _rt.localScale;
            _audio = FindFirstObjectByType<AudioController>();
            EnsureFlash();
        }

        private void OnDisable()
        {
            // 押した状態のまま非表示になると縮んだまま残るので、Tweenを止めて戻す
            // (ExtraButtonは未解放時に非アクティブになる)
            if (_rt != null) { _rt.DOKill(); _rt.localScale = _baseScale; }
            if (_flash != null) { _flash.DOKill(); SetFlashAlpha(0f); }
        }

        private void EnsureFlash()
        {
            if (flashAlpha <= 0f) return;

            var t = transform.Find("PressFlash");
            if (t == null)
            {
                var go = new GameObject("PressFlash", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                _flash = go.AddComponent<Image>();
                _flash.raycastTarget = false;   // 自分がタップを吸わないように
            }
            else
            {
                _flash = t.GetComponent<Image>();
            }
            if (_flash != null) SetFlashAlpha(0f);
        }

        private void SetFlashAlpha(float a)
        {
            if (_flash == null) return;
            _flash.color = new Color(1f, 1f, 1f, a);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Play();
        }

        /// <summary>外から手動で鳴らしたい場合にも使える</summary>
        public void Play()
        {
            if (playSound) _audio?.PlaySE(AudioController.SEType.Tap);

            // 連打で複数のTweenが重なるとスケールが壊れるので、必ず止めてから始める
            _rt.DOKill();
            _rt.localScale = _baseScale;

            DOTween.Sequence()
                .SetUpdate(true)              // ヒットストップの影響を受けない
                .SetLink(gameObject)          // 破棄されたらTweenも消える
                .Append(_rt.DOScale(_baseScale * pressScale, pressDuration).SetEase(Ease.OutQuad))
                // OutBackで戻すと自然に overshoot 分だけ跳ねてから収束する
                .Append(_rt.DOScale(_baseScale, releaseDuration)
                           .SetEase(Ease.OutBack, ComputeBackOvershoot()));

            if (_flash != null && flashAlpha > 0f)
            {
                _flash.DOKill();
                SetFlashAlpha(0f);
                DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .Append(_flash.DOFade(flashAlpha, pressDuration))
                    .Append(_flash.DOFade(0f, flashDuration).SetEase(Ease.OutQuad));
            }
        }

        /// <summary>
        /// overshootScale(例1.06)を Ease.OutBack の overshoot 係数へ変換する。
        /// OutBackの既定値1.70158でおよそ+10%跳ねるので、そこから比例で求める。
        /// </summary>
        private float ComputeBackOvershoot()
        {
            float amount = Mathf.Max(overshootScale - 1f, 0f);
            return Mathf.Max(0.01f, amount / 0.10f * 1.70158f);
        }
    }
}
