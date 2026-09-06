using UnityEngine;

/// <summary>
/// ひび割れ発光シェーダー(Custom/EggCrackGlow)と、放射光条シェーダー(Custom/EggRadialBurst)の
/// 両方の"_CrackAmount"を、危険度(0〜1)に応じて同期制御するテスト用スクリプト。
/// 将来的な拡張として、心音テンポに同期した明滅(ハートビートパルス)にも対応する。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class EggCrackGlowController : MonoBehaviour
{
    [Header("危険度 (0=無傷 / 1=最大)")]
    [Range(0f, 1f)]
    [Tooltip("外部のゲームロジックから dangerLevel をセットして _CrackAmount に反映する")]
    public float dangerLevel = 0f;

    [Header("放射光条エフェクト (任意)")]
    [Tooltip("卵を包む球体(Egg_RadialGlowなど)のRendererを指定すると、亀裂発光と同じ危険度で光条も連動する")]
    public Renderer radialBurstRenderer;

    [Header("テスト再生設定")]
    [Tooltip("エディタ再生中に画面左上のテスト用パネル(スライダー等)を表示する。" +
             "実機ビルドには元から含まれない(#if UNITY_EDITOR)ため、これはエディタの見た目の問題のみ")]
    public bool showTestGui = false;
    [Tooltip("有効にすると自動的に0→1→0で危険度を往復させ、発光の見え方を確認できる")]
    public bool testAutoPlay = false;
    [Tooltip("往復にかかる秒数")]
    public float testCycleSeconds = 6f;

    [Header("心音テンポ同期 (将来拡張)")]
    [Tooltip("有効にすると危険度をベースに心拍(ドクン、ドクン)の明滅を重ねる")]
    public bool enableHeartbeatSync = false;
    [Tooltip("心拍テンポ (BPM)。危険度が上がるほど外部からこの値を上げる想定")]
    [Range(40f, 200f)]
    public float heartbeatBPM = 70f;
    [Tooltip("心拍パルスがどれだけ発光を強めるか (0=無効, 1=最大2倍まで明滅)")]
    [Range(0f, 1f)]
    public float heartbeatPulseStrength = 0.35f;

    private Material _crackMaterialInstance;
    private Material _radialMaterialInstance;
    private static readonly int CrackAmountId = Shader.PropertyToID("_CrackAmount");

    private void Awake()
    {
        // インスタンス化して他のオブジェクトに影響を与えないようにする
        var renderer = GetComponent<Renderer>();
        _crackMaterialInstance = renderer.material;

        if (radialBurstRenderer == null)
        {
            // 自動検出: 子オブジェクトに Egg_RadialGlow があればそれを使う
            var child = transform.Find("Egg_RadialGlow");
            if (child != null) radialBurstRenderer = child.GetComponent<Renderer>();
        }
        if (radialBurstRenderer != null)
        {
            _radialMaterialInstance = radialBurstRenderer.material;
        }
    }

    private void Update()
    {
        if (testAutoPlay)
        {
            // 0→1→0 を往復するテストモード(PingPong)
            float t = Mathf.PingPong(Time.time / (testCycleSeconds * 0.5f), 1f);
            dangerLevel = t;
        }

        float finalAmount = dangerLevel;

        if (enableHeartbeatSync && dangerLevel > 0f)
        {
            float pulse = HeartbeatPulse(Time.time, heartbeatBPM);
            // 心拍パルスぶんだけ発光を底上げする(危険度そのものは超えない範囲でブースト)
            finalAmount = Mathf.Clamp01(dangerLevel * (1f + pulse * heartbeatPulseStrength));
        }

        // ★不具合修正(2026/08/30): 以前はここでSetDangerLevel(finalAmount)を呼んでいたため、
        // 心拍パルスでブーストされた値がdangerLevelフィールドへ毎フレーム書き戻され、
        // 「ゲーム進行に応じて基準値(dangerLevel)が変化するはずなのに変わらない/暴走する」
        // 不具合の温床になっていた。Update()内の毎フレーム反映は材質だけを更新する
        // ApplyToMaterial()を直接呼び、dangerLevelフィールドを書き換えないようにする。
        ApplyToMaterial(finalAmount);
    }

    /// <summary>
    /// 外部(ゲームロジック)から危険度を設定する。亀裂発光と放射光条の両方に反映される。
    /// 例: eggCrackGlowController.SetDangerLevel(gameManager.CrackProgress);
    ///
    /// ★不具合修正(2026/08/30): 以前はシェーダーへ値を渡すだけでdangerLevelフィールド自体を
    /// 更新していなかったため、実際には亀裂発光は正しく進行していても、Inspector上の
    /// Danger Levelの表示が常に0のまま変化して見えないという不具合があった。
    /// 外部からの呼び出しはこのフィールドにも反映する。
    /// </summary>
    public void SetDangerLevel(float amount)
    {
        amount = Mathf.Clamp01(amount);
        dangerLevel = amount;
        ApplyToMaterial(amount);
    }

    /// <summary>
    /// スイートスポットに入っている間、卵の輪郭(フレネル)を光らせる強さ(0〜1)。
    /// SweetSpotController.IsEggInside を FeedbackController 経由で受け取る。
    /// 急に切り替わると目にうるさいので、呼び出し側ではなくここで補間する。
    /// </summary>
    public void SetSweetBoost(float target, float lerpSpeed = 6f)
    {
        _sweetBoost = Mathf.MoveTowards(_sweetBoost, Mathf.Clamp01(target), Time.deltaTime * lerpSpeed);

        if (_crackMaterialInstance != null && _crackMaterialInstance.HasProperty(SweetBoostId))
        {
            _crackMaterialInstance.SetFloat(SweetBoostId, _sweetBoost);
        }
    }

    private float _sweetBoost;
    private static readonly int SweetBoostId = Shader.PropertyToID("_SweetBoost");

    private void ApplyToMaterial(float amount)
    {
        amount = Mathf.Clamp01(amount);
        if (_crackMaterialInstance != null)
        {
            _crackMaterialInstance.SetFloat(CrackAmountId, amount);
        }
        if (_radialMaterialInstance != null)
        {
            _radialMaterialInstance.SetFloat(CrackAmountId, amount);
        }
    }

    /// <summary>
    /// 心拍波形(ドクン、ドクン)を0〜1で返す。
    /// 1拍の中に「ドクン(強い主パルス)」+「ドッ(弱い副パルス)」の2山を作り、
    /// 心臓の鼓動らしい非対称な明滅にする。
    /// </summary>
    private static float HeartbeatPulse(float time, float bpm)
    {
        float period = 60f / Mathf.Max(1f, bpm);
        float phase = (time % period) / period; // 0..1

        float mainBeat = Gaussian(phase, 0.08f, 0.05f);       // 主パルス(ドクン)
        float subBeat = Gaussian(phase, 0.22f, 0.04f) * 0.5f; // 副パルス(ドッ)

        return Mathf.Clamp01(mainBeat + subBeat);
    }

    private static float Gaussian(float x, float center, float width)
    {
        float d = (x - center) / width;
        return Mathf.Exp(-d * d);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showTestGui) return;

        GUI.Box(new Rect(10, 10, 260, 90), "Egg Crack Glow Test");
        GUI.Label(new Rect(20, 35, 240, 20), $"Danger Level: {dangerLevel:F2}");
        dangerLevel = GUI.HorizontalSlider(new Rect(20, 55, 220, 20), dangerLevel, 0f, 1f);
        if (!Mathf.Approximately(dangerLevel, 0f) || !Mathf.Approximately(dangerLevel, 1f))
        {
            testAutoPlay = GUI.Toggle(new Rect(20, 75, 220, 20), testAutoPlay, "Auto PingPong Test");
        }
    }
#endif
}
