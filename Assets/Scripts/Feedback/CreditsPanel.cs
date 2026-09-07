using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// タイトルから開くクレジット表示。
    ///
    /// ★ここに書かれている内容は「素材の出典一覧」であって、
    /// ライセンス条件を満たしているかの保証ではない。
    /// CC BY 系は作者名とライセンス名の明記が必要など条件が素材ごとに違うので、
    /// 提出前に各配布ページの条件を確認して本文を直すこと。
    /// </summary>
    public class CreditsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMPro.TextMeshProUGUI _body;
        [SerializeField] private UnityEngine.UI.ScrollRect _scroll;

        [TextArea(10, 40)]
        [Tooltip("クレジット本文。Inspectorで直接編集できる")]
        public string credits =
"■ BGM\n" +
"Children's March Theme\n" +
"  opengameart.org/content/childrens-march-theme\n" +
"Two Simple Game Music Loops\n" +
"  opengameart.org/content/two-simple-game-music-loops\n" +
"やらかした Game Over\n" +
"  opengameart.org/content/yarakashitagameover-music\n" +
"Win Jingle\n" +
"  opengameart.org/content/win-jingle\n" +
"\n" +
"■ 効果音 (Freesound)\n" +
"Faulkin / sounds/336262\n" +
"tbone1999 / sounds/401706\n" +
"deleted_user_2104797 / sounds/164576\n" +
"egomassive / sounds/536759\n" +
"\n" +
"■ 効果音 (Pixabay)\n" +
"screen-tap-38717\n" +
"whistle-slide-down-323778\n" +
"game-start-6104\n" +
"success-fanfare-trumpets-6185\n" +
"success-videogame-sfx-mrstokes302-423626\n" +
"musical-playful-failure-310480\n" +
"rolling-24b-100544\n" +
"heartbeat-single-383748\n" +
"egg-crack-3-389261\n" +
"household-egg-cracking-6844\n" +
"egg-crack21-85839\n" +
"\n" +
"■ 効果音\n" +
"効果音ラボ\n" +
"  soundeffect-lab.info\n" +
"\n" +
"■ 3Dモデル\n" +
"Animated Easy Enemies / Quaternius\n" +
"  quaternius.itch.io/animated-easy-enemies\n" +
"Kitchen / styloo\n" +
"  styloo.itch.io/kitchen\n" +
"\n" +
"■ フォント\n" +
"M PLUS Rounded 1c\n" +
"  SIL Open Font License 1.1\n";

        // ★このコンポーネントは「常にアクティブなオブジェクト」(Canvasなど)に付けること。
        // パネル自身に付けると、パネルが非アクティブな間はAwake()が走らず、
        // 最初のOpen()でSetActive(true)した瞬間に初めてAwake()が実行され、
        // その中のSetActive(false)で即座に閉じてしまう
        // (1回目は開かず2回目から開く、という症状になる)。
        private void Awake()
        {
            if (_panel == null)
            {
                Debug.LogError($"[{nameof(CreditsPanel)}] _panel が未設定です。", this);
                return;
            }
            if (_panel == gameObject)
            {
                Debug.LogWarning($"[{nameof(CreditsPanel)}] このコンポーネントは常時アクティブな" +
                                 "オブジェクトに付けてください。パネル自身に付けると初回が開きません。", this);
            }

            if (_body != null) _body.text = credits;
            _panel.SetActive(false);
        }

        [Header("開閉アニメーション")]
        public float openDuration = 0.25f;
        public float closeDuration = 0.18f;
        [Tooltip("この倍率から等倍へ開く")]
        public float openFromScale = 0.92f;

        private CanvasGroup _group;

        /// <summary>タイトルのCREDITSボタンから呼ぶ</summary>
        public void Open()
        {
            if (_body != null) _body.text = credits;
            _panel.SetActive(true);
            // 開くたびに先頭から読ませる
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;

            var g = GetGroup();
            var t = _panel.transform;
            g.DOKill(); t.DOKill();
            g.alpha = 0f;
            t.localScale = Vector3.one * openFromScale;

            DOTween.Sequence().SetUpdate(true).SetLink(_panel)
                .Append(g.DOFade(1f, openDuration).SetEase(Ease.OutQuad))
                .Join(t.DOScale(1f, openDuration).SetEase(Ease.OutBack));
        }

        /// <summary>閉じるボタンから呼ぶ</summary>
        public void Close()
        {
            var g = GetGroup();
            var t = _panel.transform;
            g.DOKill(); t.DOKill();

            DOTween.Sequence().SetUpdate(true).SetLink(_panel)
                .Append(g.DOFade(0f, closeDuration).SetEase(Ease.InQuad))
                .Join(t.DOScale(openFromScale, closeDuration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    // 閉じ終わってから非アクティブにする。先に消すとTweenが走らない
                    _panel.SetActive(false);
                    t.localScale = Vector3.one;
                    g.alpha = 1f;
                });
        }

        private CanvasGroup GetGroup()
        {
            if (_group == null)
            {
                _group = _panel.GetComponent<CanvasGroup>();
                if (_group == null) _group = _panel.AddComponent<CanvasGroup>();
            }
            return _group;
        }

        public bool IsOpen => _panel != null && _panel.activeSelf;
    }
}
