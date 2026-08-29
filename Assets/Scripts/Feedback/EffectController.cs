using UnityEngine;

/// <summary>
/// 補助エフェクト(仕様書3.6節)。
/// ステータス: 未実装(スタブ)。
///
/// 重要: ヒビ・破損・孵化の主要VFXは既存アセット側(EggCrackGlowController/EggBreakController/
/// EggHatchControllerのシェーダー・パーティクル)が既に担っているため、このクラスでは
/// 重複実装しない(仕様書3.6節)。クラス図には UpdateCrack()/PlaySuccess()/PlayFailure()/
/// PlayHatch() 等も描かれているが、これらは意図的に実装対象から外している。
/// このクラスは「それ以外の補助エフェクト」のみを担当する範囲に縮小されている。
/// </summary>
public class EffectController : MonoBehaviour
{
    [Header("補助エフェクト (タップ演出などプラスアルファのみ)")]
    public GameObject rollingEffectPrefab;

    /// <summary>卵が転がっている間の砂煙・軌跡等(あれば)。FeedbackController.PlayEggMovementFeedback()から呼ばれる</summary>
    public void PlayRolling(float speed)
    {
        // TODO(必要になった場合のみ実装。仕様書3.6節: 主要VFXは既存アセット側が担当)
    }
}
