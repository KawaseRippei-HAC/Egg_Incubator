using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 卵が台から落下し、地面などに衝突した瞬間に
/// 「割れていない卵メッシュ(shellWhole)」から「破片群(fracturedPieces)」へ切り替え、
/// 卵が持っていた速度・角速度を引き継いだ物理的な破壊表現を行う。
///
/// 仕様: 「落下破壊表現 実装仕様書」の Unity側の作業ステップ3 に対応。
/// 破片側の各メッシュには事前に MeshCollider(Convex) + Rigidbody(isKinematic=true) が
/// 付与されている前提(EggFragments プレハブ参照)。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EggBreakController : MonoBehaviour
{
    [Header("参照: 卵メッシュ / 破片群")]
    [Tooltip("割れていない卵のメッシュ本体(通常このオブジェクトの子)")]
    public GameObject shellWhole;

    [Tooltip("破片群の親オブジェクト(EggFragmentsなど)。子に破片ごとのMeshCollider+Rigidbodyが必要")]
    public GameObject fracturedPieces;

    [Header("破壊トリガー設定")]
    [Tooltip("有効にすると、groundTagと一致するオブジェクトに衝突した瞬間に自動でTriggerBreak()を呼ぶ。\n" +
             "実際のバランス崩壊ロジックなど外部から直接TriggerBreak()を呼ぶ設計にする場合はオフにする")]
    public bool autoTriggerOnCollision = true;

    [Tooltip("autoTriggerOnCollisionが有効な場合に反応する衝突相手のタグ")]
    public string groundTag = "Ground";

    [Header("爆発力設定")]
    [Tooltip("破片に加える爆発力の基本値(落下速度0のときの値)")]
    public float baseExplosionForce = 1.5f;

    [Tooltip("落下速度(m/s)に応じて爆発力を上乗せする倍率。速く落ちるほど激しく弾け、" +
             "受け入れ基準「落下の勢いの強弱によって破片の弾け方に差が出る」に対応する")]
    public float velocityForceMultiplier = 0.8f;

    [Tooltip("爆発力の上限。速すぎる落下でも破片が飛びすぎないようにする安全上限")]
    public float maxExplosionForce = 12f;

    [Tooltip("AddExplosionForceの有効半径")]
    public float explosionRadius = 1.2f;

    [Tooltip("爆発力のうち上方向へ振り分ける割合(0=水平方向のみ, 1=真上寄り)")]
    public float explosionUpwardsModifier = 0.3f;

    [Header("速度の上限(想定外の吹き飛び・めり込み防止)")]
    [Tooltip("破片の最終的な最大速度(m/s)。受け入れ基準の安全上限に対応")]
    public float maxFragmentSpeed = 8f;

    [Tooltip("破片の最終的な最大角速度(rad/s)")]
    public float maxFragmentAngularSpeed = 25f;

    [Header("破片の微振動対策")]
    [Tooltip("破片同士は閉じた状態でぴったり重なっているため、物理を有効にする直前にこの分だけ" +
             "放射方向へずらしてから弾く。めり込み解消の押し合いが延々と続く微振動を防ぐ" +
             "(EggHatchControllerのprePhysicsSeparationと同じ仕組み)")]
    public float prePhysicsSeparation = 0.02f;

    [Tooltip("破壊からこの秒数が経ったら破片の物理を止めて完全に静止させる。0以下で無効。\n" +
             "小さく軽い破片は接地したあともソルバーが揺らし続けて微振動が残るため、" +
             "弾ける演出が終わったタイミングで固めてしまう")]
    public float freezeFragmentsAfterSeconds = 2.5f;

    [Header("演出フック(任意・未設定でも動作する)")]
    [Tooltip("破壊の瞬間に再生するパーティクル(飛び散り・閃光など)")]
    public ParticleSystem breakParticles;

    [Tooltip("破壊音を鳴らすAudioSource")]
    public AudioSource breakAudioSource;

    [Tooltip("破壊音のSEクリップ")]
    public AudioClip breakSfx;

    [Tooltip("カメラシェイクやバイブレーションなど、追加の演出をInspectorから自由に登録できる汎用フック。\n" +
             "例: CameraShakeのShake()メソッドや、Handheld.Vibrate()を呼ぶラッパーメソッドを登録する")]
    public UnityEvent onBreak;

    [Header("テスト用")]
    [Tooltip("エディタ再生中に指定キーで手動テストできるようにする")]
    public bool enableTestKey = true;
    public KeyCode testKey = KeyCode.Space;

    private Rigidbody _eggRigidbody;
    private bool _hasBroken;
    private bool _eggWasKinematicBeforeBreak;
    private Coroutine _freezeRoutine;

    // 破片ごとの初期ローカル変換。ResetForTesting()は破片ルートしか戻していなかったため、
    // 一度割れたあとリトライすると、物理で散らばった位置のまま破片が再表示されてしまう。
    private Transform[] _fragmentTransforms;
    private Vector3[] _fragmentInitialLocalPositions;
    private Quaternion[] _fragmentInitialLocalRotations;

    /// <summary>
    /// shellWholeの見た目(Renderer)と当たり判定(Collider)だけを切り替える。
    /// shellWholeがfracturedPiecesの親を兼ねる構成でも、fracturedPiecesを巻き込まないよう
    /// GameObject.SetActiveではなくコンポーネント単位で制御する。
    /// </summary>
    private void SetShellVisible(bool visible)
    {
        if (shellWhole == null) return;

        // shellWhole自身が持つRenderer/Colliderのみを対象にする(子は対象外)
        foreach (var r in shellWhole.GetComponents<Renderer>())
        {
            r.enabled = visible;
        }
        foreach (var c in shellWhole.GetComponents<Collider>())
        {
            c.enabled = visible;
        }

        // Egg_RadialGlowのような、殻本体に付随する装飾用の子オブジェクトも一緒に切り替える
        var radialGlow = shellWhole.transform.Find("Egg_RadialGlow");
        if (radialGlow != null) radialGlow.gameObject.SetActive(visible);
    }

    private void Awake()
    {
        _eggRigidbody = GetComponent<Rigidbody>();

        if (shellWhole == null)
        {
            Debug.LogWarning($"[{nameof(EggBreakController)}] shellWhole が未設定です。Inspectorで割れていない卵メッシュを指定してください。", this);
        }

        if (fracturedPieces == null)
        {
            Debug.LogWarning($"[{nameof(EggBreakController)}] fracturedPieces が未設定です。Inspectorで破片群(EggFragmentsなど)を指定してください。", this);
        }
        else
        {
            // リトライ時に破片を元の「閉じた卵」の配置へ戻せるよう、初期状態を控えておく
            var bodies = fracturedPieces.GetComponentsInChildren<Rigidbody>(includeInactive: true);
            _fragmentTransforms = new Transform[bodies.Length];
            _fragmentInitialLocalPositions = new Vector3[bodies.Length];
            _fragmentInitialLocalRotations = new Quaternion[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                _fragmentTransforms[i] = bodies[i].transform;
                _fragmentInitialLocalPositions[i] = bodies[i].transform.localPosition;
                _fragmentInitialLocalRotations[i] = bodies[i].transform.localRotation;
            }

            // 待機状態: 起動時は必ず非表示にしておく
            fracturedPieces.SetActive(false);
        }
    }

    // private void Update()
    // {
    //     if (enableTestKey && !_hasBroken && Input.GetKeyDown(testKey))
    //     {
    //         TriggerBreak();
    //     }
    // }

    private void OnCollisionEnter(Collision collision)
    {
        if (!autoTriggerOnCollision || _hasBroken) return;
        if (!string.IsNullOrEmpty(groundTag) && !collision.gameObject.CompareTag(groundTag)) return;
        TriggerBreak();
    }

    /// <summary>
    /// 落下判定が成立した瞬間に呼び出す破壊トリガー。
    /// バランス崩壊ロジックなど外部スクリプトから直接呼んでもよい。
    /// 例: eggBreakController.TriggerBreak();
    /// 多重発火防止のため、一度呼ばれると以後は何もしない(ResetForTestingで解除可能)。
    /// </summary>
    public void TriggerBreak()
    {
        if (_hasBroken) return;

        if (shellWhole == null || fracturedPieces == null)
        {
            Debug.LogError($"[{nameof(EggBreakController)}] shellWhole / fracturedPieces が未設定のため破壊できません。", this);
            return;
        }

        _hasBroken = true;

        // 1. 卵の速度・角速度を退避
        Vector3 velocity = _eggRigidbody != null ? _eggRigidbody.linearVelocity : Vector3.zero;
        Vector3 angularVelocity = _eggRigidbody != null ? _eggRigidbody.angularVelocity : Vector3.zero;
        float impactSpeed = velocity.magnitude;

        // 2. 卵本体のRigidbodyを止める。
        // 破片は卵の子オブジェクトなので、親が動き続けると各破片のTransformが
        // 「自身の物理」と「親の移動」の両方から引っ張られて延々と震え続ける。
        // さらに割れたあとの卵はコライダーが無効なまま重力で落ち続け床を貫通するため、
        // 見えていない殻を凍結しておく(EggHatchController.TriggerHatch()と同じ扱い)。
        if (_eggRigidbody != null)
        {
            _eggWasKinematicBeforeBreak = _eggRigidbody.isKinematic;
            _eggRigidbody.linearVelocity = Vector3.zero;
            _eggRigidbody.angularVelocity = Vector3.zero;
            _eggRigidbody.isKinematic = true;
        }

        // 3. 同一フレームでメッシュを切り替える(座標のズレ・ワープを防ぐ)
        // shellWhole.SetActive(false);
        SetShellVisible(false);
        fracturedPieces.SetActive(true);

        // 4〜5. 各破片へ速度を引き継ぎ、落下の勢いに応じた爆発力を加える
        Vector3 impactPoint = fracturedPieces.transform.position;
        float explosionForce = Mathf.Min(baseExplosionForce + impactSpeed * velocityForceMultiplier, maxExplosionForce);

        var rigidbodies = fracturedPieces.GetComponentsInChildren<Rigidbody>(includeInactive: true);
        foreach (var rb in rigidbodies)
        {
            // 閉じた状態では破片同士のコライダーが重なっている。そのまま物理を有効にすると
            // 物理エンジンがめり込みを解消しようと押し合い続け、微振動が残ってしまうため、
            // 先に少しだけ放射方向へずらしておく。
            if (prePhysicsSeparation > 0f)
            {
                Vector3 outDir = rb.transform.position - impactPoint;
                outDir = outDir.sqrMagnitude > 0.0001f ? outDir.normalized : Random.onUnitSphere;
                rb.transform.position += outDir * prePhysicsSeparation;
            }

            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;
            rb.AddExplosionForce(explosionForce, impactPoint, explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);

            // 想定外の吹き飛び・めり込み防止のため、最終的な速度を上限でクランプ
            if (rb.linearVelocity.magnitude > maxFragmentSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxFragmentSpeed;
            }
            if (rb.angularVelocity.magnitude > maxFragmentAngularSpeed)
            {
                rb.angularVelocity = rb.angularVelocity.normalized * maxFragmentAngularSpeed;
            }
        }

        // 6. 弾ける演出が終わったら破片を固めて、接地後の微振動を完全に止める
        if (freezeFragmentsAfterSeconds > 0f)
        {
            if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
            _freezeRoutine = StartCoroutine(FreezeFragmentsAfterDelay());
        }

        // 7. 切り替えの瞬間をごまかす演出を同時発火する
        if (breakParticles != null)
        {
            breakParticles.transform.position = impactPoint;
            breakParticles.Play();
        }
        if (breakAudioSource != null && breakSfx != null)
        {
            breakAudioSource.PlayOneShot(breakSfx);
        }
        onBreak?.Invoke();
    }

    /// <summary>
    /// 弾ける演出が落ち着いたタイミングで破片の物理を止める。
    /// 小さく軽い破片は接地したあともソルバーが押し合いを続けて微振動が残るため、
    /// 見た目が変わらないところまで来たら固めてしまう(負荷の軽減にもなる)。
    /// </summary>
    private IEnumerator FreezeFragmentsAfterDelay()
    {
        yield return new WaitForSeconds(freezeFragmentsAfterSeconds);

        if (fracturedPieces != null)
        {
            foreach (var rb in fracturedPieces.GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        _freezeRoutine = null;
    }

    /// <summary>
    /// テスト・デバッグ用: 破壊状態をリセットして再テストできるようにする。
    /// 実プレイでは通常呼ばない想定(1回きりの破壊イベントのため)。
    /// </summary>
    public void ResetForTesting()
    {
        _hasBroken = false;

        // 固める前にリトライされた場合に備えてコルーチンを止める
        if (_freezeRoutine != null)
        {
            StopCoroutine(_freezeRoutine);
            _freezeRoutine = null;
        }

        // if (shellWhole != null) shellWhole.SetActive(true);
        SetShellVisible(true);

        if (fracturedPieces != null)
        {
            fracturedPieces.SetActive(false);
            foreach (var rb in fracturedPieces.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // 破片ルートのローカル変換を初期状態(卵と同じ原点)に戻す
            fracturedPieces.transform.localPosition = Vector3.zero;
            fracturedPieces.transform.localRotation = Quaternion.identity;
            fracturedPieces.transform.localScale = Vector3.one;

            // 破片ひとつひとつも「閉じた卵」の配置へ戻す。
            // ルートだけ戻しても、物理とprePhysicsSeparationで散らばった各破片は
            // そのままなので、リトライ後に散った状態で再表示されてしまう。
            if (_fragmentTransforms != null)
            {
                for (int i = 0; i < _fragmentTransforms.Length; i++)
                {
                    if (_fragmentTransforms[i] == null) continue;
                    _fragmentTransforms[i].localPosition = _fragmentInitialLocalPositions[i];
                    _fragmentTransforms[i].localRotation = _fragmentInitialLocalRotations[i];
                }
            }
        }

        if (_eggRigidbody != null)
        {
            _eggRigidbody.linearVelocity = Vector3.zero;
            _eggRigidbody.angularVelocity = Vector3.zero;
            // TriggerBreak()で凍結する前の状態に戻す。
            // (GameManager.Retry()はこのあと改めてisKinematic=trueにするため、
            //  実プレイではどちらでも整合するが、単体テスト時の挙動を元に戻しておく)
            _eggRigidbody.isKinematic = _eggWasKinematicBeforeBreak;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUI.Box(new Rect(10, 110, 300, 90), "Egg Break Test");
        GUI.Label(new Rect(20, 135, 280, 20), _hasBroken ? "状態: 破壊済み" : "状態: 待機中");
        if (!_hasBroken)
        {
            if (GUI.Button(new Rect(20, 155, 120, 25), "TriggerBreak")) TriggerBreak();
        }
        else
        {
            if (GUI.Button(new Rect(20, 155, 120, 25), "Reset")) ResetForTesting();
        }
        if (enableTestKey)
        {
            GUI.Label(new Rect(150, 158, 140, 20), $"[{testKey}]キーでもテスト可");
        }
    }
#endif
}
