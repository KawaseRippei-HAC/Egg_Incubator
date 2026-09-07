using Proto.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    /// <summary>
    /// 難易度ボタンの「今どれが選ばれているか」を色で示す。
    /// これが無いと3つ並んだボタンのどれが有効なのか分からず、選択UIとして成立しない。
    /// </summary>
    public class DifficultyButtonsView : MonoBehaviour
    {
        [SerializeField] private DifficultySelector _selector;

        [Header("ボタンの背景Image (Easy / Normal / Hard の順)")]
        [SerializeField] private Image _easyImage;
        [SerializeField] private Image _normalImage;
        [SerializeField] private Image _hardImage;

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
            if (_selector != null)
            {
                _selector.OnDifficultyChanged += Refresh;
                Refresh(_selector.Current);
            }
        }

        private void OnDisable()
        {
            if (_selector != null) _selector.OnDifficultyChanged -= Refresh;
        }

        /// <summary>タイトルへ戻ったときなど、外から表示を合わせ直したい場合に使う</summary>
        public void RefreshCurrent()
        {
            if (_selector != null) Refresh(_selector.Current);
        }

        private void Refresh(GameDifficulty current)
        {
            Apply(_easyImage, current == GameDifficulty.Easy);
            Apply(_normalImage, current == GameDifficulty.Normal);
            Apply(_hardImage, current == GameDifficulty.Hard);
        }

        private void Apply(Image target, bool selected)
        {
            if (target == null) return;
            target.color = selected ? _selectedColor : _unselectedColor;

            var label = target.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                label.color = selected ? _selectedTextColor : _unselectedTextColor;
            }
        }
    }
}
