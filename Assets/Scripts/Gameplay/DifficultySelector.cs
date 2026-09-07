using Proto.Core;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Easy / Normal / Hard のプリセットを保持し、ゲーム開始時に各所へ流し込む。
    ///
    /// 差別化の軸は2つ:
    ///   1. 孵化までの時間  … 耐える長さそのもの
    ///   2. 卵の挙動        … 暴れる強さ・頻度・速度上限・中央アシストの効き
    ///
    /// ★フライパンの傾き(PanController.maxAngle)は難易度で変えていない。
    /// あれは「操作の反応猶予」を決める値で、下げすぎると操作が成立せず、
    /// 上げすぎると人間の反応速度を下回る。難易度ではなく操作性の値なので固定する。
    /// </summary>
    public class DifficultySelector : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("UIに出す表示名")]
            public string label = "NORMAL";

            [Header("孵化までの時間")]
            public float timeLimit = 60f;

            [Header("卵の暴れ (進行度0→1で min→max へ補間される)")]
            [Tooltip("1回の衝撃の強さの倍率")]
            public float minForceMultiplier = 1f;
            public float maxForceMultiplier = 5f;

            [Tooltip("衝撃の間隔(秒)。maxが序盤、minが終盤")]
            public float maxForceInterval = 1.4f;
            public float minForceInterval = 0.5f;

            [Tooltip("転がる水平速度の上限(m/s)")]
            public float minMaxLinearSpeed = 1f;
            public float maxMaxLinearSpeed = 2.5f;

            [Tooltip("回転速度の上限(rad/s)")]
            public float minMaxAngularSpeed = 5f;
            public float maxMaxAngularSpeed = 12f;

            [Header("中央アシスト (大きいほど中心へ引き戻される=易しい)")]
            [Range(0f, 1f)] public float centerAssistWeight = 0.5f;
            [Range(0f, 1f)] public float edgeCenterAssistWeight = 0.7f;

            // ★難易度の差別化はここが要。
            // 数値を盛るだけだと「同じゲームの数字違い」にしかならないので、
            // スイートスポットの性格そのものを難易度ごとに変える。
            // 易しいほど「素直なご褒美」、難しいほど「うまいが危険な罠」になる。
            [Header("スイートスポット")]
            [Tooltip("falseにすると出現しない。時間を早送りする手段が無い純粋な耐久になる")]
            public bool sweetSpotEnabled = true;

            [Tooltip("出現位置が変わる間隔(秒)。短いほど落ち着いて狙えない")]
            public float sweetSpotChangeInterval = 4f;

            [Tooltip("移動速度。速いほど追いかけにくい")]
            public float sweetSpotMoveSpeed = 1.5f;

            [Tooltip("出現半径。大きいほどフライパンの縁近くに出る")]
            public float sweetSpotSpawnRadius = 0.32f;

            [Tooltip("入った直後の加速倍率")]
            public float sweetSpotBaseMultiplier = 3f;

            [Tooltip("居続けたときの最大加速倍率")]
            public float sweetSpotMaxMultiplier = 5f;

            [Tooltip("滞在中に卵の力へ掛ける倍率。大きいほど『うまいが危険』になる")]
            public float sweetSpotForceWhileInside = 1.5f;
        }

        [Header("プリセット")]
        public Settings easy = new Settings
        {
            label = "EASY",
            timeLimit = 40f,
            minForceMultiplier = 0.8f, maxForceMultiplier = 3f,
            maxForceInterval = 2.0f, minForceInterval = 0.9f,
            minMaxLinearSpeed = 0.8f, maxMaxLinearSpeed = 1.8f,
            minMaxAngularSpeed = 4f, maxMaxAngularSpeed = 9f,
            centerAssistWeight = 0.7f, edgeCenterAssistWeight = 0.9f,
            // ゆっくり動いて長く留まる。素直なご褒美
            sweetSpotEnabled = true, sweetSpotChangeInterval = 5f, sweetSpotMoveSpeed = 1.0f,
            sweetSpotSpawnRadius = 0.28f,
            sweetSpotBaseMultiplier = 3f, sweetSpotMaxMultiplier = 5f,
            sweetSpotForceWhileInside = 1.2f,
        };

        public Settings normal = new Settings
        {
            label = "NORMAL",
            timeLimit = 60f,
            minForceMultiplier = 1f, maxForceMultiplier = 5f,
            maxForceInterval = 1.4f, minForceInterval = 0.5f,
            minMaxLinearSpeed = 1f, maxMaxLinearSpeed = 2.5f,
            minMaxAngularSpeed = 5f, maxMaxAngularSpeed = 12f,
            centerAssistWeight = 0.5f, edgeCenterAssistWeight = 0.7f,
            sweetSpotEnabled = true, sweetSpotChangeInterval = 4f, sweetSpotMoveSpeed = 1.5f,
            sweetSpotSpawnRadius = 0.32f,
            sweetSpotBaseMultiplier = 3f, sweetSpotMaxMultiplier = 5f,
            sweetSpotForceWhileInside = 1.5f,
        };

        public Settings hard = new Settings
        {
            label = "HARD",
            timeLimit = 90f,
            minForceMultiplier = 1.6f, maxForceMultiplier = 7f,
            maxForceInterval = 1.0f, minForceInterval = 0.35f,
            minMaxLinearSpeed = 1.3f, maxMaxLinearSpeed = 3.2f,
            minMaxAngularSpeed = 6f, maxMaxAngularSpeed = 16f,
            centerAssistWeight = 0.25f, edgeCenterAssistWeight = 0.5f,
            // 動きが速く縁寄り。取りに行く判断が要る
            sweetSpotEnabled = true, sweetSpotChangeInterval = 3f, sweetSpotMoveSpeed = 2.2f,
            sweetSpotSpawnRadius = 0.34f,
            sweetSpotBaseMultiplier = 3f, sweetSpotMaxMultiplier = 4.5f,
            sweetSpotForceWhileInside = 1.9f,
        };

        [Tooltip("Easy/Normal/Hardを全てクリアすると解放される最難関")]
        public Settings extra = new Settings
        {
            label = "EXTRA",
            timeLimit = 120f,
            minForceMultiplier = 2.4f, maxForceMultiplier = 9f,
            maxForceInterval = 0.8f, minForceInterval = 0.25f,
            minMaxLinearSpeed = 1.6f, maxMaxLinearSpeed = 4f,
            minMaxAngularSpeed = 8f, maxMaxAngularSpeed = 20f,
            centerAssistWeight = 0.1f, edgeCenterAssistWeight = 0.35f,
            // ★EXTRAだけ性格が反転する。
            // 常に動き回り、縁ギリギリに出て、見返りは小さいのに滞在中の危険は最大。
            // 「取りに行くほど損」に近いバランスで、HARDまでとは判断の質が変わる。
            sweetSpotEnabled = true, sweetSpotChangeInterval = 1.8f, sweetSpotMoveSpeed = 3.6f,
            sweetSpotSpawnRadius = 0.37f,
            sweetSpotBaseMultiplier = 2.2f, sweetSpotMaxMultiplier = 3.0f,
            sweetSpotForceWhileInside = 2.6f,
        };

        [Header("反映先 (未設定ならAwakeで自動取得)")]
        [SerializeField] private TimerManager _timerManager;
        [SerializeField] private DifficultyManager _difficultyManager;
        [SerializeField] private EggController _eggController;
        [SerializeField] private SweetSpotController _sweetSpotController;

        [Header("現在の選択")]
        [SerializeField] private GameDifficulty _current = GameDifficulty.Normal;

        /// <summary>選択が変わったときに通知する。UIのハイライト更新用</summary>
        public event System.Action<GameDifficulty> OnDifficultyChanged;

        public GameDifficulty Current => _current;
        public Settings CurrentSettings => GetSettings(_current);

        private const string PrefKey = "proto_d_difficulty";

        private void Awake()
        {
            if (_timerManager == null) _timerManager = FindFirstObjectByType<TimerManager>();
            if (_difficultyManager == null) _difficultyManager = FindFirstObjectByType<DifficultyManager>();
            if (_eggController == null) _eggController = FindFirstObjectByType<EggController>();
            // スイートスポットはPLAYING中しか有効でないので、非アクティブも含めて探す
            if (_sweetSpotController == null)
                _sweetSpotController = FindFirstObjectByType<SweetSpotController>(FindObjectsInactive.Include);

            // 前回選んだ難易度を覚えておく(展示で毎回選び直す手間を減らす)
            _current = (GameDifficulty)PlayerPrefs.GetInt(PrefKey, (int)GameDifficulty.Normal);
            // クリア状況を消した後などにExtraが選ばれたまま残らないようにする
            if (!IsUnlocked(_current)) _current = GameDifficulty.Normal;
            Apply();
        }

        public Settings GetSettings(GameDifficulty d)
        {
            switch (d)
            {
                case GameDifficulty.Easy: return easy;
                case GameDifficulty.Hard: return hard;
                case GameDifficulty.Extra: return extra;
                default: return normal;
            }
        }

        // UIボタンのonClickへ登録する(引数ありだとPersistentListenerで扱いにくいため個別に用意)
        public void SelectEasy() { Select(GameDifficulty.Easy); }
        public void SelectNormal() { Select(GameDifficulty.Normal); }
        public void SelectHard() { Select(GameDifficulty.Hard); }
        public void SelectExtra() { Select(GameDifficulty.Extra); }

        public void Select(GameDifficulty d)
        {
            // 未解放の難易度は選ばせない(ボタンは隠しているが念のため)
            if (!IsUnlocked(d)) return;

            _current = d;
            PlayerPrefs.SetInt(PrefKey, (int)d);
            // SetIntだけだと通常はアプリ終了時にしか書き込まれず、
            // モバイルでタスクキルされると選択が失われる。都度確定させる。
            PlayerPrefs.Save();
            Apply();
            OnDifficultyChanged?.Invoke(d);
        }

        // アプリが中断・終了するタイミングでも念のため確定させる
        private void OnApplicationPause(bool pause) { if (pause) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }

        // ===== クリア状況 =====

        private const string ClearedKeyPrefix = "proto_d_cleared_";

        /// <summary>その難易度をクリア済みか</summary>
        public bool IsCleared(GameDifficulty d)
        {
            return PlayerPrefs.GetInt(ClearedKeyPrefix + (int)d, 0) != 0;
        }

        /// <summary>
        /// クリアを記録する。GameManagerが孵化成功時に呼ぶ。
        /// Easy/Normal/Hardが揃うとExtraが解放される。
        /// </summary>
        public void MarkCleared(GameDifficulty d)
        {
            if (IsCleared(d)) return;

            // 記録する前にEXTRAが解放済みだったかを覚えておく。
            // 「今回のクリアで初めて解放された」ときだけ演出を出したいため。
            bool wasUnlocked = IsUnlocked(GameDifficulty.Extra);

            PlayerPrefs.SetInt(ClearedKeyPrefix + (int)d, 1);
            PlayerPrefs.Save();
            Debug.Log($"[DifficultySelector] {GetSettings(d).label} をクリア");

            OnClearedChanged?.Invoke();

            if (!wasUnlocked && IsUnlocked(GameDifficulty.Extra))
            {
                Debug.Log("[DifficultySelector] EXTRA を解放しました");
                OnExtraUnlocked?.Invoke();
            }
        }

        /// <summary>EXTRAが「今」解放された瞬間に一度だけ発火する。解放演出用</summary>
        public event System.Action OnExtraUnlocked;

        /// <summary>
        /// デバッグ用: Easy/Normal/Hardを全てクリア済みにしてEXTRAを解放する。
        /// 解放演出の確認や、展示前の動作確認に使う。
        /// </summary>
        [ContextMenu("EXTRAを解放する (デバッグ)")]
        public void DebugUnlockExtra()
        {
            bool wasUnlocked = IsUnlocked(GameDifficulty.Extra);
            for (int i = 0; i <= (int)GameDifficulty.Hard; i++)
            {
                PlayerPrefs.SetInt(ClearedKeyPrefix + i, 1);
            }
            PlayerPrefs.Save();

            OnClearedChanged?.Invoke();
            if (!wasUnlocked) OnExtraUnlocked?.Invoke();
            Debug.Log("[DifficultySelector] デバッグ: EXTRAを解放しました");
        }

        /// <summary>
        /// 選択できるか。Extraだけは Easy/Normal/Hard を全てクリアするまで隠す。
        /// </summary>
        public bool IsUnlocked(GameDifficulty d)
        {
            if (d != GameDifficulty.Extra) return true;
            return IsCleared(GameDifficulty.Easy)
                && IsCleared(GameDifficulty.Normal)
                && IsCleared(GameDifficulty.Hard);
        }

        /// <summary>クリア状況が変わったときに通知する。UIのCLEAR表示更新用</summary>
        public event System.Action OnClearedChanged;

        /// <summary>デバッグ用: クリア状況を全部消す</summary>
        [ContextMenu("クリア状況をリセット")]
        public void ResetClearedProgress()
        {
            for (int i = 0; i <= (int)GameDifficulty.Extra; i++)
            {
                PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);
            }
            PlayerPrefs.Save();
            if (_current == GameDifficulty.Extra) Select(GameDifficulty.Normal);
            OnClearedChanged?.Invoke();
            Debug.Log("[DifficultySelector] クリア状況をリセットしました");
        }

        /// <summary>
        /// 現在の選択を各コンポーネントへ流し込む。
        /// GameManager.StartIntro()からも呼び、ゲーム開始時点の値を確定させる。
        /// </summary>
        public void Apply()
        {
            var s = CurrentSettings;

            _timerManager?.SetTimeLimit(s.timeLimit);

            _difficultyManager?.ApplyRanges(
                s.minForceMultiplier, s.maxForceMultiplier,
                s.maxForceInterval, s.minForceInterval,
                s.minMaxLinearSpeed, s.maxMaxLinearSpeed,
                s.minMaxAngularSpeed, s.maxMaxAngularSpeed);

            if (_eggController != null)
            {
                _eggController.centerAssistWeight = s.centerAssistWeight;
                _eggController.edgeCenterAssistWeight = s.edgeCenterAssistWeight;
            }

            if (_sweetSpotController != null)
            {
                _sweetSpotController.spotEnabled = s.sweetSpotEnabled;
                _sweetSpotController.changeInterval = s.sweetSpotChangeInterval;
                _sweetSpotController.moveSpeed = s.sweetSpotMoveSpeed;
                _sweetSpotController.spawnRadius = s.sweetSpotSpawnRadius;
                _sweetSpotController.baseSpeedMultiplier = s.sweetSpotBaseMultiplier;
                _sweetSpotController.maxSpeedMultiplier = s.sweetSpotMaxMultiplier;
                _sweetSpotController.forceMultiplierWhileInside = s.sweetSpotForceWhileInside;
            }

            Debug.Log($"[DifficultySelector] 難易度: {s.label} (制限時間 {s.timeLimit}秒 / " +
                      $"力 {s.minForceMultiplier}〜{s.maxForceMultiplier} / アシスト {s.centerAssistWeight})");
        }
    }
}
