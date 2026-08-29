using System.Collections;
using UnityEngine;

/// <summary>
/// 危険度(位置ベース、0〜1)に応じて心音AudioSourceのピッチ・再生間隔を変化させる。
/// 仕様書3.2節。入力はFeedbackController.UpdateDangerFeedback()経由のdangerのみで、
/// 孵化の進行度(progress)は使わない
/// (仕様書5章: 音・振動は「今の危険」に集中させ、時間経過の意味は視覚[殻の発光]側に寄せる方針)。
///
/// ノンバーバル表現表(0826_Memo)との対応:
///   安定       : 心音なし〜かなり弱い
///   危険・軽度 : ゆっくり聞こえ始める
///   危険・中度 : 速くなる
///   危険・最大 : かなり速くなる
/// </summary>
public class HeartbeatController : MonoBehaviour
{
    [Header("再生元")]
    [Tooltip("心音の単発クリップ(ドクン、ドクン)を鳴らすAudioSource。BGMとは別トラックにすること")]
    public AudioSource heartbeatSource;

    [Header("ピッチ (danger 0→1 で basePitch→maxPitch へ線形補間)")]
    public float basePitch = 1.0f;
    public float maxPitch = 1.8f;

    [Header("再生間隔・秒 (danger 0→1 で baseInterval→minInterval へ線形補間)")]
    public float baseInterval = 1.1f;
    public float minInterval = 0.35f;

    [Header("無音にするしきい値")]
    [Tooltip("dangerがこの値未満の間は再生しない(安定状態=心音なし〜かなり弱い、に対応)")]
    [Range(0f, 1f)]
    public float minDangerToPlay = 0.05f;

    private float _currentDanger;
    private Coroutine _routine;

    /// <summary>
    /// 心音ループを開始する。ゲーム開始時にFeedbackControllerまたはGameManagerから呼ぶ想定。
    /// (現状FeedbackControllerからは自動で呼んでいないため、GameStateがPlayingになった
    ///  タイミングで呼び出し元を決めてください。UIController.ShowPlaying()と対にするのが妥当)
    /// </summary>
    public void StartHeartbeat()
    {
        if (_routine != null) return;
        _routine = StartCoroutine(HeartbeatLoop());
    }

    /// <summary>
    /// 危険度を更新する。ピッチは即座に反映され、再生間隔は次のビートから反映される。
    /// </summary>
    public void UpdateHeartbeat(float danger)
    {
        _currentDanger = Mathf.Clamp01(danger);
        if (heartbeatSource != null)
        {
            heartbeatSource.pitch = Mathf.Lerp(basePitch, maxPitch, _currentDanger);
        }
    }

    /// <summary>停止する(成功/失敗確定時にFeedbackControllerから呼ばれる)。</summary>
    public void StopHeartbeat()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            if (heartbeatSource != null && heartbeatSource.clip != null && _currentDanger >= minDangerToPlay)
            {
                heartbeatSource.PlayOneShot(heartbeatSource.clip);
            }

            float interval = Mathf.Lerp(baseInterval, minInterval, _currentDanger);
            yield return new WaitForSeconds(interval);
        }
    }

    private void OnDisable()
    {
        StopHeartbeat();
    }
}
