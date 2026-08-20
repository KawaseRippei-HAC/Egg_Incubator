using System;
using Proto.Core;
using Proto.Input;
using Proto.Input.Gestures;
using UnityEngine;
using UnityEngine.Events;

namespace ProtoInput.Core
{
    /// <summary>
    ///     入力管理システム
    ///     キーボード、マウス、タッチ、ジョイスティックの入力を統合管理
    /// </summary>
    public class ProtoInputManager : MonoBehaviour
    {
        [Header("Input Settings")] [SerializeField]
        private ProtoInputSettings inputSettings;
        
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private bool enableMouseInput = true;
        [SerializeField] private bool enableTouchInput = true;
        [SerializeField] private bool enableJoystickInput = true;
        
        [Header("Gesture Detection")] [SerializeField]
        private ProtoGestureSettings gestureSettings;

        [SerializeField] private bool enableGestureDetection = true;

        [Header("Events")] public UnityEvent<Vector2> OnInputChanged;
        public UnityEvent<Vector2> OnJoystickInputChanged;
        public UnityEvent<ProtoGestureData> OnGestureDetected;

        // 現在の入力状態
        private Vector2 currentMovementInput = Vector2.zero;

        // コンポーネント
        private ProtoGestureDetector gestureDetector;

        // 内部イベント
        private Action<Vector2> onInputChanged;
        private Action<Vector2> onJoystickInput;
        
        // プロパティ
        public Vector2 CurrentMovementInput => currentMovementInput;
        public Vector2 CurrentJoystickInput { get; private set; } = Vector2.zero;

        public bool IsInputActive { get; private set; }

        public ProtoInputSettings InputSettings => inputSettings;
        public ProtoGestureSettings GestureSettings => gestureSettings;
        
        private void Awake()
        {
            InitializeInputManager();
        }

        private void Start()
        {
            InitializeGestureDetection();
        }
        
        private void Update()
        {
            UpdateInput();
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            onInputChanged = null;
            onJoystickInput = null;

            if (gestureDetector != null) gestureDetector.OnGestureDetected -= OnGestureDetectedInternal;
        }
        
        /// <summary>
        ///     入力マネージャーを初期化
        /// </summary>
        private void InitializeInputManager()
        {
            // 設定ファイルの検証
            if (inputSettings == null)
            {
                ProtoLogger.LogError("ProtoInputSettings is not assigned! Please assign a ProtoInputSettings asset.",
                    this);
                return;
            }
            
            if (gestureSettings == null)
            {
                ProtoLogger.LogError(
                    "ProtoGestureSettings is not assigned! Please assign a ProtoGestureSettings asset.", this);
                return;
            }

            ProtoLogger.LogInput("InputManager initialized successfully", this);
        }

        /// <summary>
        ///     ジェスチャー検出を初期化
        /// </summary>
        private void InitializeGestureDetection()
        {
            if (!enableGestureDetection) return;

            gestureDetector = new ProtoGestureDetector(gestureSettings);

            // すべてのジェスチャータイプに対してコールバックを登録
            gestureDetector.OnGestureDetected += OnGestureDetectedInternal;

            ProtoLogger.LogGesture("Gesture detection initialized", this);
        }

        /// <summary>
        ///     内部ジェスチャー検出コールバック
        /// </summary>
        private void OnGestureDetectedInternal(ProtoGestureData gestureData)
        {
            OnGestureDetected?.Invoke(gestureData);
            ProtoLogger.LogGesture($"Gesture detected: {gestureData.type}", this);
        }

        /// <summary>
        ///     入力を更新
        /// </summary>
        private void UpdateInput()
        {
            var combinedInput = Vector2.zero;

            // キーボード入力
            if (enableKeyboardInput) combinedInput += GetKeyboardInput();

            // マウス入力
            if (enableMouseInput) combinedInput += GetMouseInput();

            // タッチ入力
            if (enableTouchInput) combinedInput += GetTouchInput();

            // ジョイスティック入力
            if (enableJoystickInput) combinedInput += CurrentJoystickInput;

            // 入力の正規化
            if (combinedInput.magnitude > ProtoConstants.Input.NormalizedMaxValue)
                combinedInput = combinedInput.normalized;

            // 入力の閾値チェック
            var wasInputActive = IsInputActive;
            IsInputActive = combinedInput.magnitude > ProtoConstants.Input.InputThreshold;

            // 入力開始/終了イベントの発火
            if (IsInputActive && !wasInputActive)
                InputStarted?.Invoke();
            else if (!IsInputActive && wasInputActive) InputEnded?.Invoke();

            // 入力が変更された場合のみ更新
            if (currentMovementInput != combinedInput)
            {
                currentMovementInput = combinedInput;
                OnInputChanged?.Invoke(currentMovementInput);
                onInputChanged?.Invoke(currentMovementInput);
                MovementInputChanged?.Invoke(currentMovementInput);
            }

            // ジェスチャー検出の更新
            if (enableGestureDetection && gestureDetector != null) gestureDetector.Update();
        }

        /// <summary>
        ///     キーボード入力を取得
        /// </summary>
        private Vector2 GetKeyboardInput()
        {
            var input = Vector2.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                input.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input.x += 1f;

            return input;
        }

        /// <summary>
        ///     マウス入力を取得
        /// </summary>
        private Vector2 GetMouseInput()
        {
            var input = Vector2.zero;

            if (inputSettings != null)
            {
                var sensitivity = inputSettings.MouseSensitivity;
                input.x = Input.GetAxis("Mouse X") * sensitivity;
                input.y = Input.GetAxis("Mouse Y") * sensitivity;
            }

            return input;
        }

        /// <summary>
        ///     タッチ入力を取得
        /// </summary>
        private Vector2 GetTouchInput()
        {
            var input = Vector2.zero;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                input = touch.deltaPosition;

                // ジェスチャー検出にタッチデータを送信
                if (enableGestureDetection && gestureDetector != null) gestureDetector.ProcessTouch(touch);
            }

            return input;
        }

        /// <summary>
        ///     ジョイスティック入力を設定
        /// </summary>
        public void SetJoystickInput(Vector2 input)
        {
            if (CurrentJoystickInput != input)
            {
                CurrentJoystickInput = input;
                OnJoystickInputChanged?.Invoke(CurrentJoystickInput);
                onJoystickInput?.Invoke(CurrentJoystickInput);

                if (input.magnitude > 0.01f) ProtoLogger.LogInput($"Joystick input received: {input}", this);
            }
        }

        /// <summary>
        ///     入力変更イベントを登録
        /// </summary>
        public void RegisterInputChangedCallback(Action<Vector2> callback)
        {
            onInputChanged += callback;
        }

        /// <summary>
        ///     入力変更イベントを解除
        /// </summary>
        public void UnregisterInputChangedCallback(Action<Vector2> callback)
        {
            onInputChanged -= callback;
        }

        /// <summary>
        ///     ジョイスティック入力イベントを登録
        /// </summary>
        public void RegisterJoystickInputCallback(Action<Vector2> callback)
        {
            onJoystickInput += callback;
        }

        /// <summary>
        ///     ジョイスティック入力イベントを解除
        /// </summary>
        public void UnregisterJoystickInputCallback(Action<Vector2> callback)
        {
            onJoystickInput -= callback;
        }

        /// <summary>
        ///     入力方向を取得
        /// </summary>
        public Vector2 GetInputDirection()
        {
            return currentMovementInput.normalized;
        }

        /// <summary>
        ///     入力の大きさを取得
        /// </summary>
        public float GetInputMagnitude()
        {
            return currentMovementInput.magnitude;
        }

        /// <summary>
        ///     入力の有効/無効を設定
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;
            enableMouseInput = enabled;
            enableTouchInput = enabled;
            enableJoystickInput = enabled;

            if (!enabled)
            {
                currentMovementInput = Vector2.zero;
                CurrentJoystickInput = Vector2.zero;
                IsInputActive = false;
            }
        }

        /// <summary>
        ///     キーボード入力の有効/無効を設定
        /// </summary>
        public void SetKeyboardInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;
        }

        /// <summary>
        ///     マウス入力の有効/無効を設定
        /// </summary>
        public void SetMouseInputEnabled(bool enabled)
        {
            enableMouseInput = enabled;
        }

        /// <summary>
        ///     タッチ入力の有効/無効を設定
        /// </summary>
        public void SetTouchInputEnabled(bool enabled)
        {
            enableTouchInput = enabled;
        }

        /// <summary>
        ///     ジョイスティック入力の有効/無効を設定
        /// </summary>
        public void SetJoystickInputEnabled(bool enabled)
        {
            enableJoystickInput = enabled;
            if (!enabled) CurrentJoystickInput = Vector2.zero;
        }

        /// <summary>
        ///     ジェスチャー検出の有効/無効を設定
        /// </summary>
        public void SetGestureDetectionEnabled(bool enabled)
        {
            enableGestureDetection = enabled;
        }

        /// <summary>
        ///     入力設定を更新
        /// </summary>
        public void UpdateInputSettings(ProtoInputSettings newSettings)
        {
            if (newSettings != null)
            {
                inputSettings = newSettings;
                ProtoLogger.LogInput("Input settings updated", this);
            }
        }

        /// <summary>
        ///     ジェスチャー設定を更新
        /// </summary>
        public void UpdateGestureSettings(ProtoGestureSettings newSettings)
        {
            if (newSettings != null)
            {
                gestureSettings = newSettings;
                if (gestureDetector != null) gestureDetector.UpdateSettings(newSettings);

                ProtoLogger.LogGesture("Gesture settings updated", this);
            }
        }

        /// <summary>
        ///     ジェスチャーコールバックを登録
        /// </summary>
        public void RegisterGestureCallback(ProtoGestureType gestureType, Action<ProtoGestureData> callback)
        {
            if (gestureDetector != null && callback != null) gestureDetector.RegisterCallback(gestureType, callback);
        }

        /// <summary>
        ///     ジェスチャーコールバックを解除
        /// </summary>
        public void UnregisterGestureCallback(ProtoGestureType gestureType, Action<ProtoGestureData> callback)
        {
            if (gestureDetector != null) gestureDetector.UnregisterCallback(gestureType);
        }

        /// <summary>
        ///     入力開始イベント
        /// </summary>
        public event Action InputStarted;

        /// <summary>
        ///     入力終了イベント
        /// </summary>
        public event Action InputEnded;

        /// <summary>
        ///     移動入力変更イベント
        /// </summary>
        public event Action<Vector2> MovementInputChanged;
    }
} 