using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// NOTE: 同じFeedbackフォルダのCameraController/UIController等に合わせてグローバル名前空間に置く。
// (ScreenEffectControllerだけがnamespace Feedbackに入っている)
/// <summary>
/// 孵化(成功)と破壊(失敗)の「瞬間」を強調するための衝撃演出。
/// FeedbackController.PlaySuccessFeedback() / PlayFailureFeedback() から呼ばれる。
///
/// ★ScreenEffectControllerとの住み分け:
/// あちらはVignetteを危険度表示として常時制御している。ここで同じVignetteを触ると
/// 取り合いになるため、こちらはBloomとChromaticAberration(どちらもScreenEffect側が
/// 触らない)だけを一時的に持ち上げる。加えてヒットストップとフラッシュを担当する。
/// </summary>
public class ImpactFxController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("ポストプロセスのVolume。未設定ならAwakeでシーンから探す")]
    [SerializeField] private Volume _volume = null;

    [Tooltip("全画面フラッシュを出すUIController。未設定ならAwakeで探す")]
    [SerializeField] private UIController _uiController = null;

    [Header("ヒットストップ (時間を一瞬止めて衝撃を出す)")]
    [Tooltip("この倍率までTime.timeScaleを落とす。0.05なら20分の1の速さ")]
    [Range(0.01f, 1f)] public float hitStopScale = 0.05f;

    [Tooltip("失敗(破壊)のヒットストップ時間(実時間・秒)")]
    public float failureHitStop = 0.12f;

    [Tooltip("成功(孵化)のヒットストップ時間(実時間・秒)。成功は『溜め』なので少し長め")]
    public float successHitStop = 0.18f;

    [Tooltip("元の速度へ戻すのにかける時間(実時間・秒)。0で即座に戻る")]
    public float hitStopRecover = 0.25f;

    [Header("Bloomの瞬間的な増幅")]
    [Tooltip("この値まで一瞬Bloom強度を上げる")]
    public float bloomSpike = 3.5f;

    [Tooltip("Bloomが元に戻るまでの時間(実時間・秒)")]
    public float bloomDecay = 0.7f;

    [Header("画角のキック (一番『効く』要素)")]
    [Tooltip("衝撃の瞬間にカメラのFOVをこの角度だけ動かす。負で寄る(ズームイン)、正で引く。\n" +
             "位置や回転を揺らすより画面全体が殴られた感じが出る")]
    public float fovKick = -14f;

    [Tooltip("FOVが元へ戻るまでの時間(実時間・秒)")]
    public float fovKickRecover = 0.45f;

    [Header("危機一髪 (落ちそうになってから持ち直した瞬間)")]
    [Tooltip("スローにする倍率。決着の演出より控えめにする")]
    [Range(0.05f, 1f)] public float nearMissTimeScale = 0.35f;

    [Tooltip("スローが続く時間(実時間・秒)")]
    public float nearMissDuration = 0.12f;

    [Tooltip("元の速度へ戻すのにかける時間(実時間・秒)")]
    public float nearMissRecover = 0.3f;

    [Tooltip("危機一髪のFOVキック。決着より弱くする")]
    public float nearMissFovKick = -6f;

    [Header("色収差の瞬間的な増幅")]
    [Tooltip("この値まで一瞬色収差を上げる(0〜1)")]
    [Range(0f, 1f)] public float chromaticSpike = 0.85f;

    [Tooltip("色収差が元に戻るまでの時間(実時間・秒)")]
    public float chromaticDecay = 0.5f;

    private Bloom _bloom;
    private ChromaticAberration _chromatic;
    private float _bloomBase;
    private float _chromaticBase;
    private Coroutine _routine;
    private Camera _camera;
    private float _fovBase;

    private void Awake()
    {
        if (_volume == null) _volume = FindFirstObjectByType<Volume>();
        if (_uiController == null) _uiController = FindFirstObjectByType<UIController>();

        if (_volume != null)
        {
            // Volume.profileはsharedProfileを複製したインスタンスを返すため、
            // ここで書き換えてもアセットは汚れない(ScreenEffectControllerと同じ方針)。
            var profile = _volume.profile;
            profile.TryGet(out _bloom);
            profile.TryGet(out _chromatic);
        }

        if (_bloom != null) _bloomBase = _bloom.intensity.value;
        if (_chromatic != null) _chromaticBase = _chromatic.intensity.value;

        _camera = Camera.main;
        if (_camera != null) _fovBase = _camera.fieldOfView;
    }

    /// <summary>孵化した瞬間。閃光→Bloomの余韻で「生まれた」感を出す</summary>
    public void PlaySuccessImpact()
    {
        _uiController?.FlashSuccess();
        Restart(successHitStop);
    }

    /// <summary>卵が割れた瞬間。短く鋭いヒットストップで衝撃を出す</summary>
    public void PlayFailureImpact()
    {
        _uiController?.FlashDanger();
        Restart(failureHitStop);
    }

    /// <summary>
    /// 「危機一髪」。落ちそうになってから持ち直した瞬間に鳴らす。
    /// 成功/失敗より控えめ(閃光なし、短いスロー、弱いFOVキック)にして、
    /// 決着の演出と区別がつくようにする。
    /// </summary>
    public void PlayNearMiss()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(NearMissRoutine());
    }

    private IEnumerator NearMissRoutine()
    {
        float baseFixed = Time.fixedDeltaTime / Mathf.Max(Time.timeScale, 0.0001f);

        if (_camera != null) _camera.fieldOfView = _fovBase + nearMissFovKick;
        SetTimeScale(nearMissTimeScale, baseFixed);
        yield return new WaitForSecondsRealtime(nearMissDuration);

        float t = 0f;
        float recover = Mathf.Max(nearMissRecover, 0.0001f);
        while (t < recover)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / recover);
            SetTimeScale(Mathf.Lerp(nearMissTimeScale, 1f, p), baseFixed);
            if (_camera != null)
            {
                _camera.fieldOfView = Mathf.Lerp(_fovBase + nearMissFovKick, _fovBase, Mathf.SmoothStep(0f, 1f, p));
            }
            yield return null;
        }

        RestoreImmediate(baseFixed);
        _routine = null;
    }

    private void Restart(float hitStopDuration)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ImpactRoutine(hitStopDuration));
    }

    private IEnumerator ImpactRoutine(float hitStopDuration)
    {
        // --- ポストプロセスを一気に持ち上げる ---
        if (_bloom != null)
        {
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = bloomSpike;
        }
        if (_chromatic != null)
        {
            _chromatic.intensity.overrideState = true;
            _chromatic.intensity.value = chromaticSpike;
        }
        if (_camera != null)
        {
            // 基準はAwakeで取った素の値。連続発火しても現在値を基準にしないので蓄積しない。
            // FOVはCameraControllerが触らないため、ここが単独で持ってよい。
            _camera.fieldOfView = _fovBase + fovKick;
        }

        // --- ヒットストップ ---
        // Time.timeScaleを触るので、待機はすべて実時間(WaitForSecondsRealtime)で行う。
        // fixedDeltaTimeも合わせて縮めないと、スロー中の物理が階段状に見える。
        float baseFixed = Time.fixedDeltaTime / Mathf.Max(Time.timeScale, 0.0001f);
        if (hitStopDuration > 0f)
        {
            SetTimeScale(hitStopScale, baseFixed);
            yield return new WaitForSecondsRealtime(hitStopDuration);
        }

        // --- 元の速度・ポストプロセスへ戻す ---
        float t = 0f;
        float recover = Mathf.Max(hitStopRecover, 0.0001f);
        float decay = Mathf.Max(bloomDecay, chromaticDecay);
        float total = Mathf.Max(recover, Mathf.Max(decay, fovKickRecover));

        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            if (hitStopDuration > 0f)
            {
                SetTimeScale(Mathf.Lerp(hitStopScale, 1f, Mathf.Clamp01(t / recover)), baseFixed);
            }
            if (_bloom != null)
            {
                _bloom.intensity.value = Mathf.Lerp(bloomSpike, _bloomBase, Mathf.Clamp01(t / Mathf.Max(bloomDecay, 0.0001f)));
            }
            if (_chromatic != null)
            {
                _chromatic.intensity.value = Mathf.Lerp(chromaticSpike, _chromaticBase, Mathf.Clamp01(t / Mathf.Max(chromaticDecay, 0.0001f)));
            }
            if (_camera != null)
            {
                // 行きは一瞬、戻りは弾むように(SmoothStepでオーバーシュートせず落ち着かせる)
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(fovKickRecover, 0.0001f)));
                _camera.fieldOfView = Mathf.Lerp(_fovBase + fovKick, _fovBase, p);
            }
            yield return null;
        }

        RestoreImmediate(baseFixed);
        _routine = null;
    }

    private void SetTimeScale(float scale, float baseFixedDeltaTime)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDeltaTime * scale;
    }

    private void RestoreImmediate(float baseFixedDeltaTime)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
        if (_bloom != null) _bloom.intensity.value = _bloomBase;
        if (_chromatic != null) _chromatic.intensity.value = _chromaticBase;
        if (_camera != null) _camera.fieldOfView = _fovBase;
    }

    /// <summary>リトライ時にFeedbackController.ResetFeedback()から呼ぶ。</summary>
    public void ResetFx()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        // 演出中にリトライされてもtimeScaleが戻らない事故を防ぐ
        RestoreImmediate(0.02f);
    }

    // 再生停止やシーン遷移でtimeScaleが0.05のまま残ると、次の再生まで
    // エディタ全体がスローになってしまうため必ず戻す
    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}

