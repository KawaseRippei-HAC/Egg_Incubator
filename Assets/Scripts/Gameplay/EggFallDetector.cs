using UnityEngine;
using UnityEngine.Events;

public class EggFallDetector : MonoBehaviour
{
    [Tooltip("���肷��Ώۂ̃^�O")]
    public string targetTag = "Player";

    [Tooltip("�^�}�S���ʉ߂������ɌĂ΂��C�x���g")]
    public event System.Action OnEggFall;

    // Is Trigger ���I���̃I�u�W�F�N�g��ʉ߂������ɌĂ΂��
    private void OnTriggerEnter(Collider other)
    {
        // ▼ 追加：何かが触れた瞬間に絶対にログを出す
        Debug.Log($"[EggFallDetector] 何かがエリアに入りました！ 相手の名前: {other.gameObject.name}, タグ: {other.tag}, 検知したいタグ: {targetTag}");

        if (other.CompareTag(targetTag))
        {
            Debug.Log("[EggFallDetector] ターゲットの落下を検知しました！");
            OnEggFall?.Invoke();
        }
    }
}