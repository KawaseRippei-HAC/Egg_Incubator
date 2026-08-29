using UnityEngine;
using Proto.Core;

/// <summary>
/// 卵がフライパンの縁を越えて落下した瞬間、および地面に衝突した瞬間を検知し、
/// GameManagerの状態遷移(HandleEggFall / HandleGroundImpact)を自動的に呼び出す。
/// Egg_Testと同じGameObjectにアタッチする想定(Rigidbody/Colliderは既存のものを流用)。
///
/// これまでGameManager.HandleEggFall()/HandleGroundImpact()はデバッグキー(F/G)でのみ
/// 手動発火していたが、実プレイでは以下の2条件で自動発火させる:
///   1. PLAYING中に、パン中心からの正規化距離が1.0を超えた瞬間 → HandleEggFall()
///   2. その後、Groundタグの付いたコライダーに接触した瞬間      → HandleGroundImpact()
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EggFallDetector : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("卵とパンの距離判定に使う。パンの実寸に合わせてPanController側のeffectiveRadiusを調整すること")]
    public PanController panController;

    [Tooltip("状態遷移の通知先")]
    public Gameplay.GameManager gameManager;

    [Header("落下判定")]
    [Tooltip("この正規化距離(GetNormalizedDistanceFromCenterの戻り値)を超えたら「パンから落ちた」とみなす")]
    public float fallThreshold = 1.0f;

    [Tooltip("地面衝突判定に使うタグ")]
    public string groundTag = "Ground";

    private bool _hasFallenThisRound;

    private void Update()
    {
        if (panController == null || gameManager == null) return;
        if (gameManager.CurrentState != GameState.PLAYING) return;
        if (_hasFallenThisRound) return;

        float distance = panController.GetNormalizedDistanceFromCenter(transform.position);
        if (distance >= fallThreshold)
        {
            _hasFallenThisRound = true;
            gameManager.HandleEggFall();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;
        if (gameManager.CurrentState != GameState.FALLING) return;
        if (!string.IsNullOrEmpty(groundTag) && !collision.gameObject.CompareTag(groundTag)) return;

        gameManager.HandleGroundImpact();
    }

    /// <summary>
    /// GameManager.Retry()から呼ぶことを想定。次のラウンドで再度「パンから落ちた」判定ができるようにする。
    /// </summary>
    public void ResetForTesting()
    {
        _hasFallenThisRound = false;
    }
}
