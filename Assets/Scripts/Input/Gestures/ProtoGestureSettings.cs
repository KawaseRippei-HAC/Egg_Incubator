using Proto.Core;
using UnityEngine;

namespace Proto.Input.Gestures
{
    /// <summary>
    ///     ジェスチャー検出の設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ProtoGestureSettings", menuName = "Proto/Gesture Settings")]
    public class ProtoGestureSettings : ScriptableObject
    {
        [Header("Tap Settings")] [SerializeField]
        private float tapTimeThreshold = ProtoConstants.Gesture.DefaultTapTimeThreshold;

        [SerializeField] private float doubleTapTimeThreshold = ProtoConstants.Gesture.DefaultDoubleTapTimeThreshold;
        [SerializeField] private float tapDistanceThreshold = ProtoConstants.Gesture.DefaultTapDistanceThreshold;

        [Header("Long Press Settings")] [SerializeField]
        private float longPressTimeThreshold = ProtoConstants.Gesture.DefaultLongPressTimeThreshold;

        [Header("Swipe Settings")] [SerializeField]
        private float swipeMinDistance = ProtoConstants.Gesture.DefaultSwipeMinDistance;

        [SerializeField] private float swipeMaxTime = ProtoConstants.Gesture.DefaultSwipeMaxTime;
        [SerializeField] private float swipeAngleThreshold = ProtoConstants.Gesture.DefaultSwipeAngleThreshold;

        [Header("Pinch Settings")] [SerializeField]
        private float pinchMinDistance = ProtoConstants.Gesture.DefaultPinchMinDistance;

        [SerializeField] private float pinchSensitivity = ProtoConstants.Gesture.DefaultPinchSensitivity;

        [Header("Flick Settings")] [SerializeField]
        private float flickMinVelocity = ProtoConstants.Gesture.DefaultFlickMinVelocity;

        [SerializeField] private float flickMaxTime = ProtoConstants.Gesture.DefaultFlickMaxTime;

        // Properties
        public float TapTimeThreshold => tapTimeThreshold;
        public float DoubleTapTimeThreshold => doubleTapTimeThreshold;
        public float TapDistanceThreshold => tapDistanceThreshold;
        public float LongPressTimeThreshold => longPressTimeThreshold;
        public float SwipeMinDistance => swipeMinDistance;
        public float SwipeMaxTime => swipeMaxTime;
        public float SwipeAngleThreshold => swipeAngleThreshold;
        public float PinchMinDistance => pinchMinDistance;
        public float PinchSensitivity => pinchSensitivity;
        public float FlickMinVelocity => flickMinVelocity;
        public float FlickMaxTime => flickMaxTime;

        /// <summary>
        ///     設定をデフォルト値にリセット
        /// </summary>
        public void ResetToDefaults()
        {
            tapTimeThreshold = ProtoConstants.Gesture.DefaultTapTimeThreshold;
            doubleTapTimeThreshold = ProtoConstants.Gesture.DefaultDoubleTapTimeThreshold;
            tapDistanceThreshold = ProtoConstants.Gesture.DefaultTapDistanceThreshold;
            longPressTimeThreshold = ProtoConstants.Gesture.DefaultLongPressTimeThreshold;
            swipeMinDistance = ProtoConstants.Gesture.DefaultSwipeMinDistance;
            swipeMaxTime = ProtoConstants.Gesture.DefaultSwipeMaxTime;
            swipeAngleThreshold = ProtoConstants.Gesture.DefaultSwipeAngleThreshold;
            pinchMinDistance = ProtoConstants.Gesture.DefaultPinchMinDistance;
            pinchSensitivity = ProtoConstants.Gesture.DefaultPinchSensitivity;
            flickMinVelocity = ProtoConstants.Gesture.DefaultFlickMinVelocity;
            flickMaxTime = ProtoConstants.Gesture.DefaultFlickMaxTime;
        }

        /// <summary>
        ///     設定値を検証
        /// </summary>
        public void ValidateSettings()
        {
            tapTimeThreshold = Mathf.Max(0.01f, tapTimeThreshold);
            doubleTapTimeThreshold = Mathf.Max(tapTimeThreshold, doubleTapTimeThreshold);
            tapDistanceThreshold = Mathf.Max(1f, tapDistanceThreshold);
            longPressTimeThreshold = Mathf.Max(0.1f, longPressTimeThreshold);
            swipeMinDistance = Mathf.Max(10f, swipeMinDistance);
            swipeMaxTime = Mathf.Max(0.1f, swipeMaxTime);
            swipeAngleThreshold = Mathf.Clamp(swipeAngleThreshold, 0f, 90f);
            pinchMinDistance = Mathf.Max(1f, pinchMinDistance);
            pinchSensitivity = Mathf.Max(0.1f, pinchSensitivity);
            flickMinVelocity = Mathf.Max(10f, flickMinVelocity);
            flickMaxTime = Mathf.Max(0.1f, flickMaxTime);
        }
    }
}