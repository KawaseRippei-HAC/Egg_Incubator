using System.Collections;
using System.Collections.Generic;
using Proto.Core;
using Proto.Input;
using ProtoInput.Core;
using TMPro;
using UnityEngine;

namespace Proto.UI
{
    /// <summary>
    ///     デバッグUIを管理するクラス
    /// </summary>
    public class ProtoDebugUI : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private TextMeshProUGUI inputText;

        [SerializeField] private TextMeshProUGUI safeAreaText;
        [SerializeField] private TextMeshProUGUI joystickText;
        [SerializeField] private TextMeshProUGUI touchText;

        [Header("Components")] [SerializeField]
        private ProtoInputManager inputManager;

        [SerializeField] private ProtoSafeAreaHandler safeAreaHandler;
        [SerializeField] private ProtoVirtualJoystick virtualJoystick;

        [Header("Settings")] [SerializeField] private float updateInterval = ProtoConstants.UI.DefaultUpdateInterval;

        [SerializeField] private bool enableDebugUI = true;

        // タッチ履歴
        private readonly List<TouchInfo> touchHistory = new();

        private void Start()
        {
            if (!enableDebugUI)
            {
                gameObject.SetActive(false);
                return;
            }

            // コンポーネントの自動検索
            if (inputManager == null)
                inputManager = FindObjectOfType<ProtoInputManager>();

            if (safeAreaHandler == null)
                safeAreaHandler = FindObjectOfType<ProtoSafeAreaHandler>();

            if (virtualJoystick == null)
                virtualJoystick = FindObjectOfType<ProtoVirtualJoystick>();

            StartCoroutine(UpdateDebugInfo());
        }

        /// <summary>
        ///     デバッグ情報を定期的に更新
        /// </summary>
        private IEnumerator UpdateDebugInfo()
        {
            while (enableDebugUI)
            {
                UpdateInputInfo();
                UpdateSafeAreaInfo();
                UpdateJoystickInfo();
                UpdateTouchInfo();

                yield return new WaitForSeconds(updateInterval);
            }
        }

        /// <summary>
        ///     入力情報を更新
        /// </summary>
        private void UpdateInputInfo()
        {
            if (inputText == null || inputManager == null) return;

            var keyboardInput = Vector2.zero;
            var mouseInput = Vector2.zero;
            var joystickInput = Vector2.zero;
            var touchInput = Vector2.zero;

            // キーボード入力
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                keyboardInput.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                keyboardInput.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                keyboardInput.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                keyboardInput.x += 1f;

            // マウス入力
            mouseInput = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));

            // ジョイスティック入力
            if (virtualJoystick != null) joystickInput = virtualJoystick.GetJoystickValue();

            // タッチ入力
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                touchInput = touch.deltaPosition;
            }

            // 結合された入力
            var combinedInput = keyboardInput + mouseInput + joystickInput + touchInput;

            // 入力の大きさをチェック
            var magnitude = combinedInput.magnitude;
            if (magnitude > ProtoConstants.Input.InputThreshold) // 入力がある場合のみログ出力
                ProtoLogger.LogInput($"Combined Input: {combinedInput}, Magnitude: {magnitude}", this);

            // UIテキストを更新
            var inputInfo = "Input Debug:\n" +
                            $"Keyboard: {keyboardInput}\n" +
                            $"Mouse: {mouseInput}\n" +
                            $"Joystick: {joystickInput}\n" +
                            $"Touch: {touchInput}\n" +
                            $"Combined: {combinedInput}\n" +
                            $"Magnitude: {magnitude:F2}";

            inputText.text = inputInfo;
        }

        /// <summary>
        ///     セーフエリア情報を更新
        /// </summary>
        private void UpdateSafeAreaInfo()
        {
            if (safeAreaText == null) return;

            var safeAreaInfo = "Safe Area Debug:\n";

            if (safeAreaHandler != null)
            {
                var safeArea = Screen.safeArea;
                var screenSize = new Vector2(Screen.width, Screen.height);

                safeAreaInfo += $"Screen Size: {screenSize}\n" +
                                $"Safe Area: {safeArea}\n" +
                                $"Safe Area Ratio: {safeArea.width / screenSize.x:F2} x {safeArea.height / screenSize.y:F2}\n" +
                                $"Top Offset: {screenSize.y - safeArea.yMax:F0}\n" +
                                $"Bottom Offset: {safeArea.yMin:F0}\n" +
                                $"Left Offset: {safeArea.xMin:F0}\n" +
                                $"Right Offset: {screenSize.x - safeArea.xMax:F0}";
            }
            else
            {
                safeAreaInfo += "SafeAreaHandler not found";
            }

            safeAreaText.text = safeAreaInfo;
        }

        /// <summary>
        ///     ジョイスティック情報を更新
        /// </summary>
        private void UpdateJoystickInfo()
        {
            if (joystickText == null) return;

            var joystickInfo = "Joystick Debug:\n";

            if (virtualJoystick != null)
            {
                var joystickValue = virtualJoystick.GetJoystickValue();
                var isPressed = virtualJoystick.IsPressed;

                joystickInfo += $"Value: {joystickValue}\n" +
                                $"Is Pressed: {isPressed}\n" +
                                $"Magnitude: {joystickValue.magnitude:F2}";
            }
            else
            {
                joystickInfo += "VirtualJoystick not found";
            }

            joystickText.text = joystickInfo;
        }

        /// <summary>
        ///     タッチ情報を更新
        /// </summary>
        private void UpdateTouchInfo()
        {
            if (touchText == null) return;

            // 現在のタッチ情報を取得
            var touchCount = UnityEngine.Input.touchCount;
            Vector2 mousePosition = UnityEngine.Input.mousePosition;
            var mouseScrollDelta = UnityEngine.Input.mouseScrollDelta;

            var touchInfo = "Touch Debug:\n";
            touchInfo += $"Touch Count: {touchCount}\n";
            touchInfo += $"Mouse Position: {mousePosition}\n";
            touchInfo += $"Mouse Scroll: {mouseScrollDelta}\n";

            // タッチ履歴を更新
            if (UnityEngine.Input.touchCount > 0)
                for (var i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    var touch = UnityEngine.Input.GetTouch(i);
                    UpdateTouchHistory(touch);
                }

            // タッチ履歴を表示
            touchInfo += "\nTouch History:\n";
            for (var i = 0; i < touchHistory.Count; i++)
            {
                var info = touchHistory[i];
                touchInfo += $"Touch {i}: {info.position}, Phase: {info.phase}, Time: {info.time:F2}s\n";
            }

            touchText.text = touchInfo;
        }

        /// <summary>
        ///     タッチ履歴を更新
        /// </summary>
        private void UpdateTouchHistory(Touch touch)
        {
            var currentTime = Time.time;

            // 既存のタッチ情報を更新または新規追加
            var found = false;
            for (var i = 0; i < touchHistory.Count; i++)
                if (touchHistory[i].fingerId == touch.fingerId)
                {
                    touchHistory[i] = new TouchInfo
                    {
                        fingerId = touch.fingerId,
                        position = touch.position,
                        phase = touch.phase,
                        time = currentTime
                    };
                    found = true;
                    break;
                }

            if (!found)
                touchHistory.Add(new TouchInfo
                {
                    fingerId = touch.fingerId,
                    position = touch.position,
                    phase = touch.phase,
                    time = currentTime
                });

            // 古いタッチ情報を削除
            while (touchHistory.Count > ProtoConstants.UI.MaxTouchHistoryCount) touchHistory.RemoveAt(0);

            // タイムアウトしたタッチ情報を削除
            for (var i = touchHistory.Count - 1; i >= 0; i--)
                if (currentTime - touchHistory[i].time > 5f) // 5秒でタイムアウト
                    touchHistory.RemoveAt(i);
        }

        /// <summary>
        ///     デバッグUIの表示/非表示を切り替え
        /// </summary>
        public void ToggleDebugUI()
        {
            enableDebugUI = !enableDebugUI;
            gameObject.SetActive(enableDebugUI);

            if (enableDebugUI) StartCoroutine(UpdateDebugInfo());
        }

        /// <summary>
        ///     タッチ情報を格納する構造体
        /// </summary>
        private struct TouchInfo
        {
            public int fingerId;
            public Vector2 position;
            public TouchPhase phase;
            public float time;
        }
    }
}