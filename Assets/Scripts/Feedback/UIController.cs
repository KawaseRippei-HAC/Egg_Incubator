using System.Collections;
using TMPro;
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
    public GameObject playingUI;
    public GameObject resultUI;
    public GameObject retryUI;

    // ★元の設計はノンバーバル(テキストを使わない)方針だったが、短時間で何度も
    // 人が入れ替わる展示では「何をすればいいか」が伝わらないため、
    // 各状態に短い日本語のガイドを出す方針へ変更した。
    [Header("テキスト (TextMeshPro)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI playingText;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultBodyText;
    public TextMeshProUGUI retryText;

    [Header("表示する文言")]
    public string titleMessage = "タップしてスタート";
    public string playingMessage = "卵を落とさず孵化させろ";
    public string successTitle = "ゲームクリア";
    public string successBody = "卵が孵化した";
    public string failureTitle = "ゲームオーバー";
    public string failureBody = "卵が割れた";
    public string retryMessage = "タップしてリトライ";

    [Header("孵化までの残り時間")]
    [Tooltip("残り時間表示のルート。PLAYING中だけ表示する")]
    public GameObject timerUI;

    [Tooltip("進行度に応じて横幅が変わるバー。anchorMax.xで伸縮させるのでSpriteは不要")]
    public RectTransform timerFill;

    [Tooltip("残り秒数の数字。不要ならnullで可")]
    public TextMeshProUGUI timerText;

    [Tooltip("残りが少ないときのバーの色")]
    public Color timerUrgentColor = new Color(1f, 0.35f, 0.25f, 1f);

    [Tooltip("通常時のバーの色")]
    public Color timerNormalColor = new Color(0.45f, 1f, 0.6f, 1f);

    [Tooltip("この残り割合を下回ると色が警告色へ変わる")]
    [Range(0f, 1f)] public float timerUrgentRatio = 0.25f;

    [Header("プレイ中メッセージ")]
    [Tooltip("表示し続けるとゲーム画面の邪魔になるので、この秒数で消す。0以下なら消さない")]
    public float playingMessageDuration = 3f;
    [Tooltip("消えるときのフェード時間(秒)")]
    public float playingMessageFade = 0.7f;

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

    [Tooltip("フラッシュの減衰カーブ(横=経過0〜1、縦=不透明度の倍率)。\n" +
             "既定は一瞬で最大→素早く落ちて尾を引く形。空にすると線形")]
    public AnimationCurve flashCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.15f, 0.55f), new Keyframe(1f, 0f));

    private Coroutine _flashRoutine;
    private Coroutine _playingRoutine;

    public void ShowTitle()
    {
        SetActiveSafe(titleUI, true);
        SetActiveSafe(playingUI, false);
        SetActiveSafe(resultUI, false);
        SetActiveSafe(retryUI, false);
        SetActiveSafe(timerUI, false);

        SetText(titleText, titleMessage);
        StopPlayingMessage();
    }

    public void ShowPlaying()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(resultUI, false);
        SetActiveSafe(retryUI, false);
        SetActiveSafe(timerUI, true);

        // 「何をするゲームか」を最初の数秒だけ出す。出しっぱなしだと視界の邪魔になる。
        SetActiveSafe(playingUI, true);
        SetText(playingText, playingMessage);
        StopPlayingMessage();
        if (playingText != null && playingMessageDuration > 0f)
        {
            _playingRoutine = StartCoroutine(FadeOutPlayingMessage());
        }
    }

    private IEnumerator FadeOutPlayingMessage()
    {
        SetAlpha(playingText, 1f);
        yield return new WaitForSeconds(playingMessageDuration);

        float t = 0f;
        float d = Mathf.Max(playingMessageFade, 0.0001f);
        while (t < d)
        {
            t += Time.deltaTime;
            SetAlpha(playingText, 1f - Mathf.Clamp01(t / d));
            yield return null;
        }
        SetAlpha(playingText, 0f);
        SetActiveSafe(playingUI, false);
        _playingRoutine = null;
    }

    private void StopPlayingMessage()
    {
        if (_playingRoutine != null)
        {
            StopCoroutine(_playingRoutine);
            _playingRoutine = null;
        }
        SetAlpha(playingText, 1f);
    }

    /// <summary>
    /// 孵化までの残り時間を更新する。GameManagerがPLAYING中に毎フレーム呼ぶ。
    /// バーは「残っている量」を表すので、progressが進むほど縮む。
    /// </summary>
    /// <param name="progress">0〜1の進行度</param>
    /// <param name="remainingSeconds">残り秒数</param>
    public void UpdateTimer(float progress, float remainingSeconds)
    {
        float remainRatio = Mathf.Clamp01(1f - progress);

        if (timerFill != null)
        {
            // anchorMaxのxだけを動かして左詰めで縮ませる(SpriteもfillTypeも不要)
            Vector2 max = timerFill.anchorMax;
            max.x = remainRatio;
            timerFill.anchorMax = max;

            var img = timerFill.GetComponent<Image>();
            if (img != null)
            {
                img.color = remainRatio <= timerUrgentRatio ? timerUrgentColor : timerNormalColor;
            }
        }

        if (timerText != null)
        {
            // 0.9秒を「0」と出すと嘘になるので切り上げる
            timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
        }
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }

    private static void SetAlpha(TextMeshProUGUI target, float a)
    {
        if (target == null) return;
        Color c = target.color;
        c.a = a;
        target.color = c;
    }

    /// <summary>FeedbackController.PlaySuccessFeedback()から呼ばれる</summary>
    public void ShowSuccessResult()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(playingUI, false);
        SetActiveSafe(resultUI, true);
        SetActiveSafe(timerUI, false);
        StopPlayingMessage();
        if (resultBackground != null) resultBackground.color = successColor;
        SetText(resultTitleText, successTitle);
        SetText(resultBodyText, successBody);
        // ★フラッシュはここでは出さない。
        // ShowSuccessResult()はGameManagerがsuccessResultDelay(3秒)待ってから呼ぶため、
        // ここで光らせると「孵化した瞬間」ではなく「リザルト画面が出た瞬間」に光ってしまう。
        // 現在はImpactFxController.PlaySuccessImpact()がイベントの瞬間に発火させる。
        ShowRetry();
    }

    /// <summary>FeedbackController.PlayFailureFeedback()から呼ばれる</summary>
    public void ShowFailureResult()
    {
        SetActiveSafe(titleUI, false);
        SetActiveSafe(playingUI, false);
        SetActiveSafe(resultUI, true);
        SetActiveSafe(timerUI, false);
        StopPlayingMessage();
        if (resultBackground != null) resultBackground.color = failureColor;
        SetText(resultTitleText, failureTitle);
        SetText(resultBodyText, failureBody);
        // 成功側と同じ理由でここでは光らせない(ImpactFxControllerが衝突の瞬間に発火させる)
        ShowRetry();
    }

    public void ShowRetry()
    {
        SetActiveSafe(retryUI, true);
        SetText(retryText, retryMessage);
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
            // ヒットストップ(ImpactFxControllerがTime.timeScaleを落とす)の影響を受けないよう実時間で進める
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / flashDuration);
            // 線形だとぼんやり消えるだけなので、立ち上がりは瞬間・減衰は速く落として尾を引かせる
            float k = flashCurve != null && flashCurve.length > 0 ? flashCurve.Evaluate(p) : 1f - p;
            flashImage.alpha = color.a * k;
            yield return null;
        }
        flashImage.alpha = 0f;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
