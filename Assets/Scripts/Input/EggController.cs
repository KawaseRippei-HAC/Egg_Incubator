using UnityEngine;
using Proto.Core;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class EggController : MonoBehaviour
{
    public event System.Action OnEggLanded;
    
    private Rigidbody rb;
    private float forceTimer;
    private bool hasLanded = false;

    // NOTE: 以前はAddForce/AddTorqueをForceMode.Impulseで呼んでいたため、
    // 加わる速度が卵の質量(0.06kg)に反比例していた。baseForce=2.0は
    // Δv = 2.0 / 0.06 = 33m/s に相当し、一番簡単な設定(forceMultiplier=1)でも
    // 速度上限(2m/s)を毎回一瞬で振り切っていた。その結果、難易度カーブの
    // forceMultiplier(1→5)が全域で飽和して意味を成していなかった。
    // ForceMode.VelocityChangeに変えて、以下の値がそのまま「1回で加わる速度」を
    // 表すようにしてある(質量を変えても壊れない)。
    [Header("基本設定（タマゴの暴れ具合）")]
    [Tooltip("1回の衝撃でタマゴに加わる水平方向の速度(m/s)。difficulty.forceMultiplier(1〜5)倍される。\n" +
             "フライパンの半径は0.4mなので、0.15なら中心から縁まで約2.7秒かかる勢い")]
    public float baseForce = 0.15f;

    [Tooltip("1回の衝撃でタマゴに加わる角速度(rad/s)。difficulty.forceMultiplier(1〜5)倍される。\n" +
             "転がる『勢い（ゴロゴロ感）』に直結します")]
    public float baseTorque = 1.5f;

    [Header("安全制御（理不尽な動きを防ぐリミッター）")]
    [Tooltip("1回の衝撃で加わる速度の上限(m/s)。一気に吹っ飛ぶのを防ぎます")]
    public float maxForceMagnitude = 1.0f;

    [Header("中央アシスト設定")]
    [Tooltip("フライパン中心付近にいるときの、中央へ引き寄せる方向の割合（0.0で完全ランダム、1.0で完全に中央へ向かう）")]
    [Range(0f, 1f)] public float centerAssistWeight = 0.3f;

    [Tooltip("フライパンの縁付近にいるときの引き寄せ割合。中心付近より強くしておくことで、" +
             "『落ちそうなときだけ強く引き戻される』挙動になる")]
    [Range(0f, 1f)] public float edgeCenterAssistWeight = 0.85f;

    [Tooltip("基準となるフライパンのTransform。未指定ならAwake()でシーン内のPanControllerを自動で探す")]
    public Transform panCenter;

    // 縁からの距離に応じてアシスト強度を変えるために、位置だけでなくPanController本体も持つ。
    // panCenterがInspectorで指定されていればそちらのTransformを優先する。
    private PanController _panController;

    // DifficultyManagerのprogress=0のときの値に合わせた初期難易度。
    // PLAYING中はGameManagerが毎フレームSetDifficulty()で上書きするため、
    // 実際に使われるのは着地直後〜最初のSetDifficulty()が走るまでの数フレームだけ。
    private static readonly DifficultyParams DefaultDifficulty = new DifficultyParams
    {
        forceMultiplier = 1.0f,
        forceInterval = 2.0f,
        directionVariance = 0.1f,
        maxLinearSpeed = 2.0f,
        maxAngularSpeed = 5.0f,
    };

    [Header("現在の難易度状況")]
    [Tooltip("ゲーム進行度に応じて変化するパラメータ。PLAYING中はGameManagerが毎フレーム上書きするため、" +
             "ここの値は着地直後の数フレームだけ使われる")]
    public DifficultyParams difficulty = DefaultDifficulty;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Awake()
    {
        // GameManagerがSetDifficulty()をStart()より前に呼ぶ可能性があるため、
        // Rigidbodyの取得はAwake()で済ませておく
        rb = GetComponent<Rigidbody>();

        // シーン/プレハブ側に0が焼き付いていた場合の保険。
        // maxLinearSpeedが0のままだとLimitVelocity()が卵の水平速度を
        // 毎FixedUpdateゼロにクランプし、着地の瞬間に卵が固まってしまう。
        if (difficulty.maxLinearSpeed <= 0f || difficulty.forceInterval <= 0f)
        {
            difficulty = DefaultDifficulty;
        }

        // 中央アシストの基準点を解決する。
        // panCenterが未設定だとVector3.zero(ワールド原点)へ引き寄せる実装だったが、
        // GameSceneのフライパンは原点ではなく(13.7, 3.8, -0.5)にあるため、
        // 「中央へ寄せる」どころか常に一方向へ押し続ける力になっていた。
        _panController = FindFirstObjectByType<PanController>();
        if (panCenter == null && _panController != null)
        {
            panCenter = _panController.transform;
        }
        if (panCenter == null)
        {
            Debug.LogWarning($"[{nameof(EggController)}] panCenterが未設定で、シーンにPanControllerも見つかりません。" +
                             "中央アシストはワールド原点を基準にしてしまいます。", this);
        }
    }

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
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
        Vector3 randomDir = new Vector3(randomCircle.x, 0f, randomCircle.y);

        // 中央アシストベクトルの計算
        Vector3 targetCenter = panCenter != null ? panCenter.position : Vector3.zero;
        // たまごの現在地から中央へ向かう方向
        Vector3 toCenterDir = new Vector3(targetCenter.x - transform.position.x, 0.0f, targetCenter.z - transform.position.z).normalized;

        // 中心にいるときは centerAssistWeight、縁にいるときは edgeCenterAssistWeight を使う。
        // 一定の割合で混ぜるだけだと、縁に寄って落ちそうなときも中心にいるときと同じ弱さしか
        // 働かず「吸いつき」を感じられないため、距離に応じて強度を上げる。
        float normalizedDistance = _panController != null
            ? _panController.GetNormalizedDistanceFromCenter(transform.position)
            : 0f;
        float assistWeight = Mathf.Lerp(centerAssistWeight, edgeCenterAssistWeight, normalizedDistance);

        // Lerpを使って、ランダム方向と中央方向を assistWeight の割合で混ぜ合わせる
        Vector3 forceDir = Vector3.Lerp(randomDir, toCenterDir, assistWeight).normalized;

        // 2. 急激な方向転換の抑制
        // 現在タマゴが転がっている方向(linearVelocity)と、これから力を加える方向(randomDir)を比較します
        float dot = Vector3.Dot(rb.linearVelocity.normalized, forceDir);
        float safeMultiplier = 1.0f;

        // 現在と逆方向に力が働こうとしている場合
        if (dot < -0.3f)
        {
            // 急ブレーキや急なUターンを防ぐため、加える力を半分に減らします。
            // ただし縁へ転がり出ている最中は forceDir がほぼ中央向き = 現在の進行方向と真逆になり、
            // 「一番引き戻したい瞬間に限って力が半減する」ことになる。
            // アシストが強い場面ほどこの減衰を効かせないようにする。
            safeMultiplier = Mathf.Lerp(0.5f, 1.0f, assistWeight);
        }

        // 3. 最終的な速度変化の算出と上限の適用
        // 押し出す分の速度
        Vector3 appliedForce = forceDir * (baseForce * difficulty.forceMultiplier * safeMultiplier);
        appliedForce = Vector3.ClampMagnitude(appliedForce, maxForceMagnitude); // 一気に吹っ飛ばないよう制限

        // 回転させる分の角速度
        Vector3 appliedTorque = Random.onUnitSphere * (baseTorque * difficulty.forceMultiplier);

        // 4. 適用。ForceMode.VelocityChangeなので質量・慣性モーメントに依存せず、
        //    上で計算した値がそのまま速度・角速度の増分になる。
        rb.AddForce(appliedForce, ForceMode.VelocityChange);
        rb.AddTorque(appliedTorque, ForceMode.VelocityChange);
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

        // LimitVelocity()はAddForce/AddTorqueの次のFixedUpdateで速度を制限するため、
        // 転がり摩擦によって同フレーム内で瞬間的に上限を超える速度が生じることがある。
        // Rigidbody.maxLinearVelocityで物理エンジン自体に同じ上限をかけ、その1フレームの
        // 速度超過(急発進に見える現象)自体を起きなくする。数値・挙動の意図は変えない。
        if (rb != null)
        {
            rb.maxLinearVelocity = newParams.maxLinearSpeed;
        }
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
        // 前のラウンド終盤の高い難易度が残らないよう、progress=0相当に戻す
        difficulty = DefaultDifficulty;

        Debug.Log("[EggController] タマゴの位置と状態をリセットしました。");
    }
}