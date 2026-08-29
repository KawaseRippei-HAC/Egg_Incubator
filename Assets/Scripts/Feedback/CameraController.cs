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
    private enum Mode { Normal, Shake, Following, Success }

    [Header("参照")]
    public Camera mainCamera;
    [Tooltip("成功演出でドリーインする際の注視点。未設定でも動作する(その場でズームのみ)")]
    public Transform eggTransform;

    [Header("危険シェイクの強さ (仕様書統合ガイド5.1節のCameraShake実装例を流用可)")]
    public float shakeMagnitude = 0.15f;

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
        }

        if (_impactShakeTimer > 0f)
        {
            _impactShakeTimer -= Time.deltaTime;
            float damper = Mathf.Clamp01(_impactShakeTimer / impactShakeDuration);
            t.position += Random.insideUnitSphere * (impactShakeMagnitude * damper);
        }
    }

    /// <summary>初期の固定アングルに戻す。ゲーム開始・リトライ時に呼ぶ</summary>
    public void PlayNormal()
    {
        _mode = Mode.Normal;
        _currentDanger = 0f;
        _fallTarget = null;
    }

    /// <summary>危険度が高い間、微小な揺れを加える。FeedbackControllerから毎フレーム呼んでもよい</summary>
    public void PlayDangerShake(float danger)
    {
        _currentDanger = Mathf.Clamp01(danger);

        if (_mode == Mode.Normal || _mode == Mode.Shake)
        {
            _mode = _currentDanger > 0.01f ? Mode.Shake : Mode.Normal;
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

    /// <summary>リトライ時にPlayNormal()へ戻す</summary>
    public void ResetCamera()
    {
        PlayNormal();
        if (mainCamera != null)
        {
            // リトライは即座に定位置へ戻す(Lerpで戻る途中の見た目を残さない)
            mainCamera.transform.localPosition = _initialLocalPosition;
            mainCamera.transform.localRotation = _initialLocalRotation;
        }
    }
}
