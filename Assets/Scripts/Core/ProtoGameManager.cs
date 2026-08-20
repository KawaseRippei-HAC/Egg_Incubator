using Proto.Input;
using Proto.UI;
using ProtoInput.Core;
using UnityEngine;

namespace Proto.Core
{
    /// <summary>
    ///     ゲーム全体を管理するマネージャークラス
    ///     各システムの統合例として使用
    /// </summary>
    public class ProtoGameManager : MonoBehaviour
    {
        [Header("System References")] [SerializeField]
        private ProtoInputManager inputManager;

        [SerializeField] private ProtoSafeAreaHandler safeAreaHandler;
        [SerializeField] private ProtoVirtualJoystick virtualJoystick;
        [SerializeField] private ProtoDebugUI debugUI;

        [Header("Game Settings")] [SerializeField]
        private bool enableDebugMode = true;

        [SerializeField] private bool enableSafeArea = true;
        [SerializeField] private bool enableJoystick = true;

        private void Awake()
        {
            InitializeSystems();
        }

        private void Start()
        {
            SetupEventListeners();
            ApplySettings();
        }

        private void OnDestroy()
        {
            // イベントリスナーの解除
            if (inputManager != null)
            {
                inputManager.MovementInputChanged -= OnMovementInputChanged;
                inputManager.InputStarted -= OnInputStarted;
                inputManager.InputEnded -= OnInputEnded;
            }

            if (virtualJoystick != null)
            {
                virtualJoystick.JoystickValueChanged -= OnJoystickValueChanged;
                virtualJoystick.JoystickPressed -= OnJoystickPressed;
                virtualJoystick.JoystickReleased -= OnJoystickReleased;
            }
        }

        /// <summary>
        ///     システムの初期化
        /// </summary>
        private void InitializeSystems()
        {
            // 必要なコンポーネントを自動で探す
            if (inputManager == null)
                inputManager = FindObjectOfType<ProtoInputManager>();
            if (safeAreaHandler == null)
                safeAreaHandler = FindObjectOfType<ProtoSafeAreaHandler>();
            if (virtualJoystick == null)
                virtualJoystick = FindObjectOfType<ProtoVirtualJoystick>();
            if (debugUI == null)
                debugUI = FindObjectOfType<ProtoDebugUI>();

            // 警告ログ
            if (inputManager == null)
                ProtoLogger.LogWarning("InputManager not found!", this);
            if (safeAreaHandler == null)
                ProtoLogger.LogWarning("SafeAreaHandler not found!", this);
            if (virtualJoystick == null)
                ProtoLogger.LogWarning("VirtualJoystick not found!", this);
            if (debugUI == null)
                ProtoLogger.LogWarning("DebugUI not found!", this);
        }

        /// <summary>
        ///     イベントリスナーの設定
        /// </summary>
        private void SetupEventListeners()
        {
            if (inputManager != null)
            {
                inputManager.MovementInputChanged += OnMovementInputChanged;
                inputManager.InputStarted += OnInputStarted;
                inputManager.InputEnded += OnInputEnded;
            }

            if (virtualJoystick != null)
            {
                virtualJoystick.JoystickValueChanged += OnJoystickValueChanged;
                virtualJoystick.JoystickPressed += OnJoystickPressed;
                virtualJoystick.JoystickReleased += OnJoystickReleased;
            }
        }

        /// <summary>
        ///     設定の適用
        /// </summary>
        private void ApplySettings()
        {
            // デバッグモードの設定
            if (debugUI != null) debugUI.gameObject.SetActive(enableDebugMode);

            // 安全領域の設定
            if (safeAreaHandler != null)
            {
                // SafeAreaHandlerの設定はインスペクターで行う
            }

            // ジョイスティックの設定
            if (virtualJoystick != null) virtualJoystick.SetJoystickEnabled(enableJoystick);
        }

        #region Event Handlers

        /// <summary>
        ///     移動入力が変更された時の処理
        /// </summary>
        private void OnMovementInputChanged(Vector2 input)
        {
            // ここでプレイヤーの移動処理を行う
            ProtoLogger.LogInput($"Movement Input: {input}", this);

            // 例: プレイヤーキャラクターの移動
            // playerController.Move(input);
        }

        /// <summary>
        ///     入力が開始された時の処理
        /// </summary>
        private void OnInputStarted()
        {
            ProtoLogger.LogInput("Input Started", this);

            // 例: プレイヤーのアニメーション開始
            // playerController.StartMovementAnimation();
        }

        /// <summary>
        ///     入力が終了した時の処理
        /// </summary>
        private void OnInputEnded()
        {
            ProtoLogger.LogInput("Input Ended", this);

            // 例: プレイヤーのアニメーション停止
            // playerController.StopMovementAnimation();
        }

        /// <summary>
        ///     ジョイスティックの値が変更された時の処理
        /// </summary>
        private void OnJoystickValueChanged(Vector2 value)
        {
            ProtoLogger.LogInput($"Joystick Value: {value}", this);
        }

        /// <summary>
        ///     ジョイスティックが押された時の処理
        /// </summary>
        private void OnJoystickPressed()
        {
            ProtoLogger.LogInput("Joystick Pressed", this);
        }

        /// <summary>
        ///     ジョイスティックが離された時の処理
        /// </summary>
        private void OnJoystickReleased()
        {
            ProtoLogger.LogInput("Joystick Released", this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     デバッグモードの切り替え
        /// </summary>
        public void ToggleDebugMode()
        {
            enableDebugMode = !enableDebugMode;
            if (debugUI != null) debugUI.gameObject.SetActive(enableDebugMode);
        }

        /// <summary>
        ///     安全領域の切り替え
        /// </summary>
        public void ToggleSafeArea()
        {
            enableSafeArea = !enableSafeArea;
            if (safeAreaHandler != null) safeAreaHandler.ToggleSafeArea();
        }

        /// <summary>
        ///     ジョイスティックの切り替え
        /// </summary>
        public void ToggleJoystick()
        {
            enableJoystick = !enableJoystick;
            if (virtualJoystick != null) virtualJoystick.SetJoystickEnabled(enableJoystick);
        }

        /// <summary>
        ///     すべてのシステムをリセット
        /// </summary>
        public void ResetAllSystems()
        {
            if (inputManager != null)
            {
                inputManager.SetInputEnabled(false);
                inputManager.SetInputEnabled(true);
            }

            if (virtualJoystick != null) virtualJoystick.ResetJoystick();

            ProtoLogger.LogInfo("All systems reset", this);
        }

        #endregion
    }
}