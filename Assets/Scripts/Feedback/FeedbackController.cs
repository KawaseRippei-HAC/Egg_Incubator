// using UnityEngine;

// /// <summary>
// /// C担当(演出・フィードバック)の統括クラス。
// /// GameManager(B担当)から danger / progress / result の通知を受け取り、
// /// 各演出サブコントローラー(Heartbeat/Haptic/Camera/Audio/Effect/UI)へ振り分ける。
// ///
// /// 既存の卵演出スクリプト(EggCrackGlowController/EggBreakController/EggHatchController)は
// /// 一切改変せず、このクラスは「呼び出す側」に徹する。
// /// 対応: C担当_実装仕様書.md 3.1節・5章・6章
// ///
// /// ★ 重要な設計判断(仕様書内の記述を整理した結果、このスクリプトで採用した方針)
// /// 仕様書3.1節は「PlaySuccessFeedback()がhapticController.PlaySuccess()等を直接呼ぶ」とし、
// /// 一方で3.3節は「HapticController.PlaySuccess()はeggHatchController.onHatchに(Inspector等で)
// /// 登録する」としており、両方を同時に行うと成功/失敗の演出が二重発火する
// /// (8章チェックリストが警告している事象そのもの)。
// ///
// /// このスクリプトでは【PlaySuccessFeedback()/PlayFailureFeedback()からの直接呼び出しのみを正】とし、
// /// onHatch/onBreakへのHaptic/Cameraの重複登録はしない方針を採用した。B担当のGameManagerが
// /// 6章のサンプルの通り TriggerHatch()/TriggerBreak() と同じタイミングで
// /// PlaySuccessFeedback()/PlayFailureFeedback() を呼ぶことを前提にしている。
// /// この前提が崩れる場合(onHatch/onBreak経由のみで発火させたい等)は要相談。
// /// </summary>
// public class FeedbackController : MonoBehaviour
// {
//     [Header("演出サブコントローラー (C担当・新規実装)")]
//     public HeartbeatController heartbeatController;
//     public HapticController hapticController;
//     public CameraController cameraController;
//     public EffectController effectController;
//     public AudioController audioController;
//     public UIController uiController;

//     [Header("既存の卵演出スクリプト (実装済み・改変しない)")]
//     [Tooltip("危険度(dangerLevel)の入力先。SetDangerLevel()のみを呼ぶ。TriggerBreak/TriggerHatchはB担当側の責任")]
//     public EggCrackGlowController eggCrackGlowController;

//     [Header("危険度(danger)と進行度(progress)の合成 (仕様書5章: visualCrack = Max(danger, progress))")]
//     [Tooltip("crackThresholdsを上向きに跨いだ瞬間にhapticController.PlayCrack()を1回だけ呼ぶための閾値リスト")]
//     public float[] crackThresholds = { 0.33f, 0.66f };

//     private float _currentDanger;
//     private float _currentProgress;
//     private float _lastVisualCrack = -1f;

//     private void Awake()
//     {
//         // 統合ガイド1.3節: testAutoPlayがtrueのままだと外部からのSetDangerLevel()が
//         // 毎フレーム上書きされてしまうため、ロジック制御を始める前に必ずfalseにする。
//         if (eggCrackGlowController != null)
//         {
//             eggCrackGlowController.testAutoPlay = false;
//         }
//         else
//         {
//             Debug.LogWarning($"[{nameof(FeedbackController)}] eggCrackGlowController が未設定です。Inspectorで Egg_Test の EggCrackGlowController を割り当ててください。", this);
//         }
//     }

//     /// <summary>
//     /// B担当から危険度(位置ベース, 0〜1)が更新されるたびに呼ぶ。
//     /// HeartbeatController/HapticControllerは危険度のみを見る(仕様書5章の役割分担)。
//     /// </summary>
//     public void UpdateDangerFeedback(float danger)
//     {
//         _currentDanger = Mathf.Clamp01(danger);

//         heartbeatController?.UpdateHeartbeat(_currentDanger);
//         hapticController?.PlayHeartbeat(_currentDanger);

//         RecomputeVisualCrack();
//     }

//     /// <summary>
//     /// B担当から孵化の進行度(時間経過ベース, 0〜1、あるいはcrackProgress)が更新されるたびに呼ぶ。
//     /// progressとcrackProgressが同一のものを指すかは要確認(仕様書5章の未決事項)。
//     /// </summary>
//     public void UpdateCrackFeedback(float crackProgress)
//     {
//         _currentProgress = Mathf.Clamp01(crackProgress);
//         RecomputeVisualCrack();
//     }

//     /// <summary>
//     /// danger(位置)とprogress(時間)を合成し、殻の発光(eggCrackGlowController)へ渡す。
//     /// 仕様書5章: どちらか高い方を採用(Max)することで、2つの意味を1チャンネルに矛盾なく載せる。
//     /// </summary>
//     private void RecomputeVisualCrack()
//     {
//         float visualCrack = Mathf.Max(_currentDanger, _currentProgress);

//         if (eggCrackGlowController != null)
//         {
//             eggCrackGlowController.SetDangerLevel(visualCrack);
//         }

//         CheckCrackThresholdCrossing(visualCrack);
//         _lastVisualCrack = visualCrack;
//     }

//     /// <summary>
//     /// crackThresholds(既定0.33 / 0.66)を上向きに跨いだ瞬間、1回だけPlayCrack()を呼ぶ。
//     /// 既存アセットには「ヒビが1段階増えた」というイベントが無いための近似実装(仕様書5章)。
//     /// </summary>
//     private void CheckCrackThresholdCrossing(float visualCrack)
//     {
//         if (_lastVisualCrack < 0f || hapticController == null) return;

//         foreach (var threshold in crackThresholds)
//         {
//             if (_lastVisualCrack < threshold && visualCrack >= threshold)
//             {
//                 hapticController.PlayCrack();
//                 break; // 1フレームで複数閾値を跨いでも発火は1回のみ
//             }
//         }
//     }

//     /// <summary>
//     /// 孵化成功が確定した際にB担当から呼ぶ(TriggerHatch()と同じタイミングを想定、仕様書6章)。
//     /// TriggerHatch()自体はここでは呼ばない。
//     /// </summary>
//     public void PlaySuccessFeedback()
//     {
//         heartbeatController?.StopHeartbeat();
//         hapticController?.PlaySuccess();
//         cameraController?.PlaySuccessCamera();
//         audioController?.PlaySuccess(); // 任意。破片音を既存スクリプト側(hatchAudioSource)で鳴らす場合は空実装のままでよい(3.5節)
//         uiController?.ShowSuccessResult();
//     }

//     /// <summary>
//     /// 落下失敗が確定した際にB担当から呼ぶ(TriggerBreak()と同じタイミングを想定、仕様書6章)。
//     /// TriggerBreak()自体はここでは呼ばない。
//     /// </summary>
//     public void PlayFailureFeedback()
//     {
//         heartbeatController?.StopHeartbeat();
//         hapticController?.PlayFailure();
//         audioController?.PlayFailure(); // 任意。既存スクリプト側(breakAudioSource)運用でも可(3.5節)
//         uiController?.ShowFailureResult();
//     }

//     /// <summary>
//     /// 卵の転がり・跳ねの速さに応じた演出(A担当 EggController.GetSpeed()等から呼ぶ)。
//     /// </summary>
//     public void PlayEggMovementFeedback(float speed)
//     {
//         audioController?.PlayRolling(speed);
//         effectController?.PlayRolling(speed);
//     }
// }

using UnityEngine;

/// <summary>
/// C担当(演出・フィードバック)の統括クラス。
/// GameManager(B担当)から danger / progress / result の通知を受け取り、
/// 各演出サブコントローラー(Heartbeat/Haptic/Camera/Audio/Effect/UI)へ振り分ける。
///
/// 既存の卵演出スクリプト(EggCrackGlowController/EggBreakController/EggHatchController)は
/// 一切改変せず、このクラスは「呼び出す側」に徹する。
/// 対応: C担当_実装仕様書.md 3.1節・5章・6章
///
/// ★ 重要な設計判断(仕様書内の記述を整理した結果、このスクリプトで採用した方針)
/// 仕様書3.1節は「PlaySuccessFeedback()がhapticController.PlaySuccess()等を直接呼ぶ」とし、
/// 一方で3.3節は「HapticController.PlaySuccess()はeggHatchController.onHatchに(Inspector等で)
/// 登録する」としており、両方を同時に行うと成功/失敗の演出が二重発火する
/// (8章チェックリストが警告している事象そのもの)。
///
/// このスクリプトでは【PlaySuccessFeedback()/PlayFailureFeedback()からの直接呼び出しのみを正】とし、
/// onHatch/onBreakへのHaptic/Cameraの重複登録はしない方針を採用した。B担当のGameManagerが
/// 6章のサンプルの通り TriggerHatch()/TriggerBreak() と同じタイミングで
/// PlaySuccessFeedback()/PlayFailureFeedback() を呼ぶことを前提にしている。
/// この前提が崩れる場合(onHatch/onBreak経由のみで発火させたい等)は要相談。
/// </summary>
public class FeedbackController : MonoBehaviour
{
    [Header("演出サブコントローラー (C担当・新規実装)")]
    public HeartbeatController heartbeatController;
    public HapticController hapticController;
    public CameraController cameraController;
    public EffectController effectController;
    public AudioController audioController;
    public UIController uiController;

    [Tooltip("孵化の見せ場演出。発火はEggHatchController.onHatchから直接行うため、" +
             "ここではリトライ時のリセットのためだけに参照する")]
    public HatchShowcaseController hatchShowcaseController;

    [Tooltip("孵化・破壊の『瞬間』を強調する衝撃演出(ヒットストップ/Bloom/色収差/フラッシュ)")]
    public ImpactFxController impactFxController;

    [Header("ひび割れの出方")]
    [Tooltip("進行度(0〜1)を、ひび割れの見え方(_CrackAmount)へ変換するカーブ。\n" +
             "そのまま渡すとゲーム中盤までヒビが『点』のままで見えないため、序盤を持ち上げる。\n" +
             "空にすると変換なし(そのまま渡す)")]
    public AnimationCurve crackRevealCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.3f, 0.48f), new Keyframe(1f, 1f));

    [Header("スイートスポット中の卵の演出")]
    [Tooltip("スイートスポットに入っている間、卵の輪郭を光らせる強さ")]
    [Range(0f, 1f)] public float sweetSpotEggGlow = 1f;

    [Header("既存の卵演出スクリプト (実装済み・改変しない)")]
    [Tooltip("危険度(dangerLevel)の入力先。SetDangerLevel()のみを呼ぶ。TriggerBreak/TriggerHatchはB担当側の責任")]
    public EggCrackGlowController eggCrackGlowController;

    // ★2025/08/29 GameManager.cs実装レビューにより判明した設計修正:
    // uiController.ShowSuccessResult()/ShowFailureResult() は、このクラスの
    // PlaySuccessFeedback()/PlayFailureFeedback() からは呼ばない。
    // GameManagerがHandleGroundImpact()では即座に、Success()ではsuccessResultDelay(演出待ち)後に
    // それぞれ別タイミングでuiControllerを直接呼ぶ設計になっているため
    // (演出の発火タイミングと結果UI表示のタイミングが一致しない)。
    // このクラスのuiControllerフィールドはShowPlaying()等、結果画面以外の用途のために残す。

    [Header("危険度(danger)と進行度(progress)の合成 (仕様書5章: visualCrack = Max(danger, progress))")]
    [Tooltip("crackThresholdsを上向きに跨いだ瞬間にhapticController.PlayCrack()を1回だけ呼ぶための閾値リスト")]
    public float[] crackThresholds = { 0.33f, 0.66f };

    private float _currentDanger;
    private float _currentProgress;
    private float _lastCrackProgress = -1f;

    private void Awake()
    {
        // 統合ガイド1.3節: testAutoPlayがtrueのままだと外部からのSetDangerLevel()が
        // 毎フレーム上書きされてしまうため、ロジック制御を始める前に必ずfalseにする。
        if (eggCrackGlowController != null)
        {
            eggCrackGlowController.testAutoPlay = false;
        }
        else
        {
            Debug.LogWarning($"[{nameof(FeedbackController)}] eggCrackGlowController が未設定です。Inspectorで Egg_Test の EggCrackGlowController を割り当ててください。", this);
        }

        audioController?.PlayBGM(AudioController.BGMType.Title);
    }

    /// <summary>
    /// PLAYING開始時にGameManager.StartGame()から呼ぶ。心音ループを開始する。
    /// </summary>
    public void StartGameFeedback()
    {
        heartbeatController?.StartHeartbeat();
        audioController?.PlayBGM(AudioController.BGMType.Playing);
        audioController?.PlaySE(AudioController.SEType.Start);
    }

    /// <summary>
    /// B担当から危険度(位置ベース, 卵が中心からどれだけ離れているか, 0〜1)が更新されるたびに呼ぶ。
    /// ★2026/08/30修正: dangerLevelが影響するのは心音(Heartbeat/Haptic)とカメラシェイクのみ。
    /// 殻のひび割れ演出(eggCrackGlowController)は下のUpdateCrackFeedback(crackProgress)側の担当に
    /// 一本化した(以前はMax(danger, progress)の合成値を使っていたが、要望により役割を明確に分離)。
    /// </summary>
    public void UpdateDangerFeedback(float danger)
    {
        _currentDanger = Mathf.Clamp01(danger);

        heartbeatController?.UpdateHeartbeat(_currentDanger);
        hapticController?.PlayHeartbeat(_currentDanger);
        cameraController?.PlayDangerShake(_currentDanger);
    }

    /// <summary>
    /// B担当からゲーム進行度(crackProgress, 時間経過ベース, 0〜1)が更新されるたびに呼ぶ。
    /// ★2026/08/30修正: 卵のひび割れ演出(eggCrackGlowController.SetDangerLevel)はこの値のみで
    /// 駆動する(=「どれだけ孵化に近づいているか」)。dangerLevel(位置ベース)はここには一切影響しない。
    /// </summary>
    public void UpdateCrackFeedback(float crackProgress)
    {
        _currentProgress = Mathf.Clamp01(crackProgress);

        if (eggCrackGlowController != null)
        {
            // ★進行度をそのまま渡すと、ヒビはゲーム中盤まで「点」のままで見えない。
            // Voronoiのセルが閾値を超えて初めて線になるため、序盤の立ち上がりを持ち上げる。
            // 既定カーブは pow(progress, 0.6) 相当で、progress=0.3 のとき約0.48になる。
            float shaped = crackRevealCurve != null && crackRevealCurve.length > 0
                ? Mathf.Clamp01(crackRevealCurve.Evaluate(_currentProgress))
                : _currentProgress;
            eggCrackGlowController.SetDangerLevel(shaped);
        }

        CheckCrackThresholdCrossing(_currentProgress);
        _lastCrackProgress = _currentProgress;
    }

    /// <summary>
    /// crackThresholdsを上向きに跨いだ瞬間、1回だけPlayCrack()を呼ぶ。
    /// 既存アセットには「ヒビが1段階増えた」というイベントが無いための近似実装(仕様書5章)。
    /// </summary>
    private void CheckCrackThresholdCrossing(float crackProgress)
    {
        if (_lastCrackProgress < 0f || hapticController == null) return;

        foreach (var threshold in crackThresholds)
        {
            if (_lastCrackProgress < threshold && crackProgress >= threshold)
            {
                hapticController.PlayCrack();
                break; // 1フレームで複数閾値を跨いでも発火は1回のみ
            }
        }
    }

    /// <summary>
    /// 孵化成功が確定した際にB担当から呼ぶ(TriggerHatch()と同じタイミングを想定、仕様書6章)。
    /// TriggerHatch()自体はここでは呼ばない。
    /// </summary>
    public void PlaySuccessFeedback()
    {
        heartbeatController?.StopHeartbeat();
        hapticController?.PlaySuccess();
        cameraController?.PlaySuccessCamera();
        audioController?.PlaySuccess(); // 任意。破片音を既存スクリプト側(hatchAudioSource)で鳴らす場合は空実装のままでよい(3.5節)
        audioController?.PlayBGM(AudioController.BGMType.ResultSuccess);
        audioController?.StopRolling();
        // 孵化した「瞬間」に閃光+Bloom+ヒットストップ。
        // フラッシュはUIController.ShowSuccessResult()から移設した(あちらは3秒後に呼ばれるため、
        // 一番の見せ場ではなくリザルト画面と同時に光ってしまっていた)。
        impactFxController?.PlaySuccessImpact();
        // uiController.ShowSuccessResult()はここでは呼ばない。GameManager.SuccessSequence()が
        // successResultDelay分の演出待ちをした後に個別に呼ぶ設計(GameManager.cs実装より判明)。
    }

    /// <summary>
    /// 落下確定タイミングでB担当から呼ぶ(GameManager.HandleEggFall()から。着地[onBreak]より前)。
    /// EggFallDetector.OnEggFallを受けたGameManagerがこのメソッド経由でカメラへ中継する。
    /// </summary>
    public void StartFallFeedback(Transform egg)
    {
        cameraController?.FollowFallingEgg(egg);
        audioController?.StopRolling();
    }

    /// <summary>
    /// 落下失敗が確定した際にB担当から呼ぶ(TriggerBreak()と同じタイミングを想定、仕様書6章)。
    /// TriggerBreak()自体はここでは呼ばない。
    /// </summary>
    public void PlayFailureFeedback()
    {
        heartbeatController?.StopHeartbeat();
        hapticController?.PlayFailure();
        cameraController?.ImpactShake();
        // 卵は割れた後もコライダー無効化状態のまま物理的に落下し続け、床を貫通してしまうため、
        // 追従(Following)を止めてその場でカメラを静止させる(6章の未決事項・要望対応)。
        cameraController?.StopFollowing();
        audioController?.PlayFailure(); // 任意。既存スクリプト側(breakAudioSource)運用でも可(3.5節)
        audioController?.PlayBGM(AudioController.BGMType.ResultFailure);
        audioController?.StopRolling();
        // 割れた「瞬間」に短く鋭いヒットストップ+赤フラッシュ
        impactFxController?.PlayFailureImpact();
        // uiController.ShowFailureResult()はここでは呼ばない。GameManager.HandleGroundImpact()が
        // 直後に個別に呼ぶ設計(GameManager.cs実装より判明)。
    }

    /// <summary>
    /// 卵がスイートスポットに入っているかをPLAYING中に毎フレームGameManagerから渡す。
    /// 卵の輪郭を光らせて「今おいしい状態」であることを伝える。
    /// </summary>
    public void UpdateSweetSpotFeedback(bool isEggInside)
    {
        eggCrackGlowController?.SetSweetBoost(isEggInside ? sweetSpotEggGlow : 0f);
    }

    /// <summary>
    /// 画面がタップされたときにGameManagerから呼ぶ。ゲーム状態を問わず毎回鳴らす。
    /// SEクリップはAudioController.tapSe(Feedback.prefabで設定済み)を使う。
    /// </summary>
    public void PlayTapFeedback()
    {
        audioController?.PlaySE(AudioController.SEType.Tap);
    }

    /// <summary>
    /// 卵の転がり・跳ねの速さに応じた演出(A担当 EggController.GetSpeed()等から呼ぶ)。
    /// </summary>
    public void PlayEggMovementFeedback(float speed)
    {
        audioController?.PlayRolling(speed);
        effectController?.PlayRolling(speed);
    }

    /// <summary>
    /// リトライ時にGameManager.Retry()から呼ぶ。心音を止め、danger/progress/閾値追跡を初期化する。
    /// </summary>
    public void ResetFeedback()
    {
        heartbeatController?.StopHeartbeat();
        _currentDanger = 0f;
        _currentProgress = 0f;
        _lastCrackProgress = -1f;

        if (eggCrackGlowController != null)
        {
            eggCrackGlowController.SetDangerLevel(0f);
        }

        cameraController?.ResetCamera();
        hatchShowcaseController?.ResetShowcase();
        // 演出中にリトライされてもTime.timeScaleが落ちたまま残らないよう必ず戻す
        impactFxController?.ResetFx();
        audioController?.PlayBGM(AudioController.BGMType.Title);
        audioController?.StopRolling();
        uiController?.ShowTitle();
    }
}