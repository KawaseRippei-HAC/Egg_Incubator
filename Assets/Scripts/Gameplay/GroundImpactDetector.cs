using UnityEngine;
using UnityEngine.Events;

public class GroundImpactDetector : MonoBehaviour
{
    public string targetTag = "Player";
    public UnityEvent onGroundImpact;

    // 物理的に衝突した瞬間に呼ばれる（地面でタマゴをバウンドさせたい・止めたい場合）
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Debug.Log("[GroundImpactDetector] 地面に激突した！");
            onGroundImpact.Invoke();
        }
    }
}