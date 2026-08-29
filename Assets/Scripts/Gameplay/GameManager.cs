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
        
        public float Progress => gameProgress;
        public float DangerLevel => dangerLevel;
        public float CrackProgress => crackProgress;
        public GameState CurrentState => _gameState;
        
        [Header("Reference")]
        [SerializeField] private TimerManager _timerManager = null;
        // [SerializeField] private DifficultyMangaer _difficultyManger = null;
        // [SerializeField] private EggController _eggController = null;
        // [SerializeField] private EggFallDetector _eggFallDetector = null;
        // [SerializeField] private PanController  _panController = null;
        // [SerializeField] private EggStateController _eggStateController = null;
        // [SerializeField] private FeedbackController _feedbackController = null;
        // [SerializeField] private UIController _uiController = null;

        public void StartIntro()
        {
            // TODO: TITLEからINTROへの遷移処理（Phase B5）
        }
        
        public void HandleEggLanded()
        {
            // TODO: 卵着地時の処理（Phase B5）
        }

        public void StartGame()
        {
            // TODO: PLAYING移行とTimer開始処理（Phase B5）
        }

        public void Update()
        {
            // TODO: PLAYING状態での毎フレーム更新処理（Timer, progress, dangerLevelの更新など）
            if (_gameState == GameState.PLAYING && _timerManager != null)
            {
                _timerManager.UpdateTimer();
                gameProgress = _timerManager.GetProgress();
                if (_timerManager.IsTimeUp())
                {
                    Success();
                }
            }
            // Debug
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
            // TODO: C担当への通知 (feedbackController.StartFallFeedback(egg))
            Debug.Log("[GameManager] 落下開始！ 状態: FALLING, タイマー停止");
        }
        
        public void HandleGroundImpact()
        {
            if (_gameState != GameState.FALLING) return;
            // TODO: C担当への通知 (eggStateController.Break(), feedbackController.PlayFailureFeedback())

            _gameResult = GameResult.FAILURE;
            _gameState = GameState.RESULT;

            // TODO: C担当への通知 (uiController.ShowFailureResult())
            Debug.Log("[GameManager] 地面衝突！ 卵が割れた。 状態: RESULT, 結果: FAILURE");
        }

        public void Success()
        {
            // TODO: 120秒到達時のHATCHING移行処理（Phase B9）
            Debug.Log("Success");
        }

        public IEnumerator SuccessSequence()
        {
            // TODO: 孵化演出待ちとSUCCESS Result移行処理（Phase B9）
            yield return null;
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
                _timerManager.StopTimer();
            }
            // TODO: 他クラスの初期化 (EggStateController, FeedbackController, PanController)

            // 状態をリセットして開始シーケンスへ戻る
            StartIntro();
            Debug.Log("[GameManager] リトライ！ 各種値をリセットし、Introへ戻ります。");
        }
    }
}