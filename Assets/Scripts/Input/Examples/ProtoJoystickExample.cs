using UnityEngine;
using Proto.Core;
using ProtoInput.Core;

namespace Proto.Input.Examples
{
    /// <summary>
    /// ジョイスティックの使用例を示すクラス
    /// </summary>
    public class ProtoJoystickExample : MonoBehaviour
    {
        [Header("Joystick References")]
        [SerializeField] private ProtoVirtualJoystick virtualJoystick;
        [SerializeField] private ProtoInputManager inputManager;
        
        [Header("Example Settings")]
        [SerializeField] private bool enableJoystickExample = true;
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private Transform targetObject;
        
        private Vector3 initialPosition;
        private bool isJoystickActive;
        
        private void Start()
        {
            InitializeJoystickExample();
        }
        
        private void Update()
        {
            if (!enableJoystickExample) return;
            
            HandleJoystickInput();
        }
        
        /// <summary>
        /// ジョイスティックの例を初期化
        /// </summary>
        private void InitializeJoystickExample()
        {
            // ジョイスティックの参照を取得
            if (virtualJoystick == null)
            {
                virtualJoystick = FindObjectOfType<ProtoVirtualJoystick>();
                if (virtualJoystick == null)
                {
                    ProtoLogger.LogWarning("ProtoVirtualJoystick not found in scene", this);
                    return;
                }
            }
            
            // 入力マネージャーの参照を取得
            if (inputManager == null)
            {
                inputManager = FindObjectOfType<ProtoInputManager>();
                if (inputManager == null)
                {
                    ProtoLogger.LogWarning("ProtoInputManager not found in scene", this);
                    return;
                }
            }
            
            // ターゲットオブジェクトの初期位置を保存
            if (targetObject == null)
            {
                targetObject = transform;
            }
            initialPosition = targetObject.position;
            
            // イベントリスナーを登録
            RegisterEventListeners();
            
            ProtoLogger.LogInfo("Joystick example initialized successfully", this);
        }
        
        /// <summary>
        /// イベントリスナーを登録
        /// </summary>
        private void RegisterEventListeners()
        {
            if (inputManager != null)
            {
                // 入力開始イベント
                inputManager.InputStarted += OnInputStarted;
                
                // 入力終了イベント
                inputManager.InputEnded += OnInputEnded;
                
                // 移動入力変更イベント
                inputManager.MovementInputChanged += OnMovementInputChanged;
                
                // ジョイスティック入力変更イベント（UnityEvent）
                inputManager.OnJoystickInputChanged.AddListener(OnJoystickInputChanged);
            }
            
            if (virtualJoystick != null)
            {
                // ジョイスティックの状態変更を監視
                StartCoroutine(MonitorJoystickState());
            }
        }
        
        /// <summary>
        /// イベントリスナーを解除
        /// </summary>
        private void UnregisterEventListeners()
        {
            if (inputManager != null)
            {
                inputManager.InputStarted -= OnInputStarted;
                inputManager.InputEnded -= OnInputEnded;
                inputManager.MovementInputChanged -= OnMovementInputChanged;
                inputManager.OnJoystickInputChanged.RemoveListener(OnJoystickInputChanged);
            }
        }
        
        /// <summary>
        /// ジョイスティックの状態を監視
        /// </summary>
        private System.Collections.IEnumerator MonitorJoystickState()
        {
            bool lastPressedState = false;
            
            while (true)
            {
                if (virtualJoystick != null)
                {
                    bool currentPressedState = virtualJoystick.IsPressed;
                    
                    if (currentPressedState != lastPressedState)
                    {
                        if (currentPressedState)
                        {
                            OnJoystickPressed();
                        }
                        else
                        {
                            OnJoystickReleased();
                        }
                        
                        lastPressedState = currentPressedState;
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// ジョイスティック入力の処理
        /// </summary>
        private void HandleJoystickInput()
        {
            if (virtualJoystick == null || targetObject == null) return;
            
            // ジョイスティックの値を取得
            Vector2 joystickValue = virtualJoystick.GetJoystickValue();
            
            // 入力が有効な場合のみ移動
            if (joystickValue.magnitude > 0.1f)
            {
                // 移動方向を計算
                Vector3 movement = new Vector3(joystickValue.x, 0, joystickValue.y);
                
                // ターゲットオブジェクトを移動
                targetObject.Translate(movement * movementSpeed * Time.deltaTime);
                
                // ログ出力（頻度を制限）
                if (Time.frameCount % 30 == 0)
                {
                    ProtoLogger.LogInput($"Joystick movement: {joystickValue}, Position: {targetObject.position}", this);
                }
            }
        }
        
        /// <summary>
        /// 入力開始時の処理
        /// </summary>
        private void OnInputStarted()
        {
            ProtoLogger.LogInput("Input started", this);
        }
        
        /// <summary>
        /// 入力終了時の処理
        /// </summary>
        private void OnInputEnded()
        {
            ProtoLogger.LogInput("Input ended", this);
        }
        
        /// <summary>
        /// 移動入力変更時の処理
        /// </summary>
        private void OnMovementInputChanged(Vector2 movementInput)
        {
            ProtoLogger.LogInput($"Movement input changed: {movementInput}", this);
        }
        
        /// <summary>
        /// ジョイスティック入力変更時の処理
        /// </summary>
        private void OnJoystickInputChanged(Vector2 joystickInput)
        {
            ProtoLogger.LogInput($"Joystick input changed: {joystickInput}", this);
        }
        
        /// <summary>
        /// ジョイスティックが押された時の処理
        /// </summary>
        private void OnJoystickPressed()
        {
            isJoystickActive = true;
            ProtoLogger.LogUI("Joystick pressed", this);
        }
        
        /// <summary>
        /// ジョイスティックが離された時の処理
        /// </summary>
        private void OnJoystickReleased()
        {
            isJoystickActive = false;
            ProtoLogger.LogUI("Joystick released", this);
        }
        
        /// <summary>
        /// ターゲットオブジェクトを初期位置にリセット
        /// </summary>
        [ContextMenu("Reset Target Position")]
        public void ResetTargetPosition()
        {
            if (targetObject != null)
            {
                targetObject.position = initialPosition;
                ProtoLogger.LogInfo($"Target object reset to initial position: {initialPosition}", this);
            }
        }
        
        /// <summary>
        /// ジョイスティックの有効/無効を切り替え
        /// </summary>
        [ContextMenu("Toggle Joystick")]
        public void ToggleJoystick()
        {
            if (virtualJoystick != null)
            {
                bool currentState = virtualJoystick.enabled;
                virtualJoystick.SetJoystickEnabled(!currentState);
                ProtoLogger.LogUI($"Joystick {(currentState ? "disabled" : "enabled")}", this);
            }
        }
        
        /// <summary>
        /// 移動速度を変更
        /// </summary>
        [ContextMenu("Increase Movement Speed")]
        public void IncreaseMovementSpeed()
        {
            movementSpeed += 1f;
            ProtoLogger.LogInfo($"Movement speed increased to: {movementSpeed}", this);
        }
        
        /// <summary>
        /// 移動速度を変更
        /// </summary>
        [ContextMenu("Decrease Movement Speed")]
        public void DecreaseMovementSpeed()
        {
            movementSpeed = Mathf.Max(0.1f, movementSpeed - 1f);
            ProtoLogger.LogInfo($"Movement speed decreased to: {movementSpeed}", this);
        }
        
        /// <summary>
        /// 現在の状態をログ出力
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (virtualJoystick != null)
            {
                Vector2 joystickValue = virtualJoystick.GetJoystickValue();
                bool isPressed = virtualJoystick.IsPressed;
                
                ProtoLogger.LogInfo($"Joystick State - Value: {joystickValue}, Pressed: {isPressed}, Active: {isJoystickActive}", this);
            }
            
            if (targetObject != null)
            {
                ProtoLogger.LogInfo($"Target Position: {targetObject.position}", this);
            }
        }
        
        private void OnDestroy()
        {
            UnregisterEventListeners();
        }
    }
} 