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

    [Header("危険度判定 (2025/08/29追加)")]
    [Tooltip("卵がこの半径(ワールド単位)だけパン中心から離れると危険度1.0(=パンの外)とみなす。" +
             "パンの実寸に合わせて調整してください")]
    [SerializeField] private float effectiveRadius = 1.0f;

    void Start()
    {
        // 端末のジャイロセンサーを有効化
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // スマホの加速度センサーを取得して、傾きを検知
        Vector3 accel = Input.acceleration;

        // スマホの縦の傾きと横の傾きを計算し、最大角度で制限します。
        float pitch = Mathf.Clamp(accel.y * sensitivity * maxAngle, -maxAngle, maxAngle);
        float roll = Mathf.Clamp(-accel.x * sensitivity * maxAngle, -maxAngle, maxAngle);

        // 目的となるフライパンの角度を作成
        Quaternion targetRot = Quaternion.Euler(pitch, 0f, roll);

        // 現在の角度から目的の角度へ、滑らかに回転させる
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// 卵の水平位置(XZ平面)がパン中心からどれだけ離れているかを0〜1(以上)で返す。
    /// 1.0以上でパンの縁の外に出たとみなす。GameManager.Update()からdangerLevel算出に使う。
    /// EggFallDetectorから「パンを外れたか」の判定にも使う。
    /// </summary>
    public float GetNormalizedDistanceFromCenter(Vector3 worldPosition)
    {
        Vector3 toEgg = worldPosition - transform.position;
        toEgg.y = 0f; // 高さは無視し、水平距離だけで判定する
        if (effectiveRadius <= 0f) return 0f;
        return toEgg.magnitude / effectiveRadius;
    }
}
