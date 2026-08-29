using UnityEngine;

/// <summary>
/// カメラ制御(仕様書3.4節)。通常固定・危険時シェイク・失敗時追従・成功演出カメラ。
/// ステータス: 未実装(スタブ)。8/29午後に実装予定。
///
/// 実装時の注意(仕様書3.4節、重要):
/// - FollowFallingEgg(Transform egg) は「卵が台から落下したと確定した瞬間」
///   (EggBreakController.TriggerBreak()が呼ばれるより前、A/B担当の落下確定判定のタイミング)に
///   呼ぶこと。onBreak(=着地の瞬間)より前段のイベントなので、onBreakに登録するのは誤り。
///   onBreakには「衝撃の一押し(シェイク)」だけを登録するのが正しい設計(統合ガイド5章とのズレに注意)。
/// - PlaySuccessCamera() は FeedbackController.PlaySuccessFeedback() から直接呼ばれる
///   (onHatchへの重複登録はしない。FeedbackController.cs冒頭コメント参照)。
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("参照")]
    public Camera mainCamera;

    [Header("危険シェイクの強さ (仕様書統合ガイド5.1節のCameraShake実装例を流用可)")]
    public float shakeMagnitude = 0.15f;

    /// <summary>初期の固定アングルに戻す。ゲーム開始・リトライ時に呼ぶ</summary>
    public void PlayNormal()
    {
        // TODO(8/29午後実装)
    }

    /// <summary>危険度が高い間、微小な揺れを加える。FeedbackControllerから毎フレーム呼んでもよい</summary>
    public void PlayDangerShake(float danger)
    {
        // TODO(8/29午後実装)
    }

    /// <summary>落下確定タイミングで呼ぶ(着地=onBreakより前。呼び出し元はA/B担当のEggFallDetector相当)</summary>
    public void FollowFallingEgg(Transform egg)
    {
        // TODO(8/29午後実装)
    }

    /// <summary>孵化成功時の演出カメラ。FeedbackController.PlaySuccessFeedback()から呼ばれる</summary>
    public void PlaySuccessCamera()
    {
        // TODO(8/29午後実装)
    }

    /// <summary>リトライ時にPlayNormal()へ戻す</summary>
    public void ResetCamera()
    {
        // TODO(8/29午後実装)
        PlayNormal();
    }
}
