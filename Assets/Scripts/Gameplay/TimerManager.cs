using UnityEngine;

namespace Gameplay
{
    public class TimerManager : MonoBehaviour
    {
        // 落下=即死を維持する設計なので、120秒では1回あたりの投資が大きすぎる。
        // 60秒ならリトライ性と展示映えが両立する。
        [SerializeField] private float timeLimit = 60f;
        private float elapsedTime = 0f;
        private bool isRunning = false;

        public void StartTimer()
        {
            isRunning = true;
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        public void UpdateTimer(float multiplier = 1.0f)
        {
            if (isRunning)
            {
                // 経過時間に倍率を掛けて足す
                elapsedTime += Time.deltaTime * multiplier;
            }
        }

        public float GetProgress()
        {
            if (timeLimit <= 0f) return 1f;
            return Mathf.Clamp01(elapsedTime / timeLimit);
            
        }

        public bool IsTimeUp()
        {
            return elapsedTime > timeLimit;
        }

        public void ResetTimer()
        {
            elapsedTime = 0f;
        }
    }
}