using Proto.Core;
using UnityEngine;

namespace Proto.UI
{
    /// <summary>
    ///     セーフエリアを管理するクラス
    /// </summary>
    public class ProtoSafeAreaHandler : MonoBehaviour
    {
        [Header("Safe Area Settings")] [SerializeField]
        private bool enableSafeArea = true;

        [Header("Mock Safe Area (for testing)")] [SerializeField]
        private bool useMockSafeArea;

        [SerializeField] private float mockSafeAreaTop = ProtoConstants.SafeArea.DefaultMockTop;
        [SerializeField] private float mockSafeAreaBottom = ProtoConstants.SafeArea.DefaultMockBottom;
        [SerializeField] private float mockSafeAreaLeft = ProtoConstants.SafeArea.DefaultMockLeft;
        [SerializeField] private float mockSafeAreaRight = ProtoConstants.SafeArea.DefaultMockRight;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (!enableSafeArea) return;

            // セーフエリアまたは画面サイズが変更された場合に再適用
            if (HasSafeAreaChanged() || HasScreenSizeChanged()) ApplySafeArea();
        }

        /// <summary>
        ///     セーフエリアを適用
        /// </summary>
        private void ApplySafeArea()
        {
            var safeArea = GetSafeArea();
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            var safeAreaRect = GetComponent<RectTransform>();
            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        /// <summary>
        ///     セーフエリアを取得
        /// </summary>
        private Rect GetSafeArea()
        {
            if (useMockSafeArea) return GetMockSafeArea();

            return Screen.safeArea;
        }

        /// <summary>
        ///     モックセーフエリアを取得（テスト用）
        /// </summary>
        private Rect GetMockSafeArea()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            var top = screenHeight * mockSafeAreaTop;
            var bottom = screenHeight * mockSafeAreaBottom;
            var left = screenWidth * mockSafeAreaLeft;
            var right = screenWidth * mockSafeAreaRight;

            return new Rect(left, bottom, screenWidth - left - right, screenHeight - top - bottom);
        }

        /// <summary>
        ///     セーフエリアが変更されたかチェック
        /// </summary>
        private bool HasSafeAreaChanged()
        {
            return Screen.safeArea != lastSafeArea;
        }

        /// <summary>
        ///     画面サイズが変更されたかチェック
        /// </summary>
        private bool HasScreenSizeChanged()
        {
            return Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y;
        }

        /// <summary>
        ///     セーフエリアの有効/無効を設定
        /// </summary>
        public void SetSafeAreaEnabled(bool enabled)
        {
            enableSafeArea = enabled;
            if (enabled) ApplySafeArea();
            ProtoLogger.LogUI($"Safe area {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        ///     モックセーフエリアの有効/無効を設定
        /// </summary>
        public void SetMockSafeAreaEnabled(bool enabled)
        {
            useMockSafeArea = enabled;
            ApplySafeArea();
            ProtoLogger.LogUI($"Mock safe area {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        ///     セーフエリアの有効/無効を切り替え
        /// </summary>
        public void ToggleSafeArea()
        {
            enableSafeArea = !enableSafeArea;
            if (enableSafeArea) ApplySafeArea();
            ProtoLogger.LogUI($"Safe area {(enableSafeArea ? "enabled" : "disabled")}", this);
        }
    }
}