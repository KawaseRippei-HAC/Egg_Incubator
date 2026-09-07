using Proto.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    /// <summary>
    /// 難易度ボタンの見た目を管理する。
    ///   ・今どれが選ばれているかを色で示す
    ///     (3つ並んだボタンのどれが有効か分からないと選択UIとして成立しないため)
    ///   ・クリア済みの難易度に CLEAR! を出す
    ///   ・EXTRAは Easy/Normal/Hard を全てクリアするまで隠す
    /// </summary>
    public class DifficultyButtonsView : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public GameDifficulty difficulty;
            [Tooltip("ボタンの背景Image")]
            public Image background;
            [Tooltip("クリア済みのときだけ出すバッジ。未設定でも可")]
            public GameObject clearBadge;
        }

        [SerializeField] private DifficultySelector _selector;

        [Header("ボタン")]
        [SerializeField] private Entry[] _entries;

        [Header("色")]
        [SerializeField] private Color _selectedColor = new Color(1f, 0.78f, 0.25f, 0.95f);
        [SerializeField] private Color _unselectedColor = new Color(0.10f, 0.12f, 0.16f, 0.8f);

        [Header("文字色")]
        [SerializeField] private Color _selectedTextColor = new Color(0.15f, 0.12f, 0.05f, 1f);
        [SerializeField] private Color _unselectedTextColor = Color.white;

        private void Awake()
        {
            if (_selector == null) _selector = FindFirstObjectByType<DifficultySelector>();
        }

        private void OnEnable()
        {
            if (_selector == null) return;

            _selector.OnDifficultyChanged += OnChanged;
            _selector.OnClearedChanged += Refresh;
            // タイトルへ戻るたびにここが呼ばれるので、クリア直後の解放もここで反映される
            Refresh();
        }

        private void OnDisable()
        {
            if (_selector == null) return;
            _selector.OnDifficultyChanged -= OnChanged;
            _selector.OnClearedChanged -= Refresh;
        }

        private void OnChanged(GameDifficulty _) { Refresh(); }

        /// <summary>外から表示を合わせ直したい場合に使う</summary>
        public void Refresh()
        {
            if (_selector == null || _entries == null) return;

            foreach (var e in _entries)
            {
                if (e == null) continue;

                bool unlocked = _selector.IsUnlocked(e.difficulty);
                bool selected = unlocked && _selector.Current == e.difficulty;
                bool cleared = _selector.IsCleared(e.difficulty);

                if (e.background != null)
                {
                    // 未解放のボタンはそもそも表示しない(EXTRAの存在を隠す)
                    e.background.gameObject.SetActive(unlocked);

                    e.background.color = selected ? _selectedColor : _unselectedColor;

                    var label = e.background.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                    if (label != null)
                    {
                        label.color = selected ? _selectedTextColor : _unselectedTextColor;
                    }
                }

                if (e.clearBadge != null)
                {
                    e.clearBadge.SetActive(unlocked && cleared);
                }
            }
        }
    }
}
