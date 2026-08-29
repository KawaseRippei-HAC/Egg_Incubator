using UnityEngine;
using Proto.Core;

[RequireComponent(typeof(Rigidbody))]
public class EggController : MonoBehaviour
{
    private Rigidbody rb;
    private float forceTimer;

    [Header("基本設定（タマゴの暴れ具合）")]
    [Tooltip("タマゴを水平に押し出す力の基本値。転がる『速さ』に直結します")]
    public float baseForce = 2.0f;

    [Tooltip("タマゴを回転させる力の基本値。転がる『勢い（ゴロゴロ感）』に直結します")]
    public float baseTorque = 1.0f;

    [Header("安全制御（理不尽な動きを防ぐリミッター）")]
    [Tooltip("タマゴの最大移動速度。これ以上速く転がりません")]
    public float maxLinearSpeed = 5.0f;

    [Tooltip("タマゴの最大回転速度。これ以上高速でスピンしません")]
    public float maxAngularSpeed = 10.0f;

    [Tooltip("1回のアクションで加わる力の最大値。一気に吹っ飛ぶのを防ぎます")]
    public float maxForceMagnitude = 8.0f;

    [Header("現在の難易度状況 (デバッグ確認用)")]
    [Tooltip("ゲーム進行度に応じて変化するパラメータ")]
    public DifficultyParams difficulty;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        //// テスト用の初期値（後でManagerからSetDifficultyで上書きされる想定）
        //difficulty = new DifficultyParams
        //{
        //    forceMultiplier = 1.0f,
        //    forceInterval = 0.5f,
        //    directionVariance = 0.3f
        //};
    }

    void FixedUpdate()
    {
        UpdateEgg();
        LimitVelocity(); // 毎フレーム、速度が上限を超えないように監視・制限する
    }

    private void UpdateEgg()
    {
        forceTimer += Time.fixedDeltaTime;
        // 指定した秒数（forceInterval）が経過したら、タマゴを暴れさせる
        if (forceTimer >= difficulty.forceInterval)
        {
            ApplyInternalForce();
            forceTimer = 0f;
        }
    }

    private void ApplyInternalForce()
    {
        // 1. 方向の決定
        // ランダムな円（X, Z軸のみ）の方向を取得して、水平方向に力を加えるようにします
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        Vector3 forceDir = new Vector3(randomCircle.x, 0f, randomCircle.y);

        // 2. 急激な方向転換の抑制
        // 現在タマゴが転がっている方向(linearVelocity)と、これから力を加える方向(forceDir)を比較します
        float dot = Vector3.Dot(rb.linearVelocity.normalized, forceDir);
        float safeMultiplier = 1.0f;

        // 現在と逆方向に力が働こうとしている場合
        if (dot < -0.3f)
        {
            // 急ブレーキや急なUターンを防ぐため、加える力を半分に減らします
            safeMultiplier = 0.5f;
        }

        // 3. 最終的な力の算出と上限の適用
        // 押し出す力
        Vector3 appliedForce = forceDir * (baseForce * difficulty.forceMultiplier * safeMultiplier);
        appliedForce = Vector3.ClampMagnitude(appliedForce, maxForceMagnitude); // 一定以上の力にならないよう制限

        // 回転させる力
        Vector3 appliedTorque = Random.onUnitSphere * (baseTorque * difficulty.forceMultiplier);

        // 4. 力の適用
        rb.AddForce(appliedForce, ForceMode.Impulse);
        rb.AddTorque(appliedTorque, ForceMode.Impulse);
    }

    private void LimitVelocity()
    {
        // 移動速度(Linear Speed)が上限を超えていたら、方向はそのままに速度だけ上限値に抑える
        if (rb.linearVelocity.magnitude > difficulty.maxLinearSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * difficulty.maxLinearSpeed;
        }

        // 回転速度(Angular Speed)が上限を超えていたら、同様に上限値に抑える
        if (rb.angularVelocity.magnitude > difficulty.maxAngularSpeed)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * difficulty.maxAngularSpeed;
        }
    }

    // --- 外部連携用メソッド ---
    // Managerスクリプトから、難易度を更新するために呼ばれる
    public void SetDifficulty(DifficultyParams newParams)
    {
        difficulty = newParams;
    }

    public Vector3 GetPosition() { return transform.position; }
    public float GetSpeed() { return rb.linearVelocity.magnitude; }
}