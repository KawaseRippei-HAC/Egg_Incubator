using UnityEngine;

/// <summary>
/// タイトル・プレイ中・リザルト・リトライのUI切り替え(仕様書3.7節)。
/// 「操作方法を伝える導入演出(テキストを用いない誘導)」もここに含める想定。
/// ステータス: 未実装(スタブ)。
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("UIルート (Canvas配下のパネル等)")]
    public GameObject titleUI;
    public GameObject resultUI;
    public GameObject retryUI;

    public void ShowTitle()
    {
        // TODO
    }

    public void ShowPlaying()
    {
        // TODO
    }

    /// <summary>FeedbackController.PlaySuccessFeedback()から呼ばれる</summary>
    public void ShowSuccessResult()
    {
        // TODO
    }

    /// <summary>FeedbackController.PlayFailureFeedback()から呼ばれる</summary>
    public void ShowFailureResult()
    {
        // TODO
    }

    public void ShowRetry()
    {
        // TODO
    }
}
