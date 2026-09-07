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
    [Tooltip("危険度が最大のときの振動間隔(秒)。Handheld.Vibrate()はAndroidで約0.5秒鳴るため、" +
             "これを0.3秒まで詰めると事実上鳴りっぱなしになる")]
    public float minVibrateInterval = 0.45f;
    public float maxVibrateInterval = 1.2f;

    [Tooltip("この危険度未満では振動させない。\n" +
             "★卵はほぼ常にフライパン中心から少しずれているため、ここが低いと" +
             "プレイ中ずっと振動し続けて『危ない』という情報が伝わらなくなる。" +
             "CameraController.dangerShakeThreshold(0.6)と近い水準にして、" +
             "本当に落ちそうなときだけ鳴らす")]
    public float minDangerToVibrate = 0.5f;

    private float _nextVibrateTime;

    /// <summary>危険度に応じた継続的な短い振動(心音と同じ間隔を想定)。FeedbackController.UpdateDangerFeedback()から毎回呼ばれる。</summary>
    public void PlayHeartbeat(float danger)
    {
        if (danger < minDangerToVibrate) return;
        if (Time.time < _nextVibrateTime) return;

        // Handheld.Vibrateは強さを指定できないため、危険度が高いほど間隔を詰めることで
        // 「強弱」を頻度の変化として近似する(仕様書3.3節・10章)。
        float interval = Mathf.Lerp(maxVibrateInterval, minVibrateInterval, Mathf.Clamp01(danger));
        Handheld.Vibrate();
        _nextVibrateTime = Time.time + interval;
    }

    /// <summary>視覚的なヒビ(visualCrack)が閾値を跨いだ瞬間の短い振動。FeedbackController.CheckCrackThresholdCrossing()から呼ばれる。</summary>
    public void PlayCrack()
    {
        Handheld.Vibrate();
    }

    /// <summary>孵化成功時の気持ちよい強めの短い振動。FeedbackController.PlaySuccessFeedback()から呼ばれる。</summary>
    public void PlaySuccess()
    {
        Handheld.Vibrate();
    }

    /// <summary>落下・破損時の強い一発振動。FeedbackController.PlayFailureFeedback()から呼ばれる。</summary>
    public void PlayFailure()
    {
        Handheld.Vibrate();
    }
}
