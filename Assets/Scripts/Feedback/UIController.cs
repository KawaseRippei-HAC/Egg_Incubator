using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル・プレイ中・リザルト・リトライのUI切り替え(仕様書3.7節)。
/// 「操作方法を伝える導入演出(テキストを用いない誘導)」もここに含める想定。
/// 本番グラフィックが用意されるまでは、色・フェード・図形のみの仮UIとして実装している。
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("UIルート (Canvas配下のパネル等)")]
    public GameObject titleUI;
    public GameObject resultUI;
    public GameObject retryUI;

    [Header("リザルト背景 (成功/失敗で色を変える仮実装)")]
    public Image resultBackground;
    public Color successColor = new Color(0.2f, 0.8f, 0.4f, 0.6f);
    public Color failureColor = new Color(0.8f, 0.15f, 0.15f, 0.6f);

    [Header("失敗時の赤フラッシュ (画面全体、常時最前面)")]
    public CanvasGroup flashImage;
    public Color flashColor = new Color(1f, 0f, 0f, 0.5f);
    public float flashDuration = 0.4f;

    private Coroutine _flashRoutine;

    public void ShowTitle()
    {
        SetActiveSafe(titleUI, true);
        SetActiveSafe(resultUI, false);
        SetActiveSafe(retryUI, false);
    }

    public void ShowPlaying()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(resultUI, false);
        SetActiveSafe(retryUI, false);
    }

    /// <summary>FeedbackController.PlaySuccessFeedback()から呼ばれる</summary>
    public void ShowSuccessResult()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(resultUI, true);
        if (resultBackground != null) resultBackground.color = successColor;
        ShowRetry();
    }

    /// <summary>FeedbackController.PlayFailureFeedback()から呼ばれる</summary>
    public void ShowFailureResult()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(resultUI, true);
        if (resultBackground != null) resultBackground.color = failureColor;
        FlashDanger();
        ShowRetry();
    }

    public void ShowRetry()
    {
        SetActiveSafe(retryUI, true);
    }

    /// <summary>画面全体を一瞬赤くフェードさせる(テキストを使わない失敗演出)</summary>
    public void FlashDanger()
    {
        if (flashImage == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashImage.alpha = flashColor.a;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashImage.alpha = Mathf.Lerp(flashColor.a, 0f, t / flashDuration);
            yield return null;
        }
        flashImage.alpha = 0f;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
