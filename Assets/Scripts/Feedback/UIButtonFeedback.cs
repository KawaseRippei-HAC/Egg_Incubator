using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay
{
    /// <summary>
    /// UIボタンを押したときの手応え(音・押し込み・光)をまとめて出す。
    /// ボタンと同じGameObjectに付けるだけで動く。
    ///
    /// ★DOTweenは導入していないためコルーチンで実装している。
    /// 依存を増やさずに済むうえ、やっていることは押し込み→跳ね返りのイージングだけなので
    /// 挙動としては同等。DOTweenを入れる場合はPunchRoutineを差し替えればよい。
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
        private Coroutine _routine;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _baseScale = _rt.localScale;
            _audio = FindFirstObjectByType<AudioController>();
            EnsureFlash();
        }

        private void OnDisable()
        {
            // 押した状態のまま非表示になると縮んだまま残るので戻す
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            if (_rt != null) _rt.localScale = _baseScale;
            if (_flash != null) SetFlashAlpha(0f);
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

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            // ヒットストップでTime.timeScaleが落ちていても同じ速さで動くよう実時間で進める
            float t = 0f;
            while (t < pressDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(pressDuration, 0.0001f));
                _rt.localScale = _baseScale * Mathf.Lerp(1f, pressScale, p);
                if (flashAlpha > 0f) SetFlashAlpha(flashAlpha * p);
                yield return null;
            }

            t = 0f;
            while (t < releaseDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(releaseDuration, 0.0001f));
                // 前半で膨らみ、後半で等倍へ収束する
                float s = p < 0.5f
                    ? Mathf.Lerp(pressScale, overshootScale, Mathf.SmoothStep(0f, 1f, p * 2f))
                    : Mathf.Lerp(overshootScale, 1f, Mathf.SmoothStep(0f, 1f, (p - 0.5f) * 2f));
                _rt.localScale = _baseScale * s;

                if (flashAlpha > 0f)
                {
                    float fp = Mathf.Clamp01(t / Mathf.Max(flashDuration, 0.0001f));
                    SetFlashAlpha(flashAlpha * (1f - fp));
                }
                yield return null;
            }

            _rt.localScale = _baseScale;
            SetFlashAlpha(0f);
            _routine = null;
        }
    }
}
