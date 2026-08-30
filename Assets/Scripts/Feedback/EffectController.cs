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
    [Tooltip("卵の転がり中に再生する補助エフェクトのプレハブ。未設定の間は何もしない(後から素材を割り当てられる)")]
    public GameObject rollingEffectPrefab;
    [Tooltip("rollingEffectPrefabの追従先(卵のTransform)。未設定の間は何もしない")]
    public Transform followTarget;
    [Tooltip("この速度(m/s)を超えている間だけ再生する")]
    public float rollingSpeedThreshold = 0.15f;

    [Header("スイートスポット演出 (追加要望: 触れると孵化が早まることを視覚的に示す)")]
    [Tooltip("卵がスイートスポット内にいるかどうかを持つコンポーネント(A/B担当のSweetSpotController)。" +
             "参照のみ行い、このスクリプト側からは一切変更しない")]
    public Gameplay.SweetSpotController sweetSpotController;
    [Tooltip("スイートスポットに触れている間再生するパーティクル(未設定の間は何もしない)")]
    public ParticleSystem sweetSpotBoostEffect;

    private GameObject _rollingEffectInstance;
    private bool _wasEggInSweetSpot;

    /// <summary>卵が転がっている間の砂煙・軌跡等(あれば)。FeedbackController.PlayEggMovementFeedback()から呼ばれる</summary>
    public void PlayRolling(float speed)
    {
        if (rollingEffectPrefab == null || followTarget == null) return;

        if (_rollingEffectInstance == null)
        {
            _rollingEffectInstance = Instantiate(rollingEffectPrefab, followTarget.position, Quaternion.identity, followTarget);
        }

        bool shouldPlay = speed > rollingSpeedThreshold;
        if (_rollingEffectInstance.activeSelf != shouldPlay)
        {
            _rollingEffectInstance.SetActive(shouldPlay);
        }
    }

    /// <summary>
    /// SweetSpotController.IsEggInside(卵がスイートスポットに触れている間trueになる、A/B担当実装)を
    /// 毎フレーム監視し、入った瞬間/出た瞬間だけPlay/Stopを呼ぶ。GameManager/SweetSpotController側の
    /// 改変は不要(C担当側だけで完結させる)。
    /// </summary>
    private void Update()
    {
        if (sweetSpotController == null || sweetSpotBoostEffect == null) return;

        bool isInside = sweetSpotController.IsEggInside;
        if (isInside == _wasEggInSweetSpot) return;

        if (isInside)
        {
            sweetSpotBoostEffect.transform.position = sweetSpotController.transform.position;
            sweetSpotBoostEffect.Play();
        }
        else
        {
            sweetSpotBoostEffect.Stop();
        }

        _wasEggInSweetSpot = isInside;
    }
}
