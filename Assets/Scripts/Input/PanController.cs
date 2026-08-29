using UnityEngine;

public class PanController : MonoBehaviour
{
    [Header("�t���C�p���̑���ݒ�")]
    [Tooltip("�t���C�p�����X���ő�p�x")]
    [SerializeField] private float maxAngle = 25.0f;

    [Tooltip("�X�}�z���X�����Ƃ��̊��x")]
    [SerializeField] private float sensitivity = 1.5f;

    [Tooltip("�t���C�p���̓����̊��炩���B���l��������Ƃʂ���ƁA�グ��ƃL�r�L�r")]
    [SerializeField] private float smoothSpeed = 10.0f;
    
    [Header("フライパンの設定")]
    [Tooltip("フライパンの半径（中心から端までの距離）。この値で危険度の最大ラインが決まります")]
    public float panRadius = 0.4f;

    void Start()
    {
        // �[���̃W���C���Z���T�[��L����
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // �X�}�z�̉����x�Z���T�[���擾���āA�X�������m
        Vector3 accel = Input.acceleration;

        // �X�}�z�̏c�̌X���Ɖ��̌X�����v�Z���A�ő�p�x �Ő������܂��B
        float pitch = Mathf.Clamp(accel.y * sensitivity * maxAngle, -maxAngle, maxAngle);
        float roll = Mathf.Clamp(-accel.x * sensitivity * maxAngle, -maxAngle, maxAngle);

        // �ړI�ƂȂ�t���C�p���̊p�x���쐬
        Quaternion targetRot = Quaternion.Euler(pitch, 0f, roll);

        // ���݂̊p�x����ړI�̊p�x�ցA���炩�ɉ�]������
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
    public float GetNormalizedDistanceFromCenter(Vector3 eggPosition)
    {
        // Y軸（高さ）の差を無視して、平面上の距離だけを測るためのベクトルを作成
        Vector3 offset = eggPosition - transform.position;
        offset.y = 0f; 

        // 中心からの水平距離を計算
        float distance = offset.magnitude;

        // 半径で割って 0.0 〜 1.0 の範囲に丸める（Clamp01で1.0を超えないようにする）
        float normalizedDistance = Mathf.Clamp01(distance / panRadius);

        return normalizedDistance;
    }
    public void ResetPan()
    {
        // フライパンの傾きをリセットする処理
        transform.rotation = Quaternion.identity;
    }
}