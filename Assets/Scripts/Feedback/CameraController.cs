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
    private enum Mode { Normal, Shake, Following, Success, Frozen }

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
