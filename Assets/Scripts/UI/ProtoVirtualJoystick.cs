using System;
using Proto.Core;
using ProtoInput.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Proto.Input
{
    /// <summary>
    ///     バーチャルジョイスティック
    ///     タッチ操作でジョイスティック入力を提供
    /// </summary>
    public class ProtoVirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick Components")] [SerializeField]
        public RectTransform joystickBase;

        [SerializeField] public RectTransform joystickHandle;

        [Header("Joystick Settings")] [SerializeField]
        public float maxDistance = ProtoConstants.UI.DefaultJoystickMaxDistance;

        [SerializeField] public float deadZone = ProtoConstants.UI.DefaultJoystickDeadZone;
        [SerializeField] public bool snapToCenter = true;
        [SerializeField] public bool enableVisualFeedback = true;

        [Header("Input Manager")] [SerializeField]
        private ProtoInputManager inputManager;

        [Header("Events")] [SerializeField] private UnityEvent<Vector2> onJoystickValueChanged;
        [SerializeField] private UnityEvent onJoystickPressed;
        [SerializeField] private UnityEvent onJoystickReleased;
        private int currentPointerId = -1;

        // 内部状態
        private Vector2 joystickValue;

        // プロパティ
        public Vector2 JoystickValue => joystickValue;
        public bool IsPressed { get; private set; }

        public Vector2 JoystickCenter { get; private set; }

        private void Start()
        {
            // ジョイスティックの初期化
            InitializeJoystick();

            // InputManagerが設定されていない場合、自動で探す
            if (inputManager == null)
            {
                inputManager = FindObjectOfType<ProtoInputManager>();
                if (inputManager == null)
                    ProtoLogger.LogWarning("InputManager not found! Joystick input will not be sent to InputManager.",
                        this);
                else
                    ProtoLogger.LogUI("InputManager found and connected to joystick", this);
            }
        }

        /// <summary>
        ///     ドラッグ時の処理
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            ProtoLogger.LogUI(
                $"OnDrag called - isPressed: {IsPressed}, currentPointerId: {currentPointerId}, eventPointerId: {eventData.pointerId}",
                this);

            if (!IsPressed || eventData.pointerId != currentPointerId)
            {
                ProtoLogger.LogUI(
                    $"Joystick drag ignored: isPressed={IsPressed}, currentPointerId={currentPointerId}, eventPointerId={eventData.pointerId}",
                    this);
                return;
            }

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBase, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // ジョイスティックの値を計算（中心からの相対位置）
                var direction = localPoint - JoystickCenter;
                var distance = direction.magnitude;

                ProtoLogger.LogUI(
                    $"OnDrag debug - localPoint: {localPoint}, direction: {direction}, distance: {distance}, maxDistance: {maxDistance}, deadZone: {deadZone}",
                    this);

                // 最大距離で制限
                if (distance > maxDistance)
                {
                    direction = direction.normalized * maxDistance;
                    distance = maxDistance;
                    ProtoLogger.LogUI($"Distance limited to maxDistance: {distance}", this);
                }

                // デッドゾーンの処理（正規化された距離で比較）
                var normalizedDistance = distance / maxDistance;
                ProtoLogger.LogUI($"normalizedDistance: {normalizedDistance}, deadZone: {deadZone}", this);

                if (normalizedDistance < deadZone)
                {
                    joystickValue = Vector2.zero;
                    // デッドゾーン内では常にハンドルを中心に戻す
                    ResetJoystickHandle();
                    ProtoLogger.LogUI("Joystick in dead zone - value set to zero", this);
                }
                else
                {
                    // 正規化された値を計算
                    joystickValue = direction / maxDistance;

                    // ハンドルの位置を更新
                    if (enableVisualFeedback) joystickHandle.anchoredPosition = JoystickCenter + direction;

                    ProtoLogger.LogUI($"Joystick value calculated: {joystickValue}", this);
                }

                // イベント発火
                JoystickValueChanged?.Invoke(joystickValue);
                onJoystickValueChanged?.Invoke(joystickValue);

                // InputManagerに値を送信
                if (inputManager != null)
                {
                    inputManager.SetJoystickInput(joystickValue);
                    ProtoLogger.LogUI($"Joystick value sent to InputManager: {joystickValue}", this);
                }
                else
                {
                    ProtoLogger.LogWarning("InputManager is null, cannot send joystick value", this);
                }
            }
            else
            {
                ProtoLogger.LogUI("Failed to convert screen point to local point", this);
            }
        }

        /// <summary>
        ///     ポインターダウン時の処理
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            ProtoLogger.LogUI(
                $"OnPointerDown called - currentPointerId: {currentPointerId}, eventPointerId: {eventData.pointerId}",
                this);

            if (currentPointerId != -1)
            {
                ProtoLogger.LogUI($"OnPointerDown ignored - already active pointer: {currentPointerId}", this);
                return; // 既に別のポインターがアクティブ
            }

            currentPointerId = eventData.pointerId;
            IsPressed = true;

            // ジョイスティックの中心位置は固定（変更しない）
            // タッチ位置に関係なく、ベースの中心を基準とする

            ProtoLogger.LogUI(
                $"Joystick pointer down: {eventData.position}, pointerId: {eventData.pointerId}, isPressed: {IsPressed}",
                this);

            JoystickPressed?.Invoke();
            onJoystickPressed?.Invoke();
        }

        /// <summary>
        ///     ポインターアップ時の処理
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != currentPointerId) return;

            currentPointerId = -1;
            IsPressed = false;

            // ジョイスティックをリセット
            joystickValue = Vector2.zero;

            // 常にハンドルを中心に戻す
            ResetJoystickHandle();

            // イベント発火
            JoystickReleased?.Invoke();
            onJoystickReleased?.Invoke();

            // InputManagerに値を送信
            if (inputManager != null) inputManager.SetJoystickInput(Vector2.zero);
        }

        // イベント
        public event Action<Vector2> JoystickValueChanged;
        public event Action JoystickPressed;
        public event Action JoystickReleased;

        /// <summary>
        ///     ジョイスティックの初期化
        /// </summary>
        public void InitializeJoystick()
        {
            if (joystickBase == null)
            {
                ProtoLogger.LogError("JoystickBase not assigned!", this);
                return;
            }

            if (joystickHandle == null)
            {
                ProtoLogger.LogError("JoystickHandle not assigned!", this);
                return;
            }

            // ジョイスティックの中心位置を保存（ベースの中心）
            JoystickCenter = Vector2.zero; // ベースの中心を原点とする

            ProtoLogger.LogUI(
                $"Joystick initialized - maxDistance: {maxDistance}, deadZone: {deadZone}, center: {JoystickCenter}",
                this);

            // ハンドルを中心に配置
            ResetJoystickHandle();

            // 初期状態でリセット
            joystickValue = Vector2.zero;
            IsPressed = false;
            currentPointerId = -1;

            ProtoLogger.LogUI("VirtualJoystick initialized successfully", this);
        }

        /// <summary>
        ///     ジョイスティックハンドルをリセット
        /// </summary>
        private void ResetJoystickHandle()
        {
            if (joystickHandle != null) joystickHandle.anchoredPosition = JoystickCenter;
        }

        /// <summary>
        ///     ジョイスティックの設定を更新
        /// </summary>
        public void UpdateSettings(float newMaxDistance, float newDeadZone, bool newSnapToCenter)
        {
            maxDistance = newMaxDistance;
            deadZone = newDeadZone;
            snapToCenter = newSnapToCenter;
        }

        /// <summary>
        ///     ジョイスティックを手動でリセット
        /// </summary>
        public void ResetJoystick()
        {
            joystickValue = Vector2.zero;
            IsPressed = false;
            currentPointerId = -1;
            ResetJoystickHandle();

            if (inputManager != null) inputManager.SetJoystickInput(Vector2.zero);
        }

        /// <summary>
        ///     ジョイスティックの有効/無効を切り替え
        /// </summary>
        public void SetJoystickEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled) ResetJoystick();
        }

        /// <summary>
        ///     ジョイスティックの方向を取得
        /// </summary>
        public Vector2 GetJoystickDirection()
        {
            return joystickValue.normalized;
        }

        /// <summary>
        ///     ジョイスティックの値を取得
        /// </summary>
        public Vector2 GetJoystickValue()
        {
            return joystickValue;
        }

        /// <summary>
        ///     ジョイスティックの大きさを取得
        /// </summary>
        public float GetJoystickMagnitude()
        {
            return joystickValue.magnitude;
        }

        /// <summary>
        ///     ジョイスティックがアクティブかどうかを取得
        /// </summary>
        public bool IsJoystickActive()
        {
            return joystickValue.magnitude > deadZone;
        }
    }
}