namespace Proto.Input.Gestures
{
    /// <summary>
    ///     ジェスチャーの種類を定義する列挙型
    /// </summary>
    public enum ProtoGestureType
    {
        Tap,
        DoubleTap,
        Swipe,
        LongPress,
        Pinch
    }

    /// <summary>
    ///     スワイプの方向を定義する列挙型
    /// </summary>
    public enum ProtoSwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}