using UnityEngine;

public class PanController : MonoBehaviour
{
    [Header("フライパンの操作設定")]
    [Tooltip("フライパンが傾く最大角度")]
    [SerializeField] private float maxAngle = 25.0f;

    [Tooltip("スマホを傾けたときの感度")]
    [SerializeField] private float sensitivity = 1.5f;

    [Tooltip("フライパンの動きの滑らかさ。数値を下げるとぬるっと、上げるとキビキビ")]
    [SerializeField] private float smoothSpeed = 10.0f;

    void Start()
    {
        // 端末のジャイロセンサーを有効化
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // スマホの加速度センサーを取得して、傾きを検知
        Vector3 accel = Input.acceleration;

        // スマホの縦の傾きと横の傾きを計算し、最大角度 で制限します。
        float pitch = Mathf.Clamp(accel.y * sensitivity * maxAngle, -maxAngle, maxAngle);
        float roll = Mathf.Clamp(-accel.x * sensitivity * maxAngle, -maxAngle, maxAngle);

        // 目的となるフライパンの角度を作成
        Quaternion targetRot = Quaternion.Euler(pitch, 0f, roll);

        // 現在の角度から目的の角度へ、滑らかに回転させる
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}