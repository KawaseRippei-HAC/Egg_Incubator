// using System.Collections;
// using Proto.Core;
// using UnityEngine;

// namespace Gameplay
// {
    
//     public class GameManager : MonoBehaviour
//     {
//         [Header("Game State")]
//         [SerializeField] private GameState _gameState = GameState.TITLE;
//         [SerializeField] private GameResult _gameResult = GameResult.NONE;

//         [Header("Game Values")] 
//         [SerializeField] private float gameProgress = 0f;
//         [SerializeField] private float dangerLevel = 0f;
//         [SerializeField] private float crackProgress = 0f;
//         // フィードバック担当の演出完了を待つための疑似的な遅延時間
//         [SerializeField] private float successResultDelay = 3f;
        
//         [Header("Debug")]
//         [SerializeField, Range(0f,1f)] private float debugNormalizedDistance = 0f;
        
//         public float Progress => gameProgress;
//         public float DangerLevel => dangerLevel;
//         public float CrackProgress => crackProgress;
//         public GameState CurrentState => _gameState;
        
//         [Header("Reference")]
//         [SerializeField] private TimerManager _timerManager = null;
//         [SerializeField] private DifficultyManager _difficultyManager = null;
//         [SerializeField] private EggStateController _eggStateController = null;
//         // [SerializeField] private EggFallDetector _eggFallDetector = null;
//         // [SerializeField] private PanController  _panController = null;
//         // [SerializeField] private EggStateController _eggStateController = null;
//         // [SerializeField] private FeedbackController _feedbackController = null;
//         // [SerializeField] private UIController _uiController = null;

//         public void StartIntro()
//         {
//             // TODO: TITLEからINTROへの遷移処理（Phase B5）
//             if (_gameState != GameState.TITLE && _gameState != GameState.RESULT) return;
            
//             _gameState = GameState.INTRO;
//             Debug.Log("[GameManager] Intro開始！ タイマーはまだ動かず、卵が落下します。");
//             // TODO: A担当の eggController.DropEgg() を呼ぶ
//             // TODO: C担当のUIやフィードバックを呼ぶ
//         }
        
//         public void HandleEggLanded()
//         {
//             // TODO: 卵着地時の処理（Phase B5）
//             if (_gameState != GameState.INTRO) return;
            
//             Debug.Log("[GameManager] フライパンに着地！ ゲームを開始します。");
//             StartGame();
            
//         }

//         public void StartGame()
//         {
//             // TODO: PLAYING移行とTimer開始処理（Phase B5）
//             _gameState = GameState.PLAYING;

//             // 着地してPLAYINGになった瞬間からタイマーを開始
//             if (_timerManager != null)
//             {
//                 _timerManager.StartTimer();
//             }
        
//             Debug.Log("[GameManager] PLAYING開始！ 60秒タイマーが動き出します。");
//         }

//         public void Update()
//         {
//             // TODO: PLAYING状態での毎フレーム更新処理（Timer, progress, dangerLevelの更新など）
//             if (_gameState == GameState.PLAYING && _timerManager != null)
//             {
//                 _timerManager.UpdateTimer();
//                 gameProgress = _timerManager.GetProgress();
//                 Debug.Log(gameProgress);
//                 if (_difficultyManager != null)
//                 {
//                     DifficultyParams currentDifficulty = _difficultyManager.GetDifficultyParams(gameProgress);
//                     // TODO: A担当のEggControllerへ currentDifficulty を渡す ( eggController.SetDifficulty(currentDifficulty) )
//                     float currentDistance = debugNormalizedDistance; 
                
//                     /* 本実装時は以下のようにA担当から値を取得する
//                     if (eggController != null && panController != null)
//                     {
//                         currentDistance = panController.GetNormalizedDistanceFromCenter(eggController.GetPosition());
//                     }
//                     */

//                     dangerLevel = _difficultyManager.CalculateDanger(currentDistance);

//                     // TODO: C担当へ渡す ( feedbackController.UpdateDangerFeedback(dangerLevel) )
//                 }
                
//                 crackProgress = gameProgress;

//                 if (_eggStateController != null)
//                 {
//                     _eggStateController.SetCrackProgress(crackProgress);
//                 }
//                 // TODO: C担当へ渡す ( feedbackController.UpdateCrackFeedback(crackProgress) )
                
//                 if (_timerManager.IsTimeUp())
//                 {
//                     Success();
//                 }
//             }
//             // Debug
//             if (Input.GetKeyDown(KeyCode.T)) StartIntro();         // Tキー: 画面タップ（Title -> Intro）
//             if (Input.GetKeyDown(KeyCode.L)) HandleEggLanded();
//             if (Input.GetKeyDown(KeyCode.F)) HandleEggFall();       // Fキーで落下開始
//             if (Input.GetKeyDown(KeyCode.G)) HandleGroundImpact();  // Gキーで地面衝突
//             if (Input.GetKeyDown(KeyCode.R)) Retry();
            

//         }

//         public void HandleEggFall()
//         {
//             if (_gameState != GameState.PLAYING) return;
//             _gameState = GameState.FALLING;
//             if (_timerManager != null)
//             {
//                 _timerManager.StopTimer();
//             }
//             // TODO: C担当への通知 (feedbackController.StartFallFeedback(egg))
//             Debug.Log("[GameManager] 落下開始！ 状態: FALLING, タイマー停止");
//         }
        
//         public void HandleGroundImpact()
//         {
//             if (_gameState != GameState.FALLING) return;
//             // TODO: C担当への通知 (eggStateController.Break(), feedbackController.PlayFailureFeedback())

//             _gameResult = GameResult.FAILURE;
//             _gameState = GameState.RESULT;

//             // TODO: C担当への通知 (uiController.ShowFailureResult())
//             Debug.Log("[GameManager] 地面衝突！ 卵が割れた。 状態: RESULT, 結果: FAILURE");
//         }

//         public void Success()
//         {
//             if (_gameState != GameState.PLAYING) return;
//             _gameState = GameState.HATCHING;
//             Debug.Log("[GameManager] 120秒到達！ HATCHING状態へ移行し、孵化を待機します。");
            
//             if (_timerManager != null)
//             {
//                 _timerManager.StopTimer();
//             }

//             if (_eggStateController != null)
//             {
//                 _eggStateController.Hatch();
//             }

//             StartCoroutine(SuccessSequence());
//         }

//         public IEnumerator SuccessSequence()
//         {
//             yield return new WaitForSeconds(successResultDelay);
            
//             _gameResult = GameResult.SUCCESS;
//             _gameState = GameState.RESULT;
            
//             // TODO: C担当へ通知 ( uiController.ShowSuccessResult() )
//             Debug.Log("[GameManager] 孵化完了！ SUCCESS RESULT画面へ進みます。");
//         }

//         public void Retry()
//         {
//             if (_gameState != GameState.RESULT) return;
//             gameProgress = 0f;
//             dangerLevel = 0f;
//             crackProgress = 0f;
//             _gameResult = GameResult.NONE;

//             if (_timerManager != null)
//             {
//                 _timerManager.ResetTimer();
//             }
//             // TODO: 他クラスの初期化 (EggStateController, FeedbackController, PanController)
            
//             if (_eggStateController != null)
//             {
//                 _eggStateController.ResetState();
//             }

//             // 状態をリセットして開始シーケンスへ戻る
//             StartIntro();
//             Debug.Log("[GameManager] リトライ！ 各種値をリセットし、Introへ戻ります。");
//         }
//     }
// }

using System.Collections;
using Proto.Core;
using UnityEngine;

namespace Gameplay
{
    
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private GameState _gameState = GameState.TITLE;
        [SerializeField] private GameResult _gameResult = GameResult.NONE;

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
        public GameState CurrentState => _gameState;
        
        [Header("Reference")]
        [SerializeField] private TimerManager _timerManager = null;
        [SerializeField] private DifficultyManager _difficultyManager = null;
        [SerializeField] private EggStateController _eggStateController = null;
        // [SerializeField] private EggFallDetector _eggFallDetector = null;
        // [SerializeField] private PanController  _panController = null;
        [SerializeField] private FeedbackController _feedbackController = null;
        [SerializeField] private UIController _uiController = null;
        // 2025/08/29追加: HandleGroundImpact/Successで実際に殻の破壊・孵化アニメーションを
        // 発火させるための参照。EggBreakControllerはautoTriggerOnCollision=trueなら物理衝突で
        // 自動発火するため必須ではないが、EggHatchControllerは自動発火の仕組みがないため
        // TriggerHatch()の呼び出し元が必要。ゲームループのテストのため暫定的に追加。要A/B担当確認。
        [SerializeField] private EggHatchController _eggHatchController = null;

        public void StartIntro()
        {
            // TODO: TITLEからINTROへの遷移処理（Phase B5）
            if (_gameState != GameState.TITLE && _gameState != GameState.RESULT) return;
            
            _gameState = GameState.INTRO;
            Debug.Log("[GameManager] Intro開始！ タイマーはまだ動かず、卵が落下します。");
            // TODO: A担当の eggController.DropEgg() を呼ぶ
            // TODO: C担当のUIやフィードバックを呼ぶ
        }
        
        public void HandleEggLanded()
        {
            // TODO: 卵着地時の処理（Phase B5）
            if (_gameState != GameState.INTRO) return;
            
            Debug.Log("[GameManager] フライパンに着地！ ゲームを開始します。");
            StartGame();
            
        }

        public void StartGame()
        {
            // TODO: PLAYING移行とTimer開始処理（Phase B5）
            _gameState = GameState.PLAYING;

            // 着地してPLAYINGになった瞬間からタイマーを開始
            if (_timerManager != null)
            {
                _timerManager.StartTimer();
            }

            if (_feedbackController != null)
            {
                _feedbackController.StartGameFeedback();
            }
        
            Debug.Log("[GameManager] PLAYING開始！ 60秒タイマーが動き出します。");
        }

        public void Update()
        {
            // TODO: PLAYING状態での毎フレーム更新処理（Timer, progress, dangerLevelの更新など）
            if (_gameState == GameState.PLAYING && _timerManager != null)
            {
                _timerManager.UpdateTimer();
                gameProgress = _timerManager.GetProgress();
                Debug.Log(gameProgress);
                if (_difficultyManager != null)
                {
                    Proto.Core.DifficultyParams currentDifficulty = _difficultyManager.GetDifficultyParams(gameProgress);
                    // TODO: A担当のEggControllerへ currentDifficulty を渡す ( eggController.SetDifficulty(currentDifficulty) )
                    float currentDistance = debugNormalizedDistance; 
                
                    /* 本実装時は以下のようにA担当から値を取得する
                    if (eggController != null && panController != null)
                    {
                        currentDistance = panController.GetNormalizedDistanceFromCenter(eggController.GetPosition());
                    }
                    */

                    dangerLevel = _difficultyManager.CalculateDanger(currentDistance);

                    if (_feedbackController != null)
                    {
                        _feedbackController.UpdateDangerFeedback(dangerLevel);
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
                }
                
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
            if (_gameState != GameState.PLAYING) return;
            _gameState = GameState.FALLING;
            if (_timerManager != null)
            {
                _timerManager.StopTimer();
            }
            // NOTE: feedbackController.StartFallFeedback(egg) を呼びたいが、GameManagerが
            // 卵のTransformをまだ保持していない(EggFallDetector.OnEggFallの引数として
            // 卵を渡す設計にするか、GameManagerにegg参照フィールドを追加するか要A担当確認)。
            // 卵の参照が取得でき次第、以下を有効化してください:
            // if (_feedbackController != null) _feedbackController.StartFallFeedback(eggTransform);
            Debug.Log("[GameManager] 落下開始！ 状態: FALLING, タイマー停止");
        }
        
        public void HandleGroundImpact()
        {
            if (_gameState != GameState.FALLING) return;

            if (_eggStateController != null)
            {
                _eggStateController.Break();
            }
            if (_feedbackController != null)
            {
                _feedbackController.PlayFailureFeedback();
            }
            // NOTE: 視覚的な破壊(殻の破片化)自体はEggBreakController.autoTriggerOnCollision=true
            // であれば地面(Groundタグ)との物理衝突で自動発火する想定。手動発火にしたい場合は
            // EggBreakControllerの参照を追加しTriggerBreak()を呼ぶ運用に変更してください。

            _gameResult = GameResult.FAILURE;
            _gameState = GameState.RESULT;

            if (_uiController != null)
            {
                _uiController.ShowFailureResult();
            }
            Debug.Log("[GameManager] 地面衝突！ 卵が割れた。 状態: RESULT, 結果: FAILURE");
        }

        public void Success()
        {
            if (_gameState != GameState.PLAYING) return;
            _gameState = GameState.HATCHING;
            Debug.Log("[GameManager] 120秒到達！ HATCHING状態へ移行し、孵化を待機します。");
            
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
            
            _gameResult = GameResult.SUCCESS;
            _gameState = GameState.RESULT;
            
            if (_uiController != null)
            {
                _uiController.ShowSuccessResult();
            }
            Debug.Log("[GameManager] 孵化完了！ SUCCESS RESULT画面へ進みます。");
        }

        public void Retry()
        {
            if (_gameState != GameState.RESULT) return;
            gameProgress = 0f;
            dangerLevel = 0f;
            crackProgress = 0f;
            _gameResult = GameResult.NONE;

            if (_timerManager != null)
            {
                _timerManager.ResetTimer();
            }
            // TODO: 他クラスの初期化 (PanController)
            
            if (_eggStateController != null)
            {
                _eggStateController.ResetState();
            }

            if (_feedbackController != null)
            {
                _feedbackController.ResetFeedback();
            }

            // 状態をリセットして開始シーケンスへ戻る
            StartIntro();
            Debug.Log("[GameManager] リトライ！ 各種値をリセットし、Introへ戻ります。");
        }
    }
}