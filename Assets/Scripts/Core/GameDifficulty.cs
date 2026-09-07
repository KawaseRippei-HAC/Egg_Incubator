namespace Proto.Core
{
    /// <summary>タイトル画面で選ぶ難易度。</summary>
    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        /// <summary>Easy/Normal/Hardを全てクリアすると解放される最難関</summary>
        Extra = 3,
    }
}
