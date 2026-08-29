using System.Collections;
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
        
        // [Header("References A")]
        // [SerializeField] private EggFallDetector _eggFallDetector = null;
        [SerializeField] private PanController _panController = null;
        [SerializeField] private EggController _eggController = null;
        [SerializeField] private GroundImpactDetector  _groundImpactDetector = null;
        // [SerializeField] private EggStateController _eggStateController = null;
        // [SerializeField] private FeedbackController _feedbackController = null;
        // [SerializeField] private UIController _uiController = null;
        
        private void OnEnable()
        {
            if (_eggController != null)
            {
                _eggController.OnEggLanded += HandleEggLanded;
                _groundImpactDetector.OnGroundImpact += HandleGroundImpact;
                //eggController.OnGroundImpact += HandleGroundImpact;
            }
            // if (eggFallDetector != null)
            // {
            //     eggFallDetector.OnEggFall += HandleEggFall;
            // }
        }
        
        private void OnDisable()
        {
            if (_eggController != null)
            {
                _eggController.OnEggLanded -= HandleEggLanded;
                _groundImpactDetector.OnGroundImpact -= HandleGroundImpact;
            }
            // if (eggFallDetector != null)
            // {
            //     eggFallDetector.OnEggFall -= HandleEggFall;
            // }
        }

        public void StartIntro()
        {
            // TODO: TITLEからINTROへの遷移処理（Phase B5）
            if (gameState != GameState.TITLE && gameState != GameState.RESULT) return;
            
            gameState = GameState.INTRO;
            Debug.Log("[GameManager] Intro開始！ タイマーはまだ動かず、卵が落下します。");
            // TODO: A担当の eggController.DropEgg() を呼ぶ
            if (_eggController != null) _eggController.DropEgg();
            // TODO: C担当のUIやフィードバックを呼ぶ
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
            
            // if (_uiController != null) _uiController.ShowPlaying();
            // if (_feedbackController != null) _feedbackController.PlayStartFeedback();
        
            Debug.Log("[GameManager] PLAYING開始！ 60秒タイマーが動き出します。");
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
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
                _timerManager.UpdateTimer();
                gameProgress = _timerManager.GetProgress();
                // Debug.Log(gameProgress);
                if (_difficultyManager != null)
                {
                    DifficultyParams currentDifficulty = _difficultyManager.GetDifficultyParams(gameProgress);

                    // TODO: A担当のEggControllerへ currentDifficulty を渡す ( eggController.SetDifficulty(currentDifficulty) )
                    if(_eggController != null)
                    {
                        _eggController.SetDifficulty(currentDifficulty);
                    }
                    //Debug.Log($"[難易度変化] 進行度:{gameProgress:F2} | 力の倍率:{currentDifficulty.forceMultiplier:F2} | 発生間隔:{currentDifficulty.forceInterval:F2}秒 | 速度上限:{currentDifficulty.maxLinearSpeed:F1}");

                    float currentDistance = debugNormalizedDistance; 
                
                    /* 本実装時は以下のようにA担当から値を取得する
                    if (eggController != null && panController != null)
                    {
                        currentDistance = panController.GetNormalizedDistanceFromCenter(eggController.GetPosition());
                    }
                    */

                    dangerLevel = _difficultyManager.CalculateDanger(currentDistance);
                }
                
                crackProgress = gameProgress;

                if (_eggStateController != null)
                {
                    _eggStateController.SetCrackProgress(crackProgress);
                }
                
                // // 担当のフィードバック更新 ---
                // if (feedbackController != null)
                // {
                //     feedbackController.UpdateDangerFeedback(dangerLevel);
                //     feedbackController.UpdateCrackFeedback(crackProgress);
                //
                //     if (eggController != null)
                //     {
                //         feedbackController.PlayEggMovementFeedback(eggController.GetSpeed());
                //     }
                // }
                
                if (_timerManager.IsTimeUp())
                {
                    Success();
                }
            }
            // Debug
            if (Input.GetKeyDown(KeyCode.T)) StartIntro();         // Tキー: 画面タップ（Title -> Intro）
            if (Input.GetKeyDown(KeyCode.L)) HandleEggLanded();
            if (Input.GetKeyDown(KeyCode.F)) HandleEggFall();       // Fキーで落下開始
            if (Input.GetKeyDown(KeyCode.G)) HandleGroundImpact();  // Gキーで地面衝突
            if (Input.GetKeyDown(KeyCode.R)) Retry();
        }

        public void HandleEggFall()
        {
            if (gameState != GameState.PLAYING) return;
            
            gameState = GameState.FALLING;
            
            if (_timerManager != null)
            {
                _timerManager.StopTimer();
            }
            // if (_feedbackController != null && _eggController != null)
            // {
            //     _feedbackController.StartFallFeedback(_eggController.transform);
            // }
            // TODO: C担当への通知 (feedbackController.StartFallFeedback(egg))
            Debug.Log("[GameManager] 落下開始！ 状態: FALLING, タイマー停止");
        }
        
        public void HandleGroundImpact()
        {
            if (gameState != GameState.FALLING) return;
            // TODO: C担当への通知 (eggStateController.Break(), feedbackController.PlayFailureFeedback())

            gameResult = GameResult.FAILURE;
            gameState = GameState.RESULT;

            // TODO: C担当への通知 (uiController.ShowFailureResult())
            if (_eggStateController != null) _eggStateController.Break();
            // if (_feedbackController != null) _feedbackController.PlayFailureFeedback();
            // if (_uiController != null) _uiController.ShowFailureResult();
            Debug.Log("[GameManager] 地面衝突！ 卵が割れた。 状態: RESULT, 結果: FAILURE");
        }

        public void Success()
        {
            if (gameState != GameState.PLAYING) return;
            gameState = GameState.HATCHING;
            Debug.Log("[GameManager] 120秒到達！ HATCHING状態へ移行し、孵化を待機します。");
            
            if (_timerManager != null)
            {
                _timerManager.StopTimer();
            }

            if (_eggStateController != null)
            {
                _eggStateController.Hatch();
            }
            
            // if (_feedbackController != null) _feedbackController.PlaySuccessFeedback();

            StartCoroutine(SuccessSequence());
        }

        public IEnumerator SuccessSequence()
        {
            yield return new WaitForSeconds(successResultDelay);
            
            gameResult = GameResult.SUCCESS;
            gameState = GameState.RESULT;
            // if (_uiController != null) _uiController.ShowSuccessResult();
            Debug.Log("[GameManager] 孵化完了！ SUCCESS RESULT画面へ進みます。");
        }

        public void Retry()
        {
            if (gameState != GameState.RESULT) return;
            gameProgress = 0f;
            dangerLevel = 0f;
            crackProgress = 0f;
            gameResult = GameResult.NONE;

            if (_timerManager != null)
            {
                _timerManager.ResetTimer();
            }
            // TODO: 他クラスの初期化 (EggStateController, FeedbackController, PanController)
            
            if (_eggStateController != null)
            {
                _eggStateController.ResetState();
            }
            // if (_feedbackController != null) _feedbackController.ResetFeedback();
            // if (_panController != null) _panController.ResetPan();
            //
            // if (_uiController != null) _uiController.ShowTitle();
            if (_eggController != null) _eggController.ResetEgg();
            // 状態をリセットして開始シーケンスへ戻る
            StartIntro();
            Debug.Log("[GameManager] リトライ！ 各種値をリセットし、Introへ戻ります。");
        }
    }
}