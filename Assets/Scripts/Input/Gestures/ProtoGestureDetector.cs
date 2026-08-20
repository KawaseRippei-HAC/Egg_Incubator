using System;
using System.Collections.Generic;
using Proto.Core;
using UnityEngine;

namespace Proto.Input.Gestures
{
    /// <summary>
    ///     ジェスチャー検出クラス
    ///     タップ、スワイプ、ピンチ、ロングプレスなどのジェスチャーを検出
    /// </summary>
    public class ProtoGestureDetector
    {
        private readonly Dictionary<ProtoGestureType, Action<ProtoGestureData>> gestureCallbacks;
        private readonly Dictionary<int, TouchInfo> previousTouches;
        private readonly ProtoGestureSettings settings;

        public ProtoGestureDetector(ProtoGestureSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            gestureCallbacks = new Dictionary<ProtoGestureType, Action<ProtoGestureData>>();
            previousTouches = new Dictionary<int, TouchInfo>();

            ProtoLogger.LogGesture("GestureDetector initialized successfully");
        }

        // イベント
        public event Action<ProtoGestureData> OnGestureDetected;

        /// <summary>
        ///     ジェスチャー検出を更新
        /// </summary>
        public void Update()
        {
            ProcessTouches();
        }

        /// <summary>
        ///     タッチを処理
        /// </summary>
        public void ProcessTouch(Touch touch)
        {
            var touchInfo = new TouchInfo
            {
                fingerId = touch.fingerId,
                position = touch.position,
                phase = touch.phase,
                time = Time.time
            };

            ProcessTouchInfo(touchInfo);
        }

        /// <summary>
        ///     タッチを処理（内部メソッド）
        /// </summary>
        private void ProcessTouches()
        {
            // 現在のタッチを処理
            for (var i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                var touch = UnityEngine.Input.GetTouch(i);
                ProcessTouch(touch);
            }
        }

        /// <summary>
        ///     タッチ情報を処理
        /// </summary>
        private void ProcessTouchInfo(TouchInfo touchInfo)
        {
            switch (touchInfo.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touchInfo);
                    break;
                case TouchPhase.Moved:
                    HandleTouchMoved(touchInfo);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded(touchInfo);
                    break;
            }

            // タッチ情報を保存
            previousTouches[touchInfo.fingerId] = touchInfo;
        }

        /// <summary>
        ///     タッチ開始を処理
        /// </summary>
        private void HandleTouchBegan(TouchInfo touchInfo)
        {
            // ロングプレス検出の開始
            if (settings != null)
                // ロングプレス検出の実装
                DetectLongPress(touchInfo);
        }

        /// <summary>
        ///     タッチ移動を処理
        /// </summary>
        private void HandleTouchMoved(TouchInfo touchInfo)
        {
            if (!previousTouches.ContainsKey(touchInfo.fingerId)) return;

            var previousTouch = previousTouches[touchInfo.fingerId];
            var distance = Vector2.Distance(previousTouch.position, touchInfo.position);
            var timeDelta = touchInfo.time - previousTouch.time;

            // スワイプ検出
            if (distance > settings.SwipeMinDistance && timeDelta < settings.SwipeMaxTime)
                DetectSwipe(previousTouch, touchInfo);
        }

        /// <summary>
        ///     タッチ終了を処理
        /// </summary>
        private void HandleTouchEnded(TouchInfo touchInfo)
        {
            if (!previousTouches.ContainsKey(touchInfo.fingerId)) return;

            var previousTouch = previousTouches[touchInfo.fingerId];
            var duration = touchInfo.time - previousTouch.time;
            var distance = Vector2.Distance(previousTouch.position, touchInfo.position);

            // タップ検出
            if (duration < settings.TapTimeThreshold && distance < settings.TapDistanceThreshold) DetectTap(touchInfo);

            // 前のタッチ情報を削除
            previousTouches.Remove(touchInfo.fingerId);
        }

        /// <summary>
        ///     タップを検出
        /// </summary>
        private void DetectTap(TouchInfo touchInfo)
        {
            var gestureData = new ProtoGestureData
            {
                type = ProtoGestureType.Tap,
                position = touchInfo.position,
                startPosition = touchInfo.position,
                endPosition = touchInfo.position,
                duration = 0f,
                distance = 0f,
                direction = Vector2.zero
            };

            OnGestureDetected?.Invoke(gestureData);
            ProtoLogger.LogGesture($"Tap detected at {touchInfo.position}");
        }

        /// <summary>
        ///     スワイプを検出
        /// </summary>
        private void DetectSwipe(TouchInfo startTouch, TouchInfo endTouch)
        {
            var direction = (endTouch.position - startTouch.position).normalized;
            var distance = Vector2.Distance(startTouch.position, endTouch.position);
            var duration = endTouch.time - startTouch.time;

            var swipeDirection = GetSwipeDirection(direction);

            var gestureData = new ProtoGestureData
            {
                type = ProtoGestureType.Swipe,
                position = endTouch.position,
                startPosition = startTouch.position,
                endPosition = endTouch.position,
                duration = duration,
                distance = distance,
                direction = direction,
                swipeDirection = swipeDirection
            };

            OnGestureDetected?.Invoke(gestureData);
            ProtoLogger.LogGesture($"Swipe detected: {swipeDirection} at {endTouch.position}");
        }

        /// <summary>
        ///     ロングプレスを検出
        /// </summary>
        private void DetectLongPress(TouchInfo touchInfo)
        {
            // ロングプレス検出の実装
            // 実際の実装では、一定時間後にロングプレスを検出する
        }

        /// <summary>
        ///     スワイプ方向を取得
        /// </summary>
        private ProtoSwipeDirection GetSwipeDirection(Vector2 direction)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (angle >= -45f && angle < 45f)
                return ProtoSwipeDirection.Right;
            if (angle >= 45f && angle < 135f)
                return ProtoSwipeDirection.Up;
            if (angle >= 135f || angle < -135f)
                return ProtoSwipeDirection.Left;
            return ProtoSwipeDirection.Down;
        }

        /// <summary>
        ///     ジェスチャーコールバックを登録
        /// </summary>
        public void RegisterCallback(ProtoGestureType gestureType, Action<ProtoGestureData> callback)
        {
            if (callback != null) gestureCallbacks[gestureType] = callback;
        }

        /// <summary>
        ///     ジェスチャーコールバックを解除
        /// </summary>
        public void UnregisterCallback(ProtoGestureType gestureType)
        {
            if (gestureCallbacks.ContainsKey(gestureType)) gestureCallbacks.Remove(gestureType);
        }

        /// <summary>
        ///     設定を更新
        /// </summary>
        public void UpdateSettings(ProtoGestureSettings newSettings)
        {
            if (newSettings != null)
                // 設定を更新する実装
                ProtoLogger.LogGesture("Gesture settings updated");
        }

        /// <summary>
        ///     タッチ情報の構造体
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