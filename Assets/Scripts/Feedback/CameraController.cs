using UnityEngine;

/// <summary>
/// カメラ制御(仕様書3.4節)。通常固定・危険時シェイク・失敗時追従・成功演出カメラ。
/// Cinemachine等は使わず、mainCamera.transformを直接Lerp/Slerpで動かす自作の簡易ステートマシン。
///
/// 実装時の注意(仕様書3.4節、重要):
/// - FollowFallingEgg(Transform egg) は「卵が台から落下したと確定した瞬間」
///   (EggBreakController.TriggerBreak()が呼ばれるより前、A/B担当の落下確定判定のタイミング)に
///   呼ぶこと。onBreak(=着地の瞬間)より前段のイベントなので、onBreakに登録するのは誤り。
///   onBreakには「衝撃の一押し(シェイク)」だけを登録するのが正しい設計(統合ガイド5章とのズレに注意)。
/// - PlaySuccessCamera() は FeedbackController.PlaySuccessFeedback() から直接呼ばれる
///   (onHatchへの重複登録はしない。FeedbackController.cs冒頭コメント参照)。
/// </summary>
public class CameraController : MonoBehaviour
{
    private enum Mode { Normal, Shake, Following, Success, Frozen, Showcase }

    [Header("参照")]
    public Camera mainCamera;
    [Tooltip("成功演出でドリーインする際の注視点。未設定でも動作する(その場でズームのみ)")]
    public Transform eggTransform;

    [Header("危険シェイクの強さ (仕様書統合ガイド5.1節のCameraShake実装例を流用可)")]
    public float shakeMagnitude = 0.15f;
    [Tooltip("この危険度を超えたときだけ揺れ始める(「落ちそう」という臨界警告用。常時揺れると意味が薄れるため)")]
    public float dangerShakeThreshold = 0.6f;

    [Header("落下追従")]
    public float followSpeed = 4f;
    public Vector3 followOffset = new Vector3(0f, 1.5f, -2.5f);

    [Header("成功演出")]
    public float successZoomDistance = 1.2f;
    public float successTransitionSpeed = 2.5f;

    [Header("孵化の見せ場カメラ (HatchShowcaseControllerから使う)")]
    [Tooltip("見せ場でキャラクターが画面の縦方向に占める割合。0.45なら画面の高さの45%")]
    [Range(0.05f, 1f)] public float showcaseFillRatio = 0.45f;
    [Tooltip("見せ場カメラへ移行する速さ")]
    public float showcaseTransitionSpeed = 2.0f;
    [Tooltip("キャラを見下ろす角度(度)。0で真横から、大きいほど上から見下ろす")]
    [Range(0f, 60f)] public float showcasePitch = 12f;

    [Header("失敗着地の単発シェイク")]
    public float impactShakeMagnitude = 0.35f;
    public float impactShakeDuration = 0.3f;

    private Mode _mode = Mode.Normal;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private Transform _fallTarget;
    private float _currentDanger;
    private float _impactShakeTimer;

    // 前フレームで着弾シェイクとして加算したオフセット。
    // これを毎フレーム打ち消さずに t.position += ... を繰り返すとランダムウォークになり、
    // 特にFrozenモード(失敗時)は戻すLerpが無いためカメラが元の位置から漂流したまま固定される。
    private Vector3 _appliedImpactOffset = Vector3.zero;

    // 見せ場モードの注視対象と、その見かけの大きさ(ワールド半径)。
    // 孵化アニメーションはキャラを8.3倍(ワールド幅0.055→0.455)まで拡大するため、
    // 開始時に一度測るだけでは画角が全く合わない。毎フレーム測り直す。
    private Transform _showcaseTarget;
    private Renderer[] _showcaseRenderers;
    private float _showcaseRadius = 0.05f;

    /// <summary>見せ場で計測中の対象のワールド半径。背後エフェクトのサイズ合わせに使う</summary>
    public float CurrentShowcaseRadius => _showcaseRadius;

    private void Awake()
    {
        if (mainCamera != null)
        {
            _initialLocalPosition = mainCamera.transform.localPosition;
            _initialLocalRotation = mainCamera.transform.localRotation;
        }
    }

    private void Update()
    {
        if (mainCamera == null) return;

        var t = mainCamera.transform;

        // 各モードの位置計算に入る前に、前フレームの着弾シェイク分を打ち消して素の位置に戻す。
        // Following/Success/Frozenは現在位置を起点に動くため、これをやらないとオフセットが
        // 積み上がってカメラが漂流する。
        if (_appliedImpactOffset != Vector3.zero)
        {
            t.position -= _appliedImpactOffset;
            _appliedImpactOffset = Vector3.zero;
        }

        switch (_mode)
        {
            case Mode.Normal:
                t.localPosition = Vector3.Lerp(t.localPosition, _initialLocalPosition, Time.deltaTime * followSpeed);
                t.localRotation = Quaternion.Slerp(t.localRotation, _initialLocalRotation, Time.deltaTime * followSpeed);
                break;

            case Mode.Shake:
                Vector3 shakeOffset = Random.insideUnitSphere * (shakeMagnitude * _currentDanger);
                t.localPosition = _initialLocalPosition + shakeOffset;
                t.localRotation = Quaternion.Slerp(t.localRotation, _initialLocalRotation, Time.deltaTime * followSpeed);
                break;

            case Mode.Following:
                if (_fallTarget != null)
                {
                    Vector3 desired = _fallTarget.position + followOffset;
                    t.position = Vector3.Lerp(t.position, desired, Time.deltaTime * followSpeed);
                    Quaternion look = Quaternion.LookRotation(_fallTarget.position - t.position);
                    t.rotation = Quaternion.Slerp(t.rotation, look, Time.deltaTime * followSpeed);
                }
                break;

            case Mode.Success:
                if (eggTransform != null)
                {
                    // t.position(現在位置)から卵への方向を保ったまま、卵に少し寄る(ドリーイン)
                    Vector3 dirFromEgg = (t.position - eggTransform.position);
                    if (dirFromEgg.sqrMagnitude < 0.0001f) dirFromEgg = Vector3.back;
                    Vector3 desired = eggTransform.position + dirFromEgg.normalized * successZoomDistance;
                    t.position = Vector3.Lerp(t.position, desired, Time.deltaTime * successTransitionSpeed);
                    Quaternion look = Quaternion.LookRotation(eggTransform.position - t.position);
                    t.rotation = Quaternion.Slerp(t.rotation, look, Time.deltaTime * successTransitionSpeed);
                }
                break;

            case Mode.Showcase:
                if (_showcaseTarget != null)
                {
                    // 対象は孵化アニメーションで拡大し続けるので、毎フレーム測り直す
                    _showcaseRadius = CalculateWorldRadius(_showcaseRenderers, _showcaseTarget);

                    // キャラが画面の縦方向にshowcaseFillRatio分だけ占める距離を計算する。
                    // 半径rの対象を画面比fで写すのに必要な距離 d = r / (f/2 * tan(fov/2))
                    float halfFov = mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                    float denom = Mathf.Max(showcaseFillRatio * 0.5f * Mathf.Tan(halfFov), 0.0001f);
                    float dist = _showcaseRadius / denom;

                    // 孵化キャラはワールド半径が約0.007しかないため、必要距離は0.05程度になる。
                    // ニアクリップ(既定0.3)より手前だとモデルが丸ごとクリップされて消えるので、
                    // ニアクリップの外側に必ず収まるようクランプする。
                    // (HatchShowcaseController側で見せ場中のニアクリップ自体も下げている)
                    dist = Mathf.Max(dist, mainCamera.nearClipPlane * 1.5f + _showcaseRadius);

                    // 今カメラがいる側の水平方向を保ったまま、showcasePitch分だけ見下ろす位置へ回り込む
                    Vector3 flatDir = t.position - _showcaseTarget.position;
                    flatDir.y = 0f;
                    if (flatDir.sqrMagnitude < 0.0001f) flatDir = Vector3.back;
                    flatDir.Normalize();

                    float pitch = showcasePitch * Mathf.Deg2Rad;
                    Vector3 dir = (flatDir * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;
                    Vector3 desired = _showcaseTarget.position + dir * dist;

                    t.position = Vector3.Lerp(t.position, desired, Time.deltaTime * showcaseTransitionSpeed);
                    // 注視点を画面中央に置く。LookRotationなので対象は必ず画面中央に来る
                    Quaternion look = Quaternion.LookRotation(_showcaseTarget.position - t.position);
                    t.rotation = Quaternion.Slerp(t.rotation, look, Time.deltaTime * showcaseTransitionSpeed);
                }
                break;

            case Mode.Frozen:
                // 何もしない: 割れた瞬間の位置で完全に静止させる(卵が床を貫通して落下し続けても追従しない)
                break;
        }

        if (_impactShakeTimer > 0f)
        {
            _impactShakeTimer -= Time.deltaTime;
            float damper = Mathf.Clamp01(_impactShakeTimer / impactShakeDuration);
            // 次フレーム冒頭で打ち消せるよう、加えたオフセットを覚えておく
            _appliedImpactOffset = Random.insideUnitSphere * (impactShakeMagnitude * damper);
            t.position += _appliedImpactOffset;
        }
    }

    /// <summary>初期の固定アングルに戻す。ゲーム開始・リトライ時に呼ぶ</summary>
    public void PlayNormal()
    {
        _mode = Mode.Normal;
        _currentDanger = 0f;
        _fallTarget = null;
        _showcaseTarget = null;
        _showcaseRenderers = null;
    }

    /// <summary>危険度が閾値(dangerShakeThreshold)を超えている間だけ、微小な揺れを加える。
    /// 「落ちそう」という臨界警告用の演出であり、常時弱く揺らすものではない。
    /// FeedbackControllerから毎フレーム呼んでよい</summary>
    public void PlayDangerShake(float danger)
    {
        float clamped = Mathf.Clamp01(danger);
        float range = Mathf.Max(1f - dangerShakeThreshold, 0.0001f);
        _currentDanger = Mathf.Clamp01((clamped - dangerShakeThreshold) / range);

        if (_mode == Mode.Normal || _mode == Mode.Shake)
        {
            _mode = clamped >= dangerShakeThreshold ? Mode.Shake : Mode.Normal;
        }
    }

    /// <summary>落下確定タイミングで呼ぶ(着地=onBreakより前。呼び出し元はA/B担当のEggFallDetector相当)</summary>
    public void FollowFallingEgg(Transform egg)
    {
        _fallTarget = egg;
        _mode = Mode.Following;
    }

    /// <summary>孵化成功時の演出カメラ。FeedbackController.PlaySuccessFeedback()から呼ばれる</summary>
    public void PlaySuccessCamera()
    {
        _mode = Mode.Success;
    }

    /// <summary>
    /// 孵化の見せ場カメラ。対象を画面中央に置き、showcaseFillRatio分の大きさで写るまで寄る。
    /// HatchShowcaseControllerがEggHatchController.onHatchのタイミングで呼ぶ。
    /// PlaySuccessCamera()(卵に寄るだけ)を上書きして、孵化キャラを主役に切り替える役割。
    /// </summary>
    /// <param name="target">画面中央に置く対象(孵化キャラ)</param>
    public void PlayShowcaseCamera(Transform target)
    {
        if (target == null) return;

        _showcaseTarget = target;
        // 対象の大きさはアニメーションで変わるため、Rendererを控えて毎フレーム測り直す
        _showcaseRenderers = target.GetComponentsInChildren<Renderer>(true);
        _showcaseRadius = CalculateWorldRadius(_showcaseRenderers, target);
        _mode = Mode.Showcase;
    }

    /// <summary>
    /// Rendererのboundsを合成して、対象の見かけの大きさ(ワールド半径)を求める。
    /// Rendererが無い場合はlossyScaleから概算する。
    /// </summary>
    private static float CalculateWorldRadius(Renderer[] renderers, Transform fallback)
    {
        if (renderers != null && renderers.Length > 0)
        {
            bool started = false;
            Bounds b = default;
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled) continue;
                if (!started) { b = r.bounds; started = true; }
                else b.Encapsulate(r.bounds);
            }
            if (started)
            {
                return Mathf.Max(0.001f, Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)));
            }
        }

        if (fallback == null) return 0.05f;
        Vector3 s = fallback.lossyScale;
        return Mathf.Max(0.001f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)) * 0.5f);
    }

    /// <summary>落下失敗が着地した瞬間の単発の強いシェイク。FeedbackController.PlayFailureFeedback()から呼ばれる</summary>
    public void ImpactShake()
    {
        _impactShakeTimer = impactShakeDuration;
    }

    /// <summary>
    /// 卵が割れて(失敗確定して)、追従対象がもう存在しない体で扱ってよくなった瞬間に呼ぶ。
    /// 卵は割れた後もコライダーが無効化された状態で物理的に落下し続け、床を貫通して
    /// 落ち続けるため、Followingモードのままだとカメラが際限なく追いかけてしまう。
    /// その場でカメラを静止させ(Frozen)、以後は追従しない。
    /// FeedbackController.PlayFailureFeedback()から呼ばれる
    /// </summary>
    public void StopFollowing()
    {
        _fallTarget = null;
        _mode = Mode.Frozen;
    }

    /// <summary>リトライ時にPlayNormal()へ戻す</summary>
    public void ResetCamera()
    {
        PlayNormal();

        // 進行中の着弾シェイクを打ち切る。オフセットも捨てるので、
        // 次フレーム冒頭の打ち消し処理が定位置をずらすことはない。
        _impactShakeTimer = 0f;
        _appliedImpactOffset = Vector3.zero;

        if (mainCamera != null)
        {
            // リトライは即座に定位置へ戻す(Lerpで戻る途中の見た目を残さない)
            mainCamera.transform.localPosition = _initialLocalPosition;
            mainCamera.transform.localRotation = _initialLocalRotation;
        }
    }
}
