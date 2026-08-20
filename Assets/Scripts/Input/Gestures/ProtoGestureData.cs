using System;
using UnityEngine;

namespace Proto.Input.Gestures
{
    /// <summary>
    ///     ジェスチャーデータを格納する構造体
    ///     検出されたジェスチャーの詳細情報を保持
    /// </summary>
    [Serializable]
    public struct ProtoGestureData
    {
        public ProtoGestureType type;
        public Vector2 position;
        public Vector2 startPosition;
        public Vector2 endPosition;
        public Vector2 direction;
        public ProtoSwipeDirection swipeDirection;
        public float distance;
        public float duration;
        public float pinchDelta;
        public float pinchDistance;
        public int fingerCount;
    }
}