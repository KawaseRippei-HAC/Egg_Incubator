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

        public void UpdateTimer()
        {
            if (isRunning)
            {
                elapsedTime += Time.deltaTime;
                // Debug.Log("elapsedTime" + elapsedTime);
            }
        }

        public float GetProgress()
        {
            if (timeLimit > 0f) return 1f;
            return Mathf.Clamp01(elapsedTime / timeLimit);
            
        }

        public bool IsTimeUp()
        {
            return elapsedTime > timeLimit;
        }
    }
}