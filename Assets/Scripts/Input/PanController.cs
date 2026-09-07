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

    // 「水平」とみなす基準の加速度。CalibrateNeutral()で更新する
    private Vector3 neutralAccel = Vector3.zero;

    void Start()
    {
        // 端末のジャイロセンサーを有効化
        Input.gyro.enabled = true;
        rb = GetComponent<Rigidbody>();

        // 起動時の持ち方を基準にしておく。
        // 端末を完全に水平に持つ人はいないので、基準を取らないと最初から傾いた状態になる。
        CalibrateNeutral();
    }

    void Update()
    {
        // スマホの加速度センサーを取得して、傾きを検知
        Vector3 accel = Input.acceleration;

        // ★基準姿勢からの差分で傾きを求める。
        // 生の加速度をそのまま使うと「端末を完全に水平に持つ」ことが暗黙の前提になり、
        // 寝転がって遊ぶ・机に置く・角度をつけて持つといった姿勢では
        // 常にフライパンが傾いたままになってしまう。
        Vector3 delta = accel - neutralAccel;

        // スマホの縦の傾きと横の傾きを計算し、最大角度で制限します。
        float pitch = Mathf.Clamp(delta.y * sensitivity * maxAngle, -maxAngle, maxAngle);
        float roll = Mathf.Clamp(-delta.x * sensitivity * maxAngle, -maxAngle, maxAngle);

        // 目的となるフライパンの角度を作成
        targetRot = Quaternion.Euler(pitch, 0f, roll);
    }

    /// <summary>
    /// 「今この持ち方」を水平の基準にする。UIのリセットボタンから呼ぶ。
    /// 押した瞬間の加速度を覚え、以後はそこからの差分で傾きを計算する。
    /// </summary>
    public void CalibrateNeutral()
    {
        Vector3 accel = Input.acceleration;

        // エディタでは加速度が常にゼロ。そのまま基準にしても問題ないが、
        // 実機で値が取れていない場合の保険として、ほぼ無入力なら
        // 一般的な「やや手前に傾けて持つ」姿勢を仮の基準にする。
        if (accel.sqrMagnitude < 0.01f)
        {
            neutralAccel = Vector3.zero;
        }
        else
        {
            neutralAccel = accel;
        }

        // 基準を取り直した瞬間にフライパンが跳ねないよう、目標角も水平へ戻す
        targetRot = Quaternion.identity;

        Debug.Log($"[PanController] 傾きの基準をリセットしました。neutral={neutralAccel}");
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
