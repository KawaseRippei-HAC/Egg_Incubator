using System.Collections;
using UnityEngine;

/// <summary>
/// 孵化成功時の「見せ場」演出。EggHatchController.onHatch から Play() を呼ぶ想定。
///
/// ★設計方針: キャラクター本体のTransformには一切触らない。
/// 孵化キャラ(Egg_Test/EggHatch/Character)は EggHatch にアタッチされた Animator
/// (Assets/Animations/EggHatch.controller) にアニメーションされているため、
/// 位置・親子関係を書き換えるとアニメーションと取り合いになる。
/// そこで「キャラを画面中央へ動かす」のではなく、
///   (1) カメラ側でキャラを画面中央に収める (CameraController.PlayShowcaseCamera)
///   (2) キャラの背後に放射エフェクトを出す
/// の2点だけを行う。結果として見え方は「キャラが中央に来て背後が光る」になる。
/// </summary>
public class HatchShowcaseController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("孵化キャラ。未設定ならPlay()時にeggHatchController.characterから取得を試みる")]
    public Transform character;

    [Tooltip("見せ場カメラを実行するCameraController。未設定なら自動で探す")]
    public CameraController cameraController;

    [Tooltip("characterの自動取得元。未設定なら自動で探す")]
    public EggHatchController eggHatchController;

    [Header("背後の放射エフェクト")]
    [Tooltip("キャラの背後に出す放射エフェクトのルート。未設定ならカメラ寄せのみ行う。\n" +
             "Egg_RadialGlowと同じ LightBurst_Mesh + Egg_RadialBurst3D マテリアルを想定")]
    public Transform burst;

    [Tooltip("キャラから見てカメラの反対側(奥)へ、この距離だけ離して配置する")]
    public float burstDistanceBehind = 0.15f;

    [Tooltip("最終的な放射エフェクトのワールドスケール")]
    public float burstTargetScale = 0.5f;

    [Tooltip("0から上のスケールまで大きくなるのにかける秒数")]
    public float burstGrowDuration = 0.45f;

    [Tooltip("放射エフェクトが回転する速さ(度/秒)。0で回転しない")]
    public float burstSpinSpeed = 20f;

    [Tooltip("放射エフェクトの明るさ。Custom/EggRadialBurstシェーダーの_CrackAmountに入れる。\n" +
             "マテリアル(Egg_RadialBurst3D)はEgg_RadialGlowと共有のため、実行時に" +
             "インスタンス化して自分の分だけ書き換える")]
    [Range(0f, 1f)] public float burstIntensity = 1f;

    [Header("キャラの向き補正")]
    [Tooltip("孵化キャラが上を向いてしまうのを直すための補正回転(度)。\n" +
             "アニメーションクリップはCharacterのlocalPosition/Rotation/Scaleを全て動かすため" +
             "Character自体は触れないが、Animatorが乗っているEggHatch自身のTransformは" +
             "クリップに含まれていないので、そちらを回しても競合しない。\n" +
             "既定値(0,180,0)は、クリップ終端のCharacterローカル回転(270,0,0)と" +
             "Egg_Testのワールド回転(270,0,0)から逆算したもの。" +
             "これでキャラのワールド回転が(0,180,0)=直立してカメラを向く状態になる")]
    public Vector3 uprightCorrectionEuler = new Vector3(0f, 180f, 0f);

    [Tooltip("向き補正にかける秒数。0で即座に適用する")]
    public float uprightCorrectionDuration = 0.4f;

    [Tooltip("向き補正をかける対象。未設定ならeggHatchController.hatchVisual(EggHatch)を使う")]
    public Transform uprightTarget;

    [Header("カメラのニアクリップ")]
    [Tooltip("見せ場の間だけカメラのニアクリップをこの値に下げる。\n" +
             "孵化キャラはワールド半径が約0.007しかなく、画面に大きく写すには0.05程度まで" +
             "寄る必要がある。既定の0.3のままではモデルが手前でクリップされて消えてしまう。\n" +
             "0以下にすると変更しない")]
    public float showcaseNearClip = 0.01f;

    [Header("タイミング")]
    [Tooltip("onHatchが来てから見せ場に入るまでの待ち時間。殻が弾ける瞬間を見せてから寄りたい場合に使う")]
    public float delayBeforeShowcase = 0.2f;

    [Tooltip("キャラが画面の縦方向に占める割合。CameraController.showcaseFillRatioを上書きする(0以下なら上書きしない)")]
    [Range(0f, 1f)] public float fillRatioOverride = 0f;

    private Coroutine _routine;
    private float _burstSpin;
    private bool _burstActive;
    private Material _burstMaterialInstance;
    private static readonly int CrackAmountId = Shader.PropertyToID("_CrackAmount");

    // 見せ場を抜けるときに元へ戻すための退避
    private Quaternion _uprightOriginalLocalRotation;
    private bool _uprightApplied;
    private float _originalNearClip = -1f;
    private Camera _nearClipCamera;

    private void Awake()
    {
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        if (eggHatchController == null) eggHatchController = FindFirstObjectByType<EggHatchController>();

        // 待機状態: 起動時は必ず消しておく
        if (burst != null)
        {
            burst.localScale = Vector3.zero;
            burst.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 孵化演出の見せ場を開始する。EggHatchController.onHatch(UnityEvent)に登録して呼ぶ。
    /// </summary>
    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowcaseRoutine());
    }

    private IEnumerator ShowcaseRoutine()
    {
        if (delayBeforeShowcase > 0f)
        {
            yield return new WaitForSeconds(delayBeforeShowcase);
        }

        Transform target = ResolveCharacter();
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(HatchShowcaseController)}] 孵化キャラのTransformが見つかりません。" +
                             "characterを手動で割り当ててください。", this);
            yield break;
        }

        // キャラの見かけの大きさ(ワールド半径)をRendererのboundsから求める。
        // スケールが小さい(現状0.0156倍)ため決め打ちの距離では画角が合わない。
        float radius = CalculateWorldRadius(target);

        // キャラが極小なため、寄るとニアクリップ(既定0.3)に飲まれてモデルが消える。
        // 見せ場の間だけニアクリップを下げ、ResetShowcase()で元に戻す。
        Camera cam = cameraController != null ? cameraController.mainCamera : Camera.main;
        if (cam != null && showcaseNearClip > 0f && _originalNearClip < 0f)
        {
            _nearClipCamera = cam;
            _originalNearClip = cam.nearClipPlane;
            cam.nearClipPlane = showcaseNearClip;
        }

        // 向き補正を開始する(EggHatchを回す。Characterはアニメーションが持っているので触らない)
        StartCoroutine(ApplyUprightCorrection());

        if (cameraController != null)
        {
            if (fillRatioOverride > 0f)
            {
                cameraController.showcaseFillRatio = fillRatioOverride;
            }
            cameraController.PlayShowcaseCamera(target, radius);
        }

        if (burst != null)
        {
            burst.gameObject.SetActive(true);
            burst.localScale = Vector3.zero;
            _burstActive = true;

            // マテリアルはEgg_RadialGlowと共有アセットなので、そのまま書き換えると
            // 卵側の発光まで巻き添えになる(かつアセットが汚れる)。
            // rendererer.materialでインスタンス化してから_CrackAmountを立てる。
            if (_burstMaterialInstance == null)
            {
                var r = burst.GetComponent<Renderer>();
                if (r != null) _burstMaterialInstance = r.material;
            }
            if (_burstMaterialInstance != null && _burstMaterialInstance.HasProperty(CrackAmountId))
            {
                _burstMaterialInstance.SetFloat(CrackAmountId, burstIntensity);
            }

            float t = 0f;
            while (t < burstGrowDuration)
            {
                t += Time.deltaTime;
                float p = burstGrowDuration <= 0f ? 1f : Mathf.Clamp01(t / burstGrowDuration);
                // 最後にゆっくり収束する形(SmoothStep)で、ポンと出て落ち着く感じにする
                burst.localScale = Vector3.one * (burstTargetScale * Mathf.SmoothStep(0f, 1f, p));
                yield return null;
            }
            burst.localScale = Vector3.one * burstTargetScale;
        }

        _routine = null;
    }

    /// <summary>
    /// 孵化キャラの向きを補正する。回すのはEggHatch(Animatorが乗っているオブジェクト)で、
    /// アニメーションクリップはその子であるCharacterのローカル変換しか触っていないため競合しない。
    /// 破片(HatchFragments)はEggHatchController側でワールドへ切り離し済みなので引きずられない。
    /// </summary>
    private IEnumerator ApplyUprightCorrection()
    {
        Transform t = ResolveUprightTarget();
        if (t == null || uprightCorrectionEuler == Vector3.zero) yield break;

        if (!_uprightApplied)
        {
            _uprightOriginalLocalRotation = t.localRotation;
            _uprightApplied = true;
        }

        Quaternion from = t.localRotation;
        Quaternion to = _uprightOriginalLocalRotation * Quaternion.Euler(uprightCorrectionEuler);

        if (uprightCorrectionDuration <= 0f)
        {
            t.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < uprightCorrectionDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / uprightCorrectionDuration));
            t.localRotation = Quaternion.Slerp(from, to, p);
            yield return null;
        }
        t.localRotation = to;
    }

    private Transform ResolveUprightTarget()
    {
        if (uprightTarget != null) return uprightTarget;
        if (eggHatchController != null && eggHatchController.hatchVisual != null)
        {
            uprightTarget = eggHatchController.hatchVisual.transform;
        }
        return uprightTarget;
    }

    /// <summary>
    /// 放射エフェクトをキャラの背後に置き、カメラの方を向かせ続ける(ビルボード)。
    /// キャラはアニメーションで動くため、追従はLateUpdateで行う。
    /// </summary>
    private void LateUpdate()
    {
        if (!_burstActive || burst == null) return;

        Transform target = character;
        if (target == null) return;

        Camera cam = cameraController != null ? cameraController.mainCamera : Camera.main;
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        // キャラから見てカメラの奥側へずらす = キャラの「後ろ」に回る
        burst.position = target.position + camForward * burstDistanceBehind;
        // ビルボード: カメラと同じ向きにすることで常に正面から見える
        Quaternion facing = Quaternion.LookRotation(camForward, cam.transform.up);

        if (burstSpinSpeed != 0f)
        {
            _burstSpin += burstSpinSpeed * Time.deltaTime;
            facing *= Quaternion.AngleAxis(_burstSpin, Vector3.forward);
        }
        burst.rotation = facing;
    }

    /// <summary>リトライ時にGameManager/FeedbackController側から呼ぶ。エフェクトを消して待機状態へ戻す。</summary>
    public void ResetShowcase()
    {
        StopAllCoroutines();
        _routine = null;

        _burstActive = false;
        _burstSpin = 0f;

        // ニアクリップを元に戻す
        if (_nearClipCamera != null && _originalNearClip > 0f)
        {
            _nearClipCamera.nearClipPlane = _originalNearClip;
        }
        _originalNearClip = -1f;
        _nearClipCamera = null;

        // 向き補正を元に戻す
        Transform up = ResolveUprightTarget();
        if (_uprightApplied && up != null)
        {
            up.localRotation = _uprightOriginalLocalRotation;
        }
        _uprightApplied = false;

        if (burst != null)
        {
            burst.localScale = Vector3.zero;
            burst.gameObject.SetActive(false);
        }
    }

    private Transform ResolveCharacter()
    {
        if (character != null) return character;

        // EggHatchController.characterはAwake()でhatchVisualから自動取得される。
        // onHatchのタイミングでは解決済みなのでここで拾える。
        if (eggHatchController != null && eggHatchController.character != null)
        {
            character = eggHatchController.character.transform;
        }
        return character;
    }

    /// <summary>
    /// 対象とその子のRendererを合成したboundsから、見かけの大きさ(ワールド半径)を求める。
    /// Rendererが1つも無い場合はlossyScaleから概算する。
    /// </summary>
    private static float CalculateWorldRadius(Transform target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            // extentsのうち一番大きい辺を半径として採用する(見切れないように)
            return Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        }

        Vector3 s = target.lossyScale;
        return Mathf.Max(s.x, Mathf.Max(s.y, s.z)) * 0.5f;
    }
}
