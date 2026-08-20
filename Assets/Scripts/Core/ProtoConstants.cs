namespace Proto.Core
{
    /// <summary>
    ///     プロジェクト全体で使用する定数を管理
    /// </summary>
    public static class ProtoConstants
    {
        #region Input Constants

        public static class Input
        {
            public const float DefaultDeadZone = 0.1f;
            public const float DefaultMaxDistance = 50f;
            public const float DefaultMouseSensitivity = 1f;
            public const float InputThreshold = 0.1f;
            public const float NormalizedMaxValue = 1f;
        }

        #endregion

        #region Gesture Constants

        public static class Gesture
        {
            public const float DefaultTapTimeThreshold = 0.3f;
            public const float DefaultDoubleTapTimeThreshold = 0.5f;
            public const float DefaultTapDistanceThreshold = 50f;
            public const float DefaultLongPressTimeThreshold = 1.0f;
            public const float DefaultSwipeMinDistance = 100f;
            public const float DefaultSwipeMaxTime = 1.0f;
            public const float DefaultSwipeAngleThreshold = 45f;
            public const float DefaultPinchMinDistance = 50f;
            public const float DefaultPinchSensitivity = 1.0f;
            public const float DefaultFlickMinVelocity = 500f;
            public const float DefaultFlickMaxTime = 0.5f;
        }

        #endregion

        #region UI Constants

        public static class UI
        {
            public const float DefaultUpdateInterval = 0.1f;
            public const int MaxTouchHistoryCount = 5;
            public const float DefaultJoystickMaxDistance = 50f;
            public const float DefaultJoystickDeadZone = 0.1f; // 正規化された値（0-1の範囲）
        }

        #endregion

        #region Safe Area Constants

        public static class SafeArea
        {
            public const float DefaultMockTop = 0.1f;
            public const float DefaultMockBottom = 0.05f;
            public const float DefaultMockLeft = 0.05f;
            public const float DefaultMockRight = 0.05f;
            public const float CleanupInterval = 10f;
            public const float TouchHistoryTimeout = 5f;
        }

        #endregion
    }
}