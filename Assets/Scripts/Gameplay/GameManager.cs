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
                Debug.Log("gameProgress" + gameProgress);
                if (_timerManager.IsTimeUp())
                {
                    Success();
                }
            }
            
        }

        public void HandleEggFall()
        {
            // TODO: 落下開始時の処理（Phase B4）
        }
        
        public void HandleGroundImpact()
        {
            // TODO: 地面衝突時（割れる）の処理（Phase B4）
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
            // TODO: 各種状態のリセットと再開処理（Phase B4以降）
        }
    }
}