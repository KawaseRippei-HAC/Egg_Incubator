using UnityEngine;

public class PanController : MonoBehaviour
{
    [Header("フライパンの操作設定")]
    [Tooltip("フライパンが傾く最大角度")]
    [SerializeField] private float maxAngle = 25.0f;

    [Tooltip("スマホ側の傾きをどのくらい反映するかの感度")]
    [SerializeField] private float sensitivity = 1.5f;

    [Tooltip("フライパンの動きの滑らかさ。数値が大きいと素早く、小さいとゆったり")]
    [SerializeField] private float smoothSpeed = 10.0f;

    [Header("フライパンの設定")]
    [Tooltip("フライパンの半径(中心から端までの距離)。この値で危険度の最大ラインが決まります")]
    public float panRadius = 0.4f;

    private Rigidbody rb;
    private Quaternion targetRot = Quaternion.identity;

    void Start()
    {
        // 端末のジャイロセンサーを有効化
        Input.gyro.enabled = true;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // スマホの加速度センサーを取得して、傾きを検知
        Vector3 accel = Input.acceleration;

        // スマホの縦の傾きと横の傾きを計算し、最大角度で制限します。
        float pitch = Mathf.Clamp(accel.y * sensitivity * maxAngle, -maxAngle, maxAngle);
        float roll = Mathf.Clamp(-accel.x * sensitivity * maxAngle, -maxAngle, maxAngle);

        // 目的となるフライパンの角度を作成
        targetRot = Quaternion.Euler(pitch, 0f, roll);
    }

    void FixedUpdate()
    {
        // 現在の角度から目的の角度へ、滑らかに回転させる
        // (Kinematic Rigidbodyの回転はRigidbody.MoveRotation()で適用しないと、
        //  物理エンジンが速度を正しく計算できず上に乗っている物体が異常に弾かれることがあるため)
        Quaternion nextRot = Quaternion.Slerp(rb.rotation, targetRot, Time.deltaTime * smoothSpeed);
        rb.MoveRotation(nextRot);
    }
    public float GetNormalizedDistanceFromCenter(Vector3 eggPosition)
    {
        // Y軸(高さ)の差を無視して、平面上の距離だけを測るためのベクトルを作成
        Vector3 offset = eggPosition - transform.position;
        offset.y = 0f;

        // 中心からの水平距離を計算
        float distance = offset.magnitude;

        // 半径で割って 0.0 〜 1.0 の範囲に丸める(Clamp01で1.0を超えないようにする)
        float normalizedDistance = Mathf.Clamp01(distance / panRadius);

        return normalizedDistance;
    }
    public void ResetPan()
    {
        // フライパンの傾きをリセットする処理
        transform.rotation = Quaternion.identity;
    }
}
