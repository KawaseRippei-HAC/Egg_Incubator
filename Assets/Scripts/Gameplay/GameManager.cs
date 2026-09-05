using UnityEngine.InputSystem;
using System.Collections;
using Feedback;
using Proto.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [FormerlySerializedAs("_gameState")]
        [Header("Game State")]
        [SerializeField] private GameState gameState = GameState.TITLE;
        [FormerlySerializedAs("_gameResult")]
        [SerializeField] private GameResult gameResult = GameResult.NONE;

        [Header("Game Values")]
        [SerializeField] private float gameProgress = 0f;
        [SerializeField] private float dangerLevel = 0f;
        [SerializeField] private float crackProgress = 0f;
        // フィードバック担当の演出完了を待つための疑似的な遅延時間
        [SerializeField] private float successResultDelay = 3f;

        [Header("Debug")]
        [SerializeField, Range(0f,1f)] private float debugNormalizedDistance = 0f;

        public float Progress => gameProgress;
        public float DangerLevel => dangerLevel;
        public float CrackProgress => crackProgress;
        public GameState CurrentState => gameState;

        [Header("Reference B")]
        [SerializeField] private TimerManager _timerManager = null;
        [SerializeField] private DifficultyManager _difficultyManager = null;
        [SerializeField] private EggStateController _eggStateController = null;

        [Header("References A")]
        [SerializeField] private PanController _panController = null;
        [SerializeField] private EggController _eggController = null;
        [SerializeField] private GroundImpactDetector _groundImpactDetector = null;
        [SerializeField] private SweetSpotController _sweetSpotController = null;


        [Header("References C")]
        [SerializeField] private FeedbackController _feedbackController = null;
        [SerializeField] private UIController _uiController = null;
        // EggHatchControllerには自動発火の仕組みがないため、ここで明示的にTriggerHatch()を呼ぶ必要がある。
        [SerializeField] private EggHatchController _eggHatchController = null;
        // 2025/08/29追加: autoTriggerOnCollisionをoffにしたため、明示的にTriggerBreak()を呼ぶ必要がある
        [SerializeField] private EggBreakController _eggBreakController = null;

        
        [Header("卵の落下制御 (2025/08/29追加)")]
        [Tooltip("卵本体のRigidbody。StartIntro()が呼ばれるまで卵をその場に固定(isKinematic=true)しておき、" +
                 "意図しない自由落下を防ぐ。TriggerBreak/TriggerHatch側で再度isKinematicを操作するため、" +
                 "Retry()時にもここへ戻す")]
        [SerializeField] private Rigidbody _eggRigidbody = null;
        [Tooltip("パンの縁を超えた瞬間・地面衝突の瞬間を自動検知し、HandleEggFall/HandleGroundImpactを自動呼び出しする")]
        [SerializeField] private EggFallDetector _eggFallDetector = null;
        
        [Header("Tap Feedback")]
        [SerializeField] private ParticleSystem _tapEffectParticle = null;
        [SerializeField] private AudioSource _tapAudioSource = null;
        [SerializeField] private AudioClip _tapSE = null;
        [SerializeField] private ScreenEffectController _screenEffectController = null;

        private Vector3 _eggInitialPosition;
        private Quaternion _eggInitialRotation;
        private bool _eggInitialCaptured;

        private void OnEnable()
        {
            if (_eggController != null)
            {
                _eggController.OnEggLanded += HandleEggLanded;
            }

            if (_groundImpactDetector != null)
            {
                _groundImpactDetector.OnGroundImpact += HandleGroundImpact;
            }

            if (_eggFallDetector != null)
            {
                _eggFallDetector.OnEggFall += HandleEggFall;
            }
        }

        private void OnDisable()
        {
            if (_eggController != null)
            {
                _eggController.OnEggLanded -= HandleEggLanded;
            }

            if (_groundImpactDetector != null)
            {
                _groundImpactDetector.OnGroundImpact -= HandleGroundImpact;
            }

            if (_eggFallDetector != null)
            {
                _eggFallDetector.OnEggFall -= HandleEggFall;
            }
        }

        private void Awake()
        {
            if (_sweetSpotController != null) _sweetSpotController.DeactivateSpot();
            // 卵の初期位置・回転を記憶しておき、Retry()で物理的にも元の位置へ戻せるようにする。
            // また、StartIntro()が呼ばれるまでは重力で自由落下しないようKinematicで固定しておく。
            if (_eggRigidbody != null)
            {
                _eggInitialPosition = _eggRigidbody.transform.position;
                _eggInitialRotation = _eggRigidbody.transform.rotation;
                _eggInitialCaptured = true;

                _eggRigidbody.isKinematic = true;
                _eggRigidbody.linearVelocity = Vector3.zero;
                _eggRigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void StartIntro()
        {
            // TODO: TITLEからINTROへの遷移処理（Phase B5）
            if (gameState != GameState.TITLE && gameState != GameState.RESULT) return;

            gameState = GameState.INTRO;
            Debug.Log("[GameManager] Intro開始！ タイマーはまだ動かず、卵が落下します。");

            if (_eggController != null)
            {
                _eggController.DropEgg();
            }

            // 2025/08/29追加: ここで初めて卵の物理演算を有効化し、実際に落下させる。
            // (以前はRigidbodyが最初から非Kinematicだったため、Tキーを押す前から自由落下していた)
            if (_eggRigidbody != null)
            {
                _eggRigidbody.isKinematic = false;
            }
            _uiController?.ShowPlaying();
        }

        public void HandleEggLanded()
        {
            // TODO: 卵着地時の処理（Phase B5）
            if (gameState != GameState.INTRO) return;

            Debug.Log("[GameManager] フライパンに着地！ ゲームを開始します。");
            StartGame();
        }

        public void StartGame()
        {
            // TODO: PLAYING移行とTimer開始処理（Phase B5）
            gameState = GameState.PLAYING;

            // 着地してPLAYINGになった瞬間からタイマーを開始
            if (_timerManager != null)
            {
                _timerManager.StartTimer();
            }

            if (_feedbackController != null)
            {
                _feedbackController.StartGameFeedback();
            }
            if (_uiController != null)
            {
                _uiController.ShowPlaying();
            }

            if (_sweetSpotController != null) _sweetSpotController.ActivateSpot();
            Debug.Log("[GameManager] PLAYING開始！ 60秒タイマーが動き出します。");
        }

        public void Update()
        {
            // タップでの進行操作: TITLE中のタップでStartIntro()、RESULT中のタップでRetry()
            if (Input.GetMouseButtonDown(0))
            {
                PlayTapFeedback();
                if (gameState == GameState.TITLE)
                {
                    StartIntro();
                }
                else if (gameState == GameState.RESULT)
                {
                    Retry();
                }
            }
            // TODO: PLAYING状態での毎フレーム更新処理（Timer, progress, dangerLevelの更新など）
            if (gameState == GameState.PLAYING && _timerManager != null)
            {
                float speedMultiplier = 1.0f;

                if (_sweetSpotController != null && _sweetSpotController.IsEggInside)
                {
                    speedMultiplier = 3.0f;
                }
                
                _timerManager.UpdateTimer(speedMultiplier);
                
                gameProgress = _timerManager.GetProgress();
                if (_difficultyManager != null)
                {
                    Proto.Core.DifficultyParams currentDifficulty = _difficultyManager.GetDifficultyParams(gameProgress);

                    // 2025/08/29修正: EggController.csが Proto.Core.DifficultyParams を直接使うように
                    // 統一されたため、以前の変換コード(global::DifficultyParamsへの詰め替え)は不要になった。
                    if (_eggController != null)
                    {
                        _eggController.SetDifficulty(currentDifficulty);
                    }

                    float currentDistance = debugNormalizedDistance;

                    // 2025/08/29有効化: パンと卵の実際の距離が取れる場合はそちらを優先する。
                    // 未接続(テスト中等)は引き続きdebugNormalizedDistanceスライダーで確認できる。
                    if (_eggController != null && _panController != null)
                    {
                        currentDistance = _panController.GetNormalizedDistanceFromCenter(_eggController.GetPosition());
                    }

                    dangerLevel = _difficultyManager.CalculateDanger(currentDistance);

                    if (_feedbackController != null)
                    {
                        _feedbackController.UpdateDangerFeedback(dangerLevel);
                    }
                    if (_screenEffectController != null)
                    {
                        bool isInside = (_sweetSpotController != null && _sweetSpotController.IsEggInside);
                        _screenEffectController.UpdateEffectState(dangerLevel, isInside);
                    }
                }

                crackProgress = gameProgress;

                if (_eggStateController != null)
                {
                    _eggStateController.SetCrackProgress(crackProgress);
                }

                if (_feedbackController != null)
                {
                    _feedbackController.UpdateCrackFeedback(crackProgress);

                    if (_eggController != null)
                    {
                        _feedbackController.PlayEggMovementFeedback(_eggController.GetSpeed());
                    }
                }

                if (_timerManager.IsTimeUp())
                {
                    Success();
                }
            }
            // Debug
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tKey.wasPressedThisFrame) StartIntro();         // Tキー: 画面タップ（Title -> Intro）
                if (Keyboard.current.lKey.wasPressedThisFrame) HandleEggLanded();
                if (Keyboard.current.fKey.wasPressedThisFrame) HandleEggFall();       // Fキーで落下開始
                if (Keyboard.current.gKey.wasPressedThisFrame) HandleGroundImpact();  // Gキーで地面衝突
                if (Keyboard.current.rKey.wasPressedThisFrame) Retry();
            }
        }
        private void PlayTapFeedback()
        {
            if (_tapAudioSource != null && _tapSE != null)
            {
                _tapAudioSource.PlayOneShot(_tapSE);
            }

            if (_tapEffectParticle != null && Camera.main != null)
            {
                Vector3 mousePos = Input.mousePosition;
                // カメラからどれくらい奥にエフェクトを出すか（シーンに合わせて調整してください）
                mousePos.z = 5f; 
                
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                
                _tapEffectParticle.transform.position = worldPos;
                _tapEffectParticle.Play();
            }
        }

        public void HandleEggFall()
        {
            if (gameState != GameState.PLAYING) return;

            gameState = GameState.FALLING;

            if (_sweetSpotController != null) _sweetSpotController.DeactivateSpot();
            // PLAYINGを抜けたのでビネットを消す。UpdateEffectState()はPLAYING中しか
            // 呼ばれないため、ここで明示的に切らないと直前の危険度が残り続ける。
            if (_screenEffectController != null) _screenEffectController.ClearEffect();

            if (_timerManager != null)
            {
                _timerManager.StopTimer();
            }

            if (_feedbackController != null && _eggController != null)
            {
                _feedbackController.StartFallFeedback(_eggController.transform);
            }
            Debug.Log("[GameManager] 落下開始！ 状態: FALLING, タイマー停止");
        }

        public void HandleGroundImpact()
        {
            if (gameState != GameState.FALLING) return;

            if (_eggStateController != null)
            {
                _eggStateController.Break();
            }
            if (_feedbackController != null)
            {
                _feedbackController.PlayFailureFeedback();
            }
            // 2025/08/29修正: EggBreakController.autoTriggerOnCollisionをfalseにしたため、
            // (以前は卵が地面(Groundタグ)に接触した瞬間に自動で破壊してしまい、INTROの安全な着地でも
            //  割れてしまっていた)、ここで明示的にTriggerBreak()を呼ぶ。
            if (_eggBreakController != null)
            {
                _eggBreakController.TriggerBreak();
            }

            gameResult = GameResult.FAILURE;
            gameState = GameState.RESULT;

            if (_uiController != null)
            {
                _uiController.ShowFailureResult();
            }
            Debug.Log("[GameManager] 地面衝突！ 卵が割れた。 状態: RESULT, 結果: FAILURE");
        }

        public void Success()
        {
            if (gameState != GameState.PLAYING ) return;
            gameState = GameState.HATCHING;
            Debug.Log("[GameManager] 成功！ HATCHING状態へ移行し、孵化を待機します。");
            if (_sweetSpotController != null) _sweetSpotController.DeactivateSpot();
            // 孵化演出をビネット越しに見せないよう、PLAYINGを抜けた時点で消す
            if (_screenEffectController != null) _screenEffectController.ClearEffect();
            
            if (_eggRigidbody != null)
            {
                _eggRigidbody.isKinematic = true;
                _eggRigidbody.linearVelocity = Vector3.zero;
                _eggRigidbody.angularVelocity = Vector3.zero;
            }

            if (_timerManager != null)
            {
                _timerManager.StopTimer();
            }

            if (_eggStateController != null)
            {
                _eggStateController.Hatch();
            }

            // EggHatchControllerには自動発火の仕組みがないため、ここで明示的に呼ぶ。
            if (_eggHatchController != null)
            {
                _eggHatchController.TriggerHatch();
            }

            // 触覚・カメラ・音は孵化演出の開始と同時に(結果UIより先に)発火させる。
            if (_feedbackController != null)
            {
                _feedbackController.PlaySuccessFeedback();
            }

            StartCoroutine(SuccessSequence());
        }

        public IEnumerator SuccessSequence()
        {
            // successResultDelay = EggHatchControllerの予兆(anticipationDuration)+弾け演出が
            // ひと段落するまでの待ち時間。演出秒数が変わった場合はここも要調整。
            yield return new WaitForSeconds(successResultDelay);

            gameResult = GameResult.SUCCESS;
            gameState = GameState.RESULT;

            if (_uiController != null)
            {
                _uiController.ShowSuccessResult();
            }
            Debug.Log("[GameManager] 孵化完了！ SUCCESS RESULT画面へ進みます。");
        }

        public void Retry()
        {
            if (gameState != GameState.RESULT) return;

            if (_sweetSpotController != null) _sweetSpotController.DeactivateSpot();
            // タイトルへ戻るので、フェードを待たずに即座にビネットを消す
            if (_screenEffectController != null) _screenEffectController.ClearEffect(immediate: true);

            gameProgress = 0f;
            dangerLevel = 0f;
            crackProgress = 0f;
            gameResult = GameResult.NONE;

            if (_timerManager != null)
            {
                _timerManager.ResetTimer();
            }

            if (_eggStateController != null)
            {
                _eggStateController.ResetState();
            }

            if (_feedbackController != null)
            {
                _feedbackController.ResetFeedback();
            }

            // 2025/08/29追加: 割れた殻・孵化した殻の見た目を元に戻す。
            // (これが無かったため、Retry後も破片/孵化ビジュアルが表示されたままだった)
            if (_eggBreakController != null)
            {
                _eggBreakController.ResetForTesting();
            }
            if (_eggHatchController != null)
            {
                _eggHatchController.ResetForTesting();
            }

            // 2025/08/29追加: 卵本体を初期位置・初期回転に戻し、再びStartIntro()まで
            // 自由落下しないようKinematicへ戻す。
            if (_eggRigidbody != null && _eggInitialCaptured)
            {
                _eggRigidbody.linearVelocity = Vector3.zero;
                _eggRigidbody.angularVelocity = Vector3.zero;
                _eggRigidbody.transform.position = _eggInitialPosition;
                _eggRigidbody.transform.rotation = _eggInitialRotation;
                _eggRigidbody.isKinematic = true;
            }

            if (_panController != null)
            {
                _panController.ResetPan();
            }
            if (_eggController != null)
            {
                _eggController.ResetEgg();
            }

            // 2025/08/29仕様変更: 以前はRetry()から自動でStartIntro()を呼んで卵を落とし直していたが、
            // 「リスタートで一度タイトル画面に戻り、「ゲーム開始」で初めて卵が落ちてくる」という
            // 要望に合わせ、TITLE状態で停止して次のStartIntro()呼び出し(Tキー/タップ)を待つ。
            gameState = GameState.TITLE;
            Debug.Log("[GameManager] リトライ！ 各種値をリセットし、TITLEへ戻ります。");
        }
    }
}
