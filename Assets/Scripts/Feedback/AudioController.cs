using UnityEngine;

/// <summary>
/// BGM切り替えと、心音以外のSE(タップ・転がり・開始等)の再生(仕様書3.5節)。
/// ステータス: 未実装(スタブ)。
///
/// 実装時の注意(仕様書3.5節、重要):
/// - 「割れ音」「孵化音」は EggBreakController.breakAudioSource/breakSfx、
///   EggHatchController.hatchAudioSource/hatchSfx にInspectorで直接クリップを割り当てれば
///   既存スクリプト側が単独で鳴らせる(統合ガイド2.4節)。PlaySuccess()/PlayFailure()は
///   基本的に空実装のままでよい(二重再生防止)。
/// - クラス図(タマゴインキュベーター_クラス図.drawio.png)にはheartbeatSourceフィールドが
///   このクラスに描かれているが、C担当_実装仕様書.md 3.2節の記述を優先し、
///   heartbeatSourceはHeartbeatController側に持たせている(要B/全員合意の確認事項)。
///
/// ★不具合修正メモ(2026/08/30): 開始SE(startSe)がタイトル→着地の1回目は鳴るのに
/// 2回目以降は鳴らないことがある不具合を確認。原因は PlayRolling() が seSource.Play() を
/// 呼んでいたため、着地直後に PlaySE(Start) で流した PlayOneShot 再生を、卵が転がり始めた
/// 瞬間に上書き停止していたこと(AudioSource.Play()は同じAudioSource上の全ての再生を止める)。
/// 転がりSEをseSourceから分離し、専用のrollingSourceに逃がすことで解決した。
/// </summary>
public class AudioController : MonoBehaviour
{
    [Header("再生元")]
    public AudioSource bgmSource;
    [Tooltip("タップ音・開始音などの単発SE(PlayOneShot)専用。ループ再生する転がり音とは分離すること")]
    public AudioSource seSource;
    [Tooltip("転がり音(ループ)専用。seSourceと共用すると、AudioSource.Play()がPlayOneShot中の単発SEを停止させてしまう")]
    public AudioSource rollingSource;

    [Header("BGMクリップ")]
    public AudioClip titleBgm;
    public AudioClip playingBgm;
    public AudioClip resultSuccessBgm;
    public AudioClip resultFailureBgm;

    [Header("SEクリップ (心音はHeartbeatController側で扱うため含まない)")]
    public AudioClip tapSe;
    public AudioClip startSe;
    public AudioClip rollingSe;

    [Tooltip("孵化した瞬間に鳴らすSE。GameManager.Success()と同じタイミングで発火する")]
    public AudioClip successSe;
    [Tooltip("卵が地面で割れた瞬間に鳴らすSE。GameManager.HandleGroundImpact()と同じタイミングで発火する")]
    public AudioClip failureSe;

    [Tooltip("EXTRA解放のファンファーレ。未設定ならsuccessSeで代用する")]
    public AudioClip unlockSe;

    public enum BGMType { Title, Playing, ResultSuccess, ResultFailure }
    public enum SEType { Tap, Start }

    [Header("転がりSEの速度反応")]
    [Tooltip("この速度(m/s)を超えている間だけ転がりSEをループ再生する")]
    public float rollingSpeedThreshold = 0.15f;
    [Tooltip("この速度でrollingSeの音量/ピッチが最大になる")]
    public float rollingSpeedForMaxVolume = 5f;

    public void PlayBGM(BGMType type)
    {
        if (bgmSource == null) return;

        AudioClip clip = type switch
        {
            BGMType.Title => titleBgm,
            BGMType.Playing => playingBgm,
            BGMType.ResultSuccess => resultSuccessBgm,
            BGMType.ResultFailure => resultFailureBgm,
            _ => null
        };
        if (clip == null || bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = type == BGMType.Title || type == BGMType.Playing;
        bgmSource.Play();
    }

    public void PlaySE(SEType type)
    {
        if (seSource == null) return;

        AudioClip clip = type == SEType.Tap ? tapSe : startSe;
        if (clip != null)
        {
            seSource.PlayOneShot(clip);
        }
    }

    /// <summary>卵の転がり・跳ねの速さに応じて音量/ピッチを変化させる。FeedbackController.PlayEggMovementFeedback()から呼ばれる</summary>
    public void PlayRolling(float speed)
    {
        var source = rollingSource != null ? rollingSource : seSource; // rollingSource未設定時はseSourceにフォールバック(旧挙動)
        if (source == null || rollingSe == null) return;

        if (speed > rollingSpeedThreshold)
        {
            if (source.clip != rollingSe || !source.isPlaying)
            {
                source.clip = rollingSe;
                source.loop = true;
                source.Play();
            }

            float t = Mathf.Clamp01(speed / rollingSpeedForMaxVolume);
            source.volume = t;
            source.pitch = Mathf.Lerp(0.8f, 1.3f, t);
        }
        else if (source.isPlaying && source.clip == rollingSe)
        {
            source.Stop();
        }
    }

    /// <summary>転がりSEを強制的に停止する。PLAYING状態を抜けるタイミングやリトライ時の安全策として呼ぶ</summary>
    public void StopRolling()
    {
        var source = rollingSource != null ? rollingSource : seSource;
        if (source != null && source.isPlaying && source.clip == rollingSe)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// 孵化した瞬間のSE。FeedbackController.PlaySuccessFeedback()から呼ばれる。
    /// ★以前は「EggHatchController.hatchAudioSource/hatchSfxに割り当てれば鳴るので不要」として
    /// 空実装だったが、そのスロットが両方とも未設定のまま運用されており、成功の瞬間にSEが
    /// 一切鳴っていなかった。タップSEと同様にseSourceへ一本化する。
    /// </summary>
    public void PlaySuccess()
    {
        if (seSource != null && successSe != null)
        {
            seSource.PlayOneShot(successSe);
        }
    }

    /// <summary>
    /// 卵が割れた瞬間のSE。FeedbackController.PlayFailureFeedback()から呼ばれる。
    /// PlaySuccess()と同じ理由で、空実装からseSource再生に変更した。
    /// </summary>
    public void PlayFailure()
    {
        if (seSource != null && failureSe != null)
        {
            seSource.PlayOneShot(failureSe);
        }
    }

    /// <summary>EXTRA解放のファンファーレ。ExtraUnlockBannerから呼ばれる。</summary>
    public void PlayUnlock()
    {
        if (seSource == null) return;
        AudioClip clip = unlockSe != null ? unlockSe : successSe;
        if (clip != null) seSource.PlayOneShot(clip);
    }
}
