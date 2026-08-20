using System.Collections.Generic;
using Proto.Core;
using UnityEngine;

namespace Proto.Input.Gestures
{
    /// <summary>
    ///     タッチデータを管理するクラス
    /// </summary>
    public class ProtoTouchData
    {
        // 履歴管理
        private static readonly Dictionary<int, TouchHistory> touchHistory = new();
        private static float lastCleanupTime;
        public float altitudeAngle;
        public float azimuthAngle;
        public Vector2 deltaPosition;
        public float deltaTime;
        public int fingerId;
        public TouchPhase phase;
        public Vector2 position;
        public float pressure;
        public float radius;

        /// <summary>
        ///     UnityのTouchからProtoTouchDataを作成
        /// </summary>
        public static ProtoTouchData FromUnityTouch(Touch unityTouch)
        {
            if (unityTouch.fingerId < 0) return null;

            var touchData = new ProtoTouchData
            {
                fingerId = unityTouch.fingerId,
                position = unityTouch.position,
                deltaPosition = unityTouch.deltaPosition,
                deltaTime = unityTouch.deltaTime,
                phase = unityTouch.phase,
                pressure = unityTouch.pressure,
                radius = unityTouch.radius,
                altitudeAngle = unityTouch.altitudeAngle,
                azimuthAngle = unityTouch.azimuthAngle
            };

            // 履歴に追加
            AddToHistory(touchData);

            return touchData;
        }

        /// <summary>
        ///     タッチ履歴に追加
        /// </summary>
        private static void AddToHistory(ProtoTouchData touchData)
        {
            if (!touchHistory.ContainsKey(touchData.fingerId))
            {
                touchHistory[touchData.fingerId] = new TouchHistory
                {
                    startTime = Time.time,
                    startPosition = touchData.position,
                    lastPosition = touchData.position,
                    lastUpdateTime = Time.time
                };
            }
            else
            {
                var history = touchHistory[touchData.fingerId];
                history.lastPosition = touchData.position;
                history.lastUpdateTime = Time.time;
                touchHistory[touchData.fingerId] = history;
            }

            // 定期的にクリーンアップ
            if (Time.time - lastCleanupTime > ProtoConstants.SafeArea.CleanupInterval)
            {
                CleanupHistory();
                lastCleanupTime = Time.time;
            }
        }

        /// <summary>
        ///     タッチ履歴をクリーンアップ
        /// </summary>
        private static void CleanupHistory()
        {
            var currentTime = Time.time;
            var keysToRemove = new List<int>();

            foreach (var kvp in touchHistory)
                if (currentTime - kvp.Value.lastUpdateTime > ProtoConstants.SafeArea.TouchHistoryTimeout)
                    keysToRemove.Add(kvp.Key);

            foreach (var key in keysToRemove) touchHistory.Remove(key);
        }

        /// <summary>
        ///     タッチ履歴を取得
        /// </summary>
        public static TouchHistory? GetTouchHistory(int fingerId)
        {
            return touchHistory.ContainsKey(fingerId) ? touchHistory[fingerId] : null;
        }

        /// <summary>
        ///     タッチ履歴をクリア
        /// </summary>
        public static void ClearHistory()
        {
            touchHistory.Clear();
        }

        /// <summary>
        ///     アクティブなタッチ数を取得
        /// </summary>
        public static int GetActiveTouchCount()
        {
            return touchHistory.Count;
        }

        /// <summary>
        ///     タッチ履歴の構造体
        /// </summary>
        public struct TouchHistory
        {
            public float startTime;
            public Vector2 startPosition;
            public Vector2 lastPosition;
            public float lastUpdateTime;

            public float Duration => lastUpdateTime - startTime;
            public float Distance => Vector2.Distance(startPosition, lastPosition);
            public Vector2 Direction => (lastPosition - startPosition).normalized;
        }
    }
}