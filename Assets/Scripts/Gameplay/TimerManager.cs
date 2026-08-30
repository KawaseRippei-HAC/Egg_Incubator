using UnityEngine;

namespace Gameplay
{
    public class TimerManager : MonoBehaviour
    {
        [SerializeField] private float timeLimit = 120f;
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