using UnityEngine;

public class PanController : MonoBehaviour
{
    /// <summary>傾きをどのセンサーから読むか</summary>
    public enum TiltSource
    {
        /// <summary>ジャイロが使えるならジャイロ、無ければ加速度センサー</summary>
        Auto,
        Gyroscope,
        Accelerometer,
    }

    [Header("フライパンの操作設定")]
    [Tooltip("フライパンが傾く最大角度")]
    [SerializeField] private float maxAngle = 16.0f;

    [Tooltip("端末をこの角度だけ傾けると、フライパンが maxAngle まで倒れる。\n" +
             "小さいほど少ない手首の動きで大きく倒せる(＝高感度)")]
    [SerializeField] private float fullTiltAngle = 25.0f;

    [Tooltip("入力カーブ。1.0で線形。大きくすると中央付近の反応が緩やかになり、微調整しやすくなる")]
    [Range(1f, 3f)]
    [SerializeField] private float responseExponent = 1.3f;

    [Tooltip("この量(正規化入力0〜1)より小さい傾きは無視する。\n" +
             "手ブレでフライパンが常に微動し、卵が止まらなくなるのを防ぐ")]
    [Range(0f, 0.3f)]
    [SerializeField] private float deadZone = 0.05f;

    [Header("応答")]
    [Tooltip("センサー入力を平滑化する時定数(秒)。大きいほど滑らかだが遅れる")]
    [SerializeField] private float inputSmoothTime = 0.02f;

    [Tooltip("目標角度へ追従する速さ。大きいほど機敏")]
    [SerializeField] private float smoothSpeed = 45.0f;

    [Tooltip("フライパンの最大角速度(度/秒)。センサー値が飛んだときに\n" +
             "卵が弾き飛ばされるのを防ぐための保険")]
    [SerializeField] private float maxAngularSpeed = 300.0f;

    [Header("センサー")]
    [SerializeField] private TiltSource tiltSource = TiltSource.Auto;

    [Tooltip("実機で奥/手前が逆に感じる場合にチェックする")]
    [SerializeField] private bool invertPitch = false;

    [Tooltip("実機で左右が逆に感じる場合にチェックする")]
    [SerializeField] private bool invertRoll = false;

    [Header("フライパンの設定")]
    [Tooltip("フライパンの半径(中心から端までの距離)。この値で危険度の最大ラインが決まります")]
    public float panRadius = 0.4f;

    private Rigidbody rb;
    private Quaternion targetRot = Quaternion.identity;

    // 「水平」とみなす基準の重力方向(端末座標系・正規化済み)。CalibrateNeutral()で更新する
    private Vector3 neutralGravity = Vector3.zero;

    // 平滑化後の傾き入力(-1〜1の2次元)。x=右へ倒す, y=上端を奥へ倒す
    private Vector2 smoothedTilt = Vector2.zero;

    private bool _gyroAvailable;

    // Input.gyro.gravity の符号が端末によって加速度センサーと逆になる可能性への保険。
    // 両者は同じ物理量(重力)を同じ座標系で測っているので、内積が負なら符号が逆と判断できる。
    private float _gyroSign = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // ジャイロを有効化する。
        // ★Input.acceleration は「重力＋手の動き」の合計なので、端末を振ると
        //   傾けていないのに傾いたと誤検知する。Input.gyro.gravity は
        //   ジャイロと加速度をセンサーフュージョンした重力成分だけなので、
        //   手ブレや素早い持ち替えの影響を受けにくい。
        _gyroAvailable = SystemInfo.supportsGyroscope;
        if (_gyroAvailable)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 1f / 60f;
        }

        // 起動時の持ち方を基準にしておく。
        // 端末を完全に水平に持つ人はいないので、基準を取らないと最初から傾いた状態になる。
        CalibrateNeutral();
    }

    void Update()
    {
        Vector3 gravityDir = ReadGravityDirection();

        // Start()の時点ではまだセンサーが立ち上がっていないことがある
        // (ジャイロ有効化直後、Unity Remoteを後から接続した場合など)。
        // 基準が取れていない状態で値が来はじめたら、一度だけ取り直す。
        if (neutralGravity == Vector3.zero && gravityDir != Vector3.zero)
        {
            CalibrateNeutral();
            return;
        }

        // 基準姿勢からの傾きを求める(-1〜1に正規化済み)
        Vector2 rawTilt = ComputeNormalizedTilt(gravityDir);

        // 入力側で平滑化する。
        // ★出力(回転)側だけを強く鈍らせると、大きく倒したいときまで一律に遅れる。
        //   ノイズは高周波なので、入力側の軽い一次遅れ＋不感帯で落とすほうが
        //   同じ静粛性をより少ない遅延で得られる。
        float inputAlpha = inputSmoothTime > 0f
            ? 1f - Mathf.Exp(-Time.deltaTime / inputSmoothTime)
            : 1f;
        smoothedTilt = Vector2.Lerp(smoothedTilt, rawTilt, inputAlpha);

        Vector2 shaped = ApplyDeadZoneAndCurve(smoothedTilt);

        float pitch = (invertPitch ? -shaped.y : shaped.y) * maxAngle;
        float roll = (invertRoll ? shaped.x : -shaped.x) * maxAngle;

        targetRot = Quaternion.Euler(pitch, 0f, roll);
    }

    /// <summary>
    /// センサーから重力方向(端末座標系・正規化済み)を読む。取得できない場合はゼロを返す。
    /// </summary>
    private Vector3 ReadGravityDirection()
    {
        bool useGyro = tiltSource == TiltSource.Gyroscope
                       || (tiltSource == TiltSource.Auto && _gyroAvailable);

        Vector3 raw = Vector3.zero;
        if (useGyro)
        {
            raw = Input.gyro.gravity * _gyroSign;
            // 有効化直後はまだ値が入っていないことがあるので加速度センサーへ退避する
            if (raw.sqrMagnitude < 0.04f) raw = Input.acceleration;
        }
        else
        {
            raw = Input.acceleration;
        }

        return raw.sqrMagnitude < 1e-6f ? Vector3.zero : raw.normalized;
    }

    /// <summary>
    /// 基準姿勢からの傾きを、持ち方に依存しない形で「右へ倒した量／奥へ倒した量」に分解する。
    ///
    /// ★以前は accel.y と accel.x の差分をそのまま使っていたが、これは端末を
    ///   机に平置きした姿勢でしか正しくない。端末を立てて持つと、奥/手前の傾きは
    ///   accel.z に現れ accel.y はほとんど変化しない(cosθ ≒ 1 - θ²/2 の2次の微小量)ため、
    ///   立てて持つほど pitch が効かなくなっていた。
    ///   実測では「立てて持って20度倒す」場合、本来の 1/5.7 の入力しか得られていなかった。
    ///
    ///   ここでは重力ベクトルの「基準からのズレ」を、基準重力に垂直な平面へ射影して
    ///   画面の右方向・奥方向に分解する。大きさは sin(傾き角) となり、
    ///   どの持ち方でも1次で効く。
    /// </summary>
    private Vector2 ComputeNormalizedTilt(Vector3 g)
    {
        if (g == Vector3.zero || neutralGravity == Vector3.zero) return Vector2.zero;

        Vector3 n = neutralGravity;

        // 画面の右方向を、基準重力に垂直な平面へ落としたもの
        Vector3 right = Vector3.right - n * Vector3.Dot(Vector3.right, n);
        // 端末を真横に倒して持った場合はここが縮退するので、その姿勢では入力を切る
        if (right.sqrMagnitude < 1e-4f) return Vector2.zero;
        right.Normalize();

        // 右方向・基準重力の両方に直交する軸(画面の奥方向に対応)
        Vector3 fwd = Vector3.Cross(n, right);

        // 基準姿勢からの傾きベクトル。大きさ ≒ sin(傾き角)
        Vector3 perp = g - n * Vector3.Dot(g, n);

        float tiltRight = Vector3.Dot(perp, right);
        float tiltForward = -Vector3.Dot(perp, fwd);

        // fullTiltAngle 度の傾きで 1.0 になるよう正規化する
        float scale = Mathf.Sin(Mathf.Max(fullTiltAngle, 1f) * Mathf.Deg2Rad);
        return new Vector2(tiltRight, tiltForward) / scale;
    }

    /// <summary>
    /// 不感帯と応答カーブを適用する。
    /// 不感帯は「大きさ」に対してかけ、抜けた先を0から張り直すことで
    /// 不感帯の境界で入力が飛ぶのを防ぐ。
    /// </summary>
    private Vector2 ApplyDeadZoneAndCurve(Vector2 v)
    {
        float mag = v.magnitude;
        if (mag <= deadZone) return Vector2.zero;

        float t = Mathf.Clamp01((mag - deadZone) / Mathf.Max(1f - deadZone, 1e-4f));
        t = Mathf.Pow(t, responseExponent);

        return v * (t / mag);
    }

    /// <summary>
    /// 「今この持ち方」を水平の基準にする。UIのリセットボタンから呼ぶ。
    /// 押した瞬間の重力方向を覚え、以後はそこからの傾きを計算する。
    /// </summary>
    public void CalibrateNeutral()
    {
        // ジャイロと加速度センサーの符号が一致しているかをここで確認しておく。
        // 両者は同じ重力を同じ座標系で測っているので、内積は本来はっきり正になる。
        if (_gyroAvailable)
        {
            Vector3 gyroG = Input.gyro.gravity;
            Vector3 accel = Input.acceleration;
            if (gyroG.sqrMagnitude > 0.04f && accel.sqrMagnitude > 0.04f)
            {
                _gyroSign = Vector3.Dot(gyroG, accel) < 0f ? -1f : 1f;
            }
        }

        Vector3 g = ReadGravityDirection();

        // エディタでは加速度が常にゼロ。基準を取れないので入力そのものを無効化する
        // (ComputeNormalizedTilt が neutralGravity==zero でゼロを返す)。
        neutralGravity = g;

        // 基準を取り直した瞬間にフライパンが跳ねないよう、内部状態も水平へ戻す
        smoothedTilt = Vector2.zero;
        targetRot = Quaternion.identity;

        Debug.Log($"[PanController] 傾きの基準をリセットしました。" +
                  $"source={(tiltSource == TiltSource.Auto ? (_gyroAvailable ? "Gyro" : "Accel") : tiltSource.ToString())} " +
                  $"neutral={neutralGravity} gyroSign={_gyroSign} " +
                  $"accel={Input.acceleration} gyroGravity={(_gyroAvailable ? Input.gyro.gravity.ToString() : "n/a")}");
    }

    void FixedUpdate()
    {
        // 現在の角度から目的の角度へ、滑らかに回転させる
        // (Kinematic Rigidbodyの回転はRigidbody.MoveRotation()で適用しないと、
        //  物理エンジンが速度を正しく計算できず上に乗っている物体が異常に弾かれることがあるため)

        // ★以前は Slerp(a, b, Time.deltaTime * smoothSpeed) としていた。これは
        //   ステップ幅に比例した係数なので、Fixed Timestep を変えると手触りも変わる。
        //   指数形にしておけば smoothSpeed が「秒あたりの追従レート」という
        //   ステップ幅に依存しない意味になり、値の再調整が要らなくなる。
        //   (なお Time.deltaTime はスケール済み時間なので、ヒットストップ中は
        //    ゲーム全体と一緒にフライパンもスローになる。これは従来どおりで意図的)
        float alpha = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        Quaternion nextRot = Quaternion.Slerp(rb.rotation, targetRot, alpha);

        // 角速度の上限。センサーが飛んでも卵を弾き飛ばさないようにする
        if (maxAngularSpeed > 0f)
        {
            nextRot = Quaternion.RotateTowards(rb.rotation, nextRot, maxAngularSpeed * Time.deltaTime);
        }

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
        // ★transform.rotation に直接代入しても、次のFixedUpdateで targetRot へ
        //   引き戻されるため実質無効だった。目標角と内部状態ごと水平へ戻す。
        targetRot = Quaternion.identity;
        smoothedTilt = Vector2.zero;

        if (rb != null)
        {
            rb.rotation = Quaternion.identity;
        }
        transform.rotation = Quaternion.identity;
    }
}
