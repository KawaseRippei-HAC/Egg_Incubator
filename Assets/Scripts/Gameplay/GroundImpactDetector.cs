using UnityEngine;

/// <summary>
/// 落下した卵が地面(や台)に衝突した瞬間を検知して GameManager へ通知する。
/// 卵側ではなく「ぶつかられる側」のオブジェクトにアタッチする。
///
/// ★自己登録方式にしている理由:
/// 以前は GameManager 側が _groundImpactDetector という単一の参照フィールドで
/// 1つだけを購読していたため、地面判定を複数箇所に置いても1つしか繋がらず、
/// 繋がっていない場所へ落ちると FALLING のまま復帰できなかった。
/// (中継用の GroundImpactRelay を別途書く羽目になっていた)
/// ここで自分から GameManager を見つけて登録することで、シーンにいくつ置いても
/// 全部が機能する。GameManager 側の参照フィールドと Relay は不要になった。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GroundImpactDetector : MonoBehaviour
{
    [Tooltip("衝突を検知する相手のタグ。卵は Player タグ")]
    public string targetTag = "Player";

    [Tooltip("通知先。未設定なら OnEnable でシーン内から自動的に探す")]
    [SerializeField] private Gameplay.GameManager gameManager;

    /// <summary>衝突の瞬間に発火する。Inspector 以外から追加でフックしたい場合に使う</summary>
    public event System.Action OnGroundImpact;

    private void OnEnable()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<Gameplay.GameManager>();
        }

        if (gameManager != null)
        {
            OnGroundImpact += gameManager.HandleGroundImpact;
        }
        else
        {
            Debug.LogWarning($"[{nameof(GroundImpactDetector)}] GameManager が見つかりません。" +
                             "このオブジェクトに落ちても失敗判定が発生しません。", this);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            OnGroundImpact -= gameManager.HandleGroundImpact;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(targetTag)) return;

        Debug.Log($"[GroundImpactDetector] {name} に衝突を検知しました。", this);
        OnGroundImpact?.Invoke();
    }
}
