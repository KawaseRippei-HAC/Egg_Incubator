using UnityEngine;

/// <summary>
/// 振動パターン制御(仕様書3.3節)。
/// ステータス: 未実装(スタブ)。8/29午後に実装予定。
/// 現時点では FeedbackController のコンパイルを通すためのAPI定義のみ。
///
/// 実装時の注意(仕様書3.3節・10章):
/// - Handheld.Vibrate() は単純なON/OFFのみのため、強弱パターンを作るには
///   呼び出し頻度での近似 or Androidネイティブプラグインの検討が必要
/// - PlaySuccess()/PlayFailure() は FeedbackController.PlaySuccessFeedback()/
///   PlayFailureFeedback() から直接呼ばれる方式に統一済み(FeedbackController.cs冒頭コメント参照)。
///   onHatch/onBreak への重複登録はしないこと(二重発火防止)。
/// </summary>
public class HapticController : MonoBehaviour
{
    [Header("危険度に応じた振動間隔 (仕様書3.3節)")]
    public float minVibrateInterval = 0.3f;
    public float maxVibrateInterval = 1.2f;

    /// <summary>危険度に応じた継続的な短い振動(心音と同じ間隔を想定)。FeedbackController.UpdateDangerFeedback()から毎回呼ばれる。</summary>
    public void PlayHeartbeat(float danger)
    {
        // TODO(8/29午後実装): Handheld.Vibrate() またはネイティブAPI経由での強弱制御
    }

    /// <summary>視覚的なヒビ(visualCrack)が閾値を跨いだ瞬間の短い振動。FeedbackController.CheckCrackThresholdCrossing()から呼ばれる。</summary>
    public void PlayCrack()
    {
        // TODO(8/29午後実装)
    }

    /// <summary>孵化成功時の気持ちよい強めの短い振動。FeedbackController.PlaySuccessFeedback()から呼ばれる。</summary>
    public void PlaySuccess()
    {
        // TODO(8/29午後実装)
    }

    /// <summary>落下・破損時の強い一発振動。FeedbackController.PlayFailureFeedback()から呼ばれる。</summary>
    public void PlayFailure()
    {
        // TODO(8/29午後実装)
    }
}
