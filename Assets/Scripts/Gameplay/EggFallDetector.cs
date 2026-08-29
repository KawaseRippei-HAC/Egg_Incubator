using UnityEngine;
using UnityEngine.Events;

public class EggFallDetector : MonoBehaviour
{
    [Tooltip("判定する対象のタグ")]
    public string targetTag = "Player";

    [Tooltip("タマゴが通過した時に呼ばれるイベント")]
    public UnityEvent onEggFall;

    // Is Trigger がオンのオブジェクトを通過した時に呼ばれる
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Debug.Log("[EggFallDetector] フライパンから落ちた！");
            onEggFall.Invoke();
        }
    }
}