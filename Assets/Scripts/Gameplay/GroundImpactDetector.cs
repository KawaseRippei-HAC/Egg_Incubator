using UnityEngine;
using UnityEngine.Events;

public class GroundImpactDetector : MonoBehaviour
{
    public string targetTag = "Player";
    public event System.Action OnGroundImpact;

    // �����I�ɏՓ˂����u�ԂɌĂ΂��i�n�ʂŃ^�}�S���o�E���h���������E�~�߂����ꍇ�j
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Debug.Log("[GroundImpactDetector] �n�ʂɌ��˂����I");
            OnGroundImpact?.Invoke();
        }
    }
}