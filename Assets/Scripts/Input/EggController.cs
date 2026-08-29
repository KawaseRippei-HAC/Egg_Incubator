using UnityEngine;
using Proto.Core;

[RequireComponent(typeof(Rigidbody))]
public class EggController : MonoBehaviour
{
    public event System.Action OnEggLanded;
    
    private Rigidbody rb;
    private float forceTimer;
    private bool hasLanded = false;

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

    [Header("現在の難易度状況")]
    [Tooltip("ゲーム進行度に応じて変化するパラメータ")]
    public DifficultyParams difficulty;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

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
        if (!hasLanded) return;
        
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
        // 水平方向（XとZ）の速度だけを取り出す
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 水平方向の速度が上限を超えていたら制限
        if (horizontalVelocity.magnitude > difficulty.maxLinearSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * difficulty.maxLinearSpeed;
            
            // Y軸（落下や跳ねる速度）はそのまま維持して再設定する
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }

        // 回転速度(Angular Speed)が上限を超えていたら上限値に抑える
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
    public void DropEgg()
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        forceTimer = 0f;

        Debug.Log("[EggController] DropEggが実行されました！落下を開始します。");
    }

    public Vector3 GetPosition() { return transform.position; }
    public float GetSpeed() { return rb.linearVelocity.magnitude; }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[EggController] 何かにぶつかった！ 相手の名前: {collision.gameObject.name}, タグ: {collision.gameObject.tag}"); //
        if (!hasLanded && collision.gameObject.CompareTag("Pan"))
        {
            hasLanded = true;
            OnEggLanded?.Invoke(); // GameManagerの HandleEggLanded() が呼ばれる
            Debug.Log("[EggController] フライパンに着地したことを検知！");
        }
    }
    public void ResetEgg()
    {
        // 物理演算を一時停止（DropEggが呼ばれるまで空中で待機させる）
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 初期位置と回転に戻す
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // 内部状態のリセット
        hasLanded = false;
        forceTimer = 0f;

        Debug.Log("[EggController] タマゴの位置と状態をリセットしました。");
    }
}