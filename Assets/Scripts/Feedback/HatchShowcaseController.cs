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

    // NOTE: 孵化アニメーションはキャラを8.3倍(ワールド幅0.055→0.455)まで拡大する。
    // そのため光輪のサイズを絶対値で決めると必ずどこかで破綻する
    // (当初は固定値だったため、拡大後のキャラに対して光輪が1/10の点になっていた)。
    // キャラの実サイズに対する倍率で指定し、毎フレーム追従させる。
    [Tooltip("光輪の大きさを、キャラの幅の何倍にするか。2.5ならキャラの2.5倍の光輪")]
    public float burstScaleRelative = 2.5f;

    [Tooltip("キャラから見てカメラの反対側(奥)へ、キャラ半径の何倍だけ離すか")]
    public float burstDistanceRelative = 1.2f;

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

    [Tooltip("キャラの姿勢を毎フレーム「直立してカメラを向く」状態に固定する。\n" +
             "★これがOFFだと卵の転がった向きがそのまま残る。卵は60秒転がった末に孵化するため" +
             "Egg_Testの最終回転はランダムで、固定のオフセット補正では傾きが取り切れない。\n" +
             "ONのときはEggHatchのワールド回転を毎フレーム逆算するので、卵がどの向きで" +
             "止まっていてもキャラは必ず正面を向いて直立する")]
    public bool stabilizeCharacterRotation = true;

    [Tooltip("stabilizeCharacterRotationがONのとき、直立姿勢からさらに加える見た目の調整(度)。\n" +
             "少し斜に構えさせたい場合などに使う")]
    public Vector3 stabilizedExtraEuler = Vector3.zero;

    [Tooltip("向き補正にかける秒数。0で即座に適用する。stabilizeCharacterRotationがONのときは" +
             "この秒数をかけて現在の姿勢から直立姿勢へ寄せていく")]
    public float uprightCorrectionDuration = 0.4f;

    [Tooltip("向き補正をかける対象。未設定ならeggHatchController.hatchVisual(EggHatch)を使う")]
    public Transform uprightTarget;

    [Header("殻の破片の退場")]
    [Tooltip("見せ場に入るとき、殻の破片(HatchFragments)を縮めて消す。\n" +
             "破片1個のワールドサイズは約0.13で、キャラ全体(約0.014)の10倍近くある。" +
             "寄ったカメラでは破片が画面を覆ってキャラが見えなくなるため、既定で有効")]
    public bool hideFragmentsDuringShowcase = true;

    [Tooltip("破片が縮んで消えるまでの秒数。0で即座に消す")]
    public float fragmentsHideDuration = 0.35f;

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
    private bool _fragmentsHidden;

    // 姿勢固定の立ち上がり(0→1)
    private float _uprightBlend;

    // 光輪の立ち上がり(0→1)。実際のスケールはキャラのサイズ追従でLateUpdateが決める
    private float _burstGrow;
    // 光輪メッシュ(LightBurst_Mesh)のローカル幅。ワールド幅からlocalScaleを逆算するのに使う
    private float _burstMeshWidth = 1f;

    private void Awake()
    {
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
        if (eggHatchController == null) eggHatchController = FindFirstObjectByType<EggHatchController>();

        // 待機状態: 起動時は必ず消しておく
        if (burst != null)
        {
            var mf = burst.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                _burstMeshWidth = Mathf.Max(mf.sharedMesh.bounds.size.x, 0.0001f);
            }

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

        // 殻の破片を退場させる(キャラより10倍大きく、寄った画面を覆ってしまうため)
        if (hideFragmentsDuringShowcase)
        {
            StartCoroutine(HideFragments());
        }

        if (cameraController != null)
        {
            if (fillRatioOverride > 0f)
            {
                cameraController.showcaseFillRatio = fillRatioOverride;
            }
            // 大きさの計測はCameraController側が毎フレーム行う(キャラが拡大し続けるため)
            cameraController.PlayShowcaseCamera(target);
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

            // 立ち上がりの0→1だけをここで作る。実際の大きさはキャラのサイズ追従なので
            // LateUpdate側でburstScaleRelativeを掛けて毎フレーム決める。
            float t = 0f;
            while (t < burstGrowDuration)
            {
                t += Time.deltaTime;
                // 最後にゆっくり収束する形(SmoothStep)で、ポンと出て落ち着く感じにする
                _burstGrow = Mathf.SmoothStep(0f, 1f,
                    burstGrowDuration <= 0f ? 1f : Mathf.Clamp01(t / burstGrowDuration));
                yield return null;
            }
            _burstGrow = 1f;
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
        if (t == null) yield break;

        // 毎フレーム姿勢を作り直すモードでは、静的なオフセット補正は使わない。
        // 立ち上がりの重み(_uprightBlend)だけをここで0→1にして、
        // LateUpdate側が現在の姿勢から直立姿勢へ滑らかに寄せる。
        if (stabilizeCharacterRotation)
        {
            if (!_uprightApplied)
            {
                _uprightOriginalLocalRotation = t.localRotation;
                _uprightApplied = true;
            }

            float e = 0f;
            while (e < uprightCorrectionDuration)
            {
                e += Time.deltaTime;
                _uprightBlend = Mathf.SmoothStep(0f, 1f,
                    uprightCorrectionDuration <= 0f ? 1f : Mathf.Clamp01(e / uprightCorrectionDuration));
                yield return null;
            }
            _uprightBlend = 1f;
            yield break;
        }

        if (uprightCorrectionEuler == Vector3.zero) yield break;

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

    /// <summary>
    /// 殻の破片を縮めて消す。破片は物理で動いているため、先にRigidbodyを止めてから
    /// 親(HatchFragments)のスケールを落とす。個々の破片ではなく親だけを縮めるのは、
    /// EggHatchController.ResetForTesting()が親のlocalScaleをVector3.oneへ戻すため、
    /// リトライ時の復元がそれだけで済むから。
    /// </summary>
    private IEnumerator HideFragments()
    {
        GameObject fragments = eggHatchController != null ? eggHatchController.hatchFragments : null;
        if (fragments == null) yield break;

        // 物理を止める。止めずに親を縮めるとRigidbodyと取り合いになる
        foreach (var rb in fragments.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Transform ft = fragments.transform;
        Vector3 from = ft.localScale;

        if (fragmentsHideDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fragmentsHideDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / fragmentsHideDuration);
                ft.localScale = Vector3.Lerp(from, Vector3.zero, p);
                yield return null;
            }
        }

        ft.localScale = Vector3.zero;
        fragments.SetActive(false);
        _fragmentsHidden = true;
    }

    /// <summary>
    /// キャラの姿勢を「直立してカメラを向く」状態に固定する。
    ///
    /// 卵は60秒間フライパンの上を転がった末に孵化するため、Egg_Testの最終回転は
    /// ランダムになる。EggHatchに固定のオフセットを掛けるだけでは、その土台の傾きが
    /// そのまま残ってキャラが傾いてしまう(実際にそうなっていた)。
    /// ここでは望ましいキャラのワールド回転から逆算してEggHatchのワールド回転を毎フレーム決める。
    ///
    /// Characterのローカル回転はAnimatorが書き込むため、その後(LateUpdate)に実行すること。
    /// </summary>
    private void StabilizeRotation(Camera cam)
    {
        if (!stabilizeCharacterRotation || !_uprightApplied) return;
        if (cam == null) return;

        Transform hatchRoot = ResolveUprightTarget();
        Transform ch = character;
        if (hatchRoot == null || ch == null) return;

        // 望ましい姿勢: 直立(up=+Y)で、水平方向にカメラを向く
        Vector3 toCam = cam.transform.position - ch.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.000001f) toCam = -cam.transform.forward;
        Quaternion desiredChar = Quaternion.LookRotation(toCam.normalized, Vector3.up)
                                 * Quaternion.Euler(stabilizedExtraEuler);

        // desiredChar = hatchRoot.rotation * ch.localRotation を満たす hatchRoot.rotation を解く
        Quaternion needed = desiredChar * Quaternion.Inverse(ch.localRotation);

        // 立ち上がり中は現在の姿勢から滑らかに寄せる
        hatchRoot.rotation = _uprightBlend >= 1f
            ? needed
            : Quaternion.Slerp(hatchRoot.rotation, needed, _uprightBlend);
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
        Camera cam = cameraController != null ? cameraController.mainCamera : Camera.main;

        // 姿勢の固定はAnimatorが書き込んだ後(=LateUpdate)に行う必要がある
        StabilizeRotation(cam);

        if (!_burstActive || burst == null) return;

        Transform target = character;
        if (target == null) return;
        if (cam == null) return;

        // キャラは孵化アニメーションで8.3倍まで拡大し続けるため、
        // 光輪の大きさ・奥行きもその実サイズに毎フレーム追従させる。
        // (絶対値で決めていたときは、拡大後のキャラに対して光輪が1/10の点になっていた)
        float radius = cameraController != null
            ? cameraController.CurrentShowcaseRadius
            : Mathf.Max(target.lossyScale.x, 0.001f) * 0.5f;

        Vector3 camForward = cam.transform.forward;
        // キャラから見てカメラの奥側へずらす = キャラの「後ろ」に回る
        burst.position = target.position + camForward * (radius * burstDistanceRelative);

        // ワールド幅が「キャラ幅 × burstScaleRelative」になるlocalScaleを求める
        float targetWorldWidth = radius * 2f * burstScaleRelative;
        float scale = _burstMeshWidth > 0.0001f ? targetWorldWidth / _burstMeshWidth : targetWorldWidth;
        burst.localScale = Vector3.one * (scale * _burstGrow);

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
        _burstGrow = 0f;

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
        _uprightBlend = 0f;

        // 破片を表示状態へ戻す。スケールと親子関係と物理は
        // EggHatchController.ResetForTesting()が戻すので、ここではSetActiveだけ。
        // (GameManager.Retry()がResetForTesting()も呼ぶ)
        if (_fragmentsHidden && eggHatchController != null && eggHatchController.hatchFragments != null)
        {
            eggHatchController.hatchFragments.SetActive(true);
        }
        _fragmentsHidden = false;

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
