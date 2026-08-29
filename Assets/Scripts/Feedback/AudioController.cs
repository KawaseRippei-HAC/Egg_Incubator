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
/// </summary>
public class AudioController : MonoBehaviour
{
    [Header("再生元")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    [Header("BGMクリップ")]
    public AudioClip titleBgm;
    public AudioClip playingBgm;
    public AudioClip resultSuccessBgm;
    public AudioClip resultFailureBgm;

    [Header("SEクリップ (心音はHeartbeatController側で扱うため含まない)")]
    public AudioClip tapSe;
    public AudioClip startSe;
    public AudioClip rollingSe;

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
        if (seSource == null || rollingSe == null) return;

        if (speed > rollingSpeedThreshold)
        {
            if (seSource.clip != rollingSe || !seSource.isPlaying)
            {
                seSource.clip = rollingSe;
                seSource.loop = true;
                seSource.Play();
            }

            float t = Mathf.Clamp01(speed / rollingSpeedForMaxVolume);
            seSource.volume = t;
            seSource.pitch = Mathf.Lerp(0.8f, 1.3f, t);
        }
        else if (seSource.isPlaying && seSource.clip == rollingSe)
        {
            seSource.Stop();
        }
    }

    /// <summary>基本的に呼ばなくてよい(3.5節)。既存hatchAudioSourceで足りない追加SEがある場合のみ実装する</summary>
    public void PlaySuccess()
    {
        // 意図的に空実装。既存EggHatchController.hatchAudioSource/hatchSfxで代替可能。
    }

    /// <summary>基本的に呼ばなくてよい(3.5節)。既存breakAudioSourceで足りない追加SEがある場合のみ実装する</summary>
    public void PlayFailure()
    {
        // 意図的に空実装。既存EggBreakController.breakAudioSource/breakSfxで代替可能。
    }
}
