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
    [Tooltip("以前は0.6と濃かったため薄く調整(要望対応)")]
    public Color successColor = new Color(0.2f, 0.8f, 0.4f, 0.22f);
    [Tooltip("以前は0.6と濃かったため薄く調整(要望対応)")]
    public Color failureColor = new Color(0.8f, 0.15f, 0.15f, 0.22f);

    [Header("成功/失敗時のフラッシュ (画面全体、常時最前面)")]
    public CanvasGroup flashImage;
    [Tooltip("flashImageと同じオブジェクトのImage。以前はflashColorのRGBが未使用でアルファしか反映されて" +
             "いなかった(常に固定色)ため、成功/失敗で色を変えられるよう追加(要望対応)")]
    public Image flashGraphic;
    [Tooltip("失敗時のフラッシュ色")]
    public Color flashColor = new Color(1f, 0f, 0f, 0.5f);
    [Tooltip("成功時のフラッシュ色(テキストを使わない成功演出、要望対応)")]
    public Color successFlashColor = new Color(1f, 0.9f, 0.4f, 0.5f);
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
        FlashSuccess();
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
        Flash(flashColor);
    }

    /// <summary>画面全体を一瞬暖色にフェードさせる(テキストを使わない成功演出、要望対応)</summary>
    public void FlashSuccess()
    {
        Flash(successFlashColor);
    }

    private void Flash(Color color)
    {
        if (flashImage == null) return;
        if (flashGraphic != null)
        {
            flashGraphic.color = new Color(color.r, color.g, color.b, 1f);
        }
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        flashImage.alpha = color.a;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashImage.alpha = Mathf.Lerp(color.a, 0f, t / flashDuration);
            yield return null;
        }
        flashImage.alpha = 0f;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
