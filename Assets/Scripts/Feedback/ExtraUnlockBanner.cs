using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// EXTRAが解放された瞬間の演出。
    /// DifficultySelector.OnExtraUnlocked を購読し、バナーを出して音を鳴らす。
    ///
    /// 発火するのは孵化成功の直後(MarkCleared時)なので、
    /// 成功演出(閃光・ヒットストップ)と重ならないよう少し待ってから出す。
    /// </summary>
    public class ExtraUnlockBanner : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private DifficultySelector _selector;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private AudioController _audioController;

        [Header("文言")]
        public string message = "EXTRA UNLOCKED";

        [Header("タイミング (実時間・秒)")]
        [Tooltip("解放されてからバナーを出すまでの待ち。成功演出と重ならないようにする")]
        public float delay = 1.2f;
        public float fadeIn = 0.35f;
        public float hold = 2.2f;
        public float fadeOut = 0.6f;

        [Header("動き")]
        [Tooltip("出るときにこの倍率から等倍へ縮む(ドンと出る感じ)")]
        public float punchScale = 1.35f;

        private Coroutine _routine;

        private void Awake()
        {
            if (_selector == null) _selector = FindFirstObjectByType<DifficultySelector>();
            if (_audioController == null) _audioController = FindFirstObjectByType<AudioController>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_panel == null) _panel = transform as RectTransform;

            if (_text != null) _text.text = message;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;   // タップを吸わないようにする
                _group.interactable = false;
            }
        }

        private void OnEnable()
        {
            if (_selector != null) _selector.OnExtraUnlocked += Play;
        }

        private void UnsubscribeSelector()
        {
            if (_selector != null) _selector.OnExtraUnlocked -= Play;
        }

        public void Play()
        {
            if (_group == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            if (_text != null) _text.text = message;
            _group.alpha = 0f;

            // ヒットストップでTime.timeScaleが落ちている可能性があるので実時間で待つ
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            _audioController?.PlayUnlock();

            // DOTweenで「ドンと出て、しばらく出しっぱなし、すっと消える」を一本のSequenceに。
            // SetUpdate(true)でヒットストップの影響を受けない。
            _group.DOKill();
            if (_panel != null)
            {
                _panel.DOKill();
                _panel.localScale = Vector3.one * punchScale;
            }

            var seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            seq.Append(_group.DOFade(1f, fadeIn).SetEase(Ease.OutQuad));
            if (_panel != null)
            {
                // 大きめから等倍へ。OutBackで一度わずかに行き過ぎてから収まる
                seq.Join(_panel.DOScale(1f, fadeIn).SetEase(Ease.OutBack));
            }
            seq.AppendInterval(hold);
            seq.Append(_group.DOFade(0f, fadeOut).SetEase(Ease.InQuad));
            seq.OnComplete(() => _routine = null);

            yield break;
        }

        /// <summary>リトライ時などに途中で消したい場合</summary>
        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            if (_group != null) { _group.DOKill(); _group.alpha = 0f; }
            if (_panel != null) { _panel.DOKill(); _panel.localScale = Vector3.one; }
        }

        private void OnDisable()
        {
            UnsubscribeSelector();
            Hide();
        }
    }
}
