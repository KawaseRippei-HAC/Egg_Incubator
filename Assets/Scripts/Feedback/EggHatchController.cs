using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 制限時間を耐えきり「孵化(成功)」した瞬間に、
/// まず卵をその場に固定してごく短い予兆(揺れ+発光の増幅)を入れたあと、
/// 「割れていない卵メッシュ(shellWhole)」から「殻の破片群(hatchFragments)+中のキャラクター(character)」へ
/// 切り替え、EggBreakController(落下破壊)と同系統の物理的な弾け方で破片を飛ばす。
///
/// 仕様: 「孵化(成功)演出 実装仕様書」の Unity側の作業ステップ3〜4 に対応。
/// 破壊時との違いは、台の上で静かに孵化するイメージに合わせて弾ける勢いを大幅に弱めている点と、
/// 各破片にRigidbody+MeshCollider(Convex)が付いているため、重力で自然に回転しながら落下し、
/// 台(Groundタグの付いたコライダー)にきちんと乗って静止する点。
/// EggBreakController(落下破壊)と対になるスクリプトで、命名規則・設計方針を揃えている。
/// 両者は排他的な結末(割れる/孵化する)のため、同一フレームで同時に発火することは想定していない。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EggHatchController : MonoBehaviour
{
    [Header("参照: 卵メッシュ / 孵化後ビジュアル")]
    [Tooltip("割れていない卵のメッシュ本体(通常このオブジェクトの子、EggBreakControllerのshellWholeと同一のものを指定してよい)")]
    public GameObject shellWhole;

    [Tooltip("孵化後に表示するオブジェクト全体の親(FBXインポートで作られた\"EggHatch\")。\n" +
             "子に HatchFragments(破片群) と Character を持つ")]
    public GameObject hatchVisual;

    [Tooltip("hatchVisual内の破片群の親オブジェクト(HatchFragments)。未設定の場合hatchVisualから自動取得を試みる。\n" +
             "子の各破片にRigidbody+MeshCollider(Convex)が付いている必要がある(EggFragmentsと同じ構成)")]
    public GameObject hatchFragments;

    [Tooltip("hatchVisual内のキャラクターオブジェクト。未設定の場合hatchVisualから自動取得を試みる")]
    public GameObject character;

    [Tooltip("character(またはhatchVisual)にアタッチされているAnimator。未設定の場合hatchVisualから自動取得を試みる")]
    public Animator hatchAnimator;

    [Tooltip("Animator Controller側のTriggerパラメータ名")]
    public string hatchTriggerName = "Hatch";

    [Header("予兆演出(砕ける直前)")]
    [Tooltip("トリガーが呼ばれてから実際に殻が砕けるまでの予兆時間(秒)。この間、卵が小刻みに揺れ、ひび割れの発光が最大まで強まる")]
    public float anticipationDuration = 0.45f;

    [Tooltip("予兆中に卵が揺れる角度の最大値(度)")]
    public float shakeAngle = 4f;

    [Tooltip("予兆中の揺れの速さ(大きいほど細かく震える)")]
    public float shakeFrequency = 24f;

    [Tooltip("ひび割れ発光・放射光条を制御しているコンポーネント。未設定の場合shellWholeから自動取得を試みる。\n" +
             "予兆中、この値のdangerLevelを1まで引き上げて発光を強める(見つからなくても孵化自体は動作する)")]
    public EggCrackGlowController crackGlowController;

    [Header("破片の弾け方(EggBreakControllerよりかなり弱め、直接速度を指定する方式)")]
    [Tooltip("破片同士が閉じた状態でぴったり重なっているため、物理を有効にする直前にこの分だけ" +
             "放射方向へずらしてから弾く(めり込み解消による暴発を防ぐ)")]
    public float prePhysicsSeparation = 0.03f;

    [Tooltip("破片が放射方向へ飛び出す速さ(m/s)の最小値")]
    public float minPopSpeed = 0.5f;

    [Tooltip("破片が放射方向へ飛び出す速さ(m/s)の最大値")]
    public float maxPopSpeed = 1.1f;

    [Tooltip("上方向へのおまけの速度(m/s)。少しふわっと持ち上がる程度でよい")]
    public float upwardBoost = 0.4f;

    [Tooltip("弾けた瞬間に与える角速度(rad/s)の最大値。回転しながら落下する見た目のため")]
    public float maxSpinSpeed = 4f;

    [Header("演出フック(任意・未設定でも動作する)")]
    [Tooltip("殻が砕ける瞬間に再生するパーティクル(光の粒子・キラキラなど)")]
    public ParticleSystem hatchParticles;

    [Tooltip("孵化音を鳴らすAudioSource")]
    public AudioSource hatchAudioSource;

    [Tooltip("孵化音のSEクリップ")]
    public AudioClip hatchSfx;

    [Tooltip("カメラ演出や結果画面表示など、追加の演出をInspectorから自由に登録できる汎用フック(殻が砕けた瞬間に発火)")]
    public UnityEvent onHatch;

    [Header("テスト用")]
    [Tooltip("エディタ再生中に画面へテスト用パネル(TriggerHatchボタン等)を表示する。" +
             "実機ビルドには元から含まれない(#if UNITY_EDITOR)ため、これはエディタの見た目の問題のみ")]
    public bool showTestGui = false;
    [Tooltip("エディタ再生中に指定キーで手動テストできるようにする(新Input Systemプロジェクトのため既定では無効。EggBreakControllerと同じ方針でUpdateはコメントアウトし、OnGUIボタンのみ有効にしている)")]
    public bool enableTestKey = true;
    public KeyCode testKey = KeyCode.H;

    private Rigidbody _eggRigidbody;
    private bool _hasHatched;
    private bool _crackGlowAutoPlayOriginal;

    private void SetShellVisible(bool visible)
    {
        if (shellWhole == null) return;

        foreach (var r in shellWhole.GetComponents<Renderer>())
        {
            r.enabled = visible;
        }
        foreach (var c in shellWhole.GetComponents<Collider>())
        {
            c.enabled = visible;
        }

        var radialGlow = shellWhole.transform.Find("Egg_RadialGlow");
        if (radialGlow != null) radialGlow.gameObject.SetActive(visible);
    }

    private void Awake()
    {
        _eggRigidbody = GetComponent<Rigidbody>();

        if (shellWhole == null)
        {
            Debug.LogWarning($"[{nameof(EggHatchController)}] shellWhole が未設定です。Inspectorで割れていない卵メッシュを指定してください。", this);
        }
        else if (crackGlowController == null)
        {
            crackGlowController = shellWhole.GetComponent<EggCrackGlowController>();
        }

        if (hatchVisual == null)
        {
            Debug.LogWarning($"[{nameof(EggHatchController)}] hatchVisual が未設定です。Inspectorで孵化後ビジュアル(EggHatch)を指定してください。", this);
        }
        else
        {
            if (hatchFragments == null)
            {
                var t = hatchVisual.transform.Find("HatchFragments");
                if (t != null) hatchFragments = t.gameObject;
            }
            if (character == null)
            {
                var t = hatchVisual.transform.Find("Character");
                if (t != null) character = t.gameObject;
            }
            if (hatchAnimator == null)
            {
                hatchAnimator = hatchVisual.GetComponent<Animator>();
            }
            hatchVisual.SetActive(false);
        }
    }

    // private void Update()
    // {
    //     if (enableTestKey && !_hasHatched && Input.GetKeyDown(testKey))
    //     {
    //         TriggerHatch();
    //     }
    // }

    public void TriggerHatch()
    {
        if (_hasHatched) return;

        if (shellWhole == null || hatchVisual == null || hatchFragments == null)
        {
            Debug.LogError($"[{nameof(EggHatchController)}] shellWhole / hatchVisual / hatchFragments が未設定のため孵化できません。", this);
            return;
        }

        _hasHatched = true;

        if (_eggRigidbody != null)
        {
            _eggRigidbody.linearVelocity = Vector3.zero;
            _eggRigidbody.angularVelocity = Vector3.zero;
            _eggRigidbody.isKinematic = true;
        }

        StartCoroutine(HatchRoutine());
    }

    private IEnumerator HatchRoutine()
    {
        Quaternion baseRotation = transform.rotation;

        if (crackGlowController != null)
        {
            _crackGlowAutoPlayOriginal = crackGlowController.testAutoPlay;
            crackGlowController.testAutoPlay = false;
        }

        float t = 0f;
        while (t < anticipationDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / anticipationDuration);

            float currentShake = shakeAngle * progress;
            float wobble = Mathf.Sin(t * shakeFrequency);
            transform.rotation = baseRotation * Quaternion.Euler(
                wobble * currentShake,
                Mathf.Sin(t * shakeFrequency * 0.7f) * currentShake * 0.6f,
                Mathf.Cos(t * shakeFrequency * 1.3f) * currentShake * 0.4f);

            if (crackGlowController != null)
            {
                crackGlowController.dangerLevel = Mathf.Lerp(crackGlowController.dangerLevel, 1f, progress);
                crackGlowController.SetDangerLevel(crackGlowController.dangerLevel);
            }

            yield return null;
        }

        transform.rotation = baseRotation;
        if (crackGlowController != null)
        {
            crackGlowController.dangerLevel = 1f;
            crackGlowController.SetDangerLevel(1f);
        }

        // 2. 同一フレームでメッシュを切り替える(座標のズレ・ワープを防ぐ)
        SetShellVisible(false);
        hatchVisual.SetActive(true);

        // 3. 破片を弾く。閉じた状態でぴったり重なっているオブジェクト同士が
        //    物理演算を有効にした瞬間に「めり込み解消」で暴発しないよう、
        //    先に少しだけ放射方向へずらしてからRigidbodyを有効化し、
        //    AddExplosionForceではなく直接速度を指定して勢いを正確にコントロールする。
        Vector3 explosionCenter = hatchFragments.transform.position;
        foreach (Transform shell in hatchFragments.transform)
        {
            var rb = shell.GetComponent<Rigidbody>();
            if (rb == null) continue;

            Vector3 outDir = (shell.position - explosionCenter);
            outDir = outDir.sqrMagnitude > 0.0001f ? outDir.normalized : Random.onUnitSphere;

            shell.position += outDir * prePhysicsSeparation;

            rb.isKinematic = false;
            float speed = Random.Range(minPopSpeed, maxPopSpeed);
            rb.linearVelocity = outDir * speed + Vector3.up * upwardBoost;
            rb.angularVelocity = Random.insideUnitSphere * maxSpinSpeed;
        }

        // 4. キャラクターのアニメーションを再生する(こちらはキーフレームアニメーションのまま)
        if (hatchAnimator != null)
        {
            hatchAnimator.SetTrigger(hatchTriggerName);
        }
        else
        {
            Debug.LogWarning($"[{nameof(EggHatchController)}] hatchAnimator が見つかりません。hatchVisualにAnimatorがアタッチされているか確認してください。", this);
        }

        // 5. 切り替えの瞬間をごまかす演出を同時発火する
        if (hatchParticles != null)
        {
            hatchParticles.transform.position = explosionCenter;
            hatchParticles.Play();
        }
        if (hatchAudioSource != null && hatchSfx != null)
        {
            hatchAudioSource.PlayOneShot(hatchSfx);
        }
        onHatch?.Invoke();
    }

    /// <summary>
    /// テスト・デバッグ用: 孵化状態をリセットして再テストできるようにする。
    /// 実プレイでは通常呼ばない想定(1回きりの成功イベントのため)。
    /// </summary>
    public void ResetForTesting()
    {
        StopAllCoroutines();
        _hasHatched = false;

        SetShellVisible(true);

        if (crackGlowController != null)
        {
            crackGlowController.testAutoPlay = _crackGlowAutoPlayOriginal;
            crackGlowController.dangerLevel = 0f;
        }

        if (hatchVisual != null)
        {
            hatchVisual.SetActive(false);
        }

        if (hatchFragments != null)
        {
            foreach (var rb in hatchFragments.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            hatchFragments.transform.localPosition = Vector3.zero;
            hatchFragments.transform.localRotation = Quaternion.identity;
            hatchFragments.transform.localScale = Vector3.one;
        }

        if (_eggRigidbody != null)
        {
            _eggRigidbody.isKinematic = false;
            _eggRigidbody.linearVelocity = Vector3.zero;
            _eggRigidbody.angularVelocity = Vector3.zero;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showTestGui) return;

        GUI.Box(new Rect(10, 210, 300, 90), "Egg Hatch Test");
        GUI.Label(new Rect(20, 235, 280, 20), _hasHatched ? "状態: 孵化済み" : "状態: 待機中");
        if (!_hasHatched)
        {
            if (GUI.Button(new Rect(20, 255, 120, 25), "TriggerHatch")) TriggerHatch();
        }
        else
        {
            if (GUI.Button(new Rect(20, 255, 120, 25), "Reset")) ResetForTesting();
        }
    }
#endif
}
