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
"■ 開発\n" +
"CyberAgentHack チームD\n" +
"松崎正弘 / 川瀬莉平 / 伊藤俊生\n" +
"\n" +
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
"卵の中身 / st happens\n" +
"  sketchfab.com/3d-models/st-happens\n" +
"Animated Easy Enemies / Quaternius\n" +
"  quaternius.itch.io/animated-easy-enemies\n" +
"Kitchen / styloo\n" +
"  styloo.itch.io/kitchen\n" +
"\n" +
"■ フォント\n" +
"M PLUS Rounded 1c\n" +
"  SIL Open Font License 1.1\n";

        private void Awake()
        {
            if (_panel == null) _panel = gameObject;
            if (_body != null) _body.text = credits;
            _panel.SetActive(false);
        }

        /// <summary>タイトルのCREDITSボタンから呼ぶ</summary>
        public void Open()
        {
            if (_body != null) _body.text = credits;
            _panel.SetActive(true);
            // 開くたびに先頭から読ませる
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>閉じるボタンから呼ぶ</summary>
        public void Close()
        {
            _panel.SetActive(false);
        }

        public bool IsOpen => _panel != null && _panel.activeSelf;
    }
}
