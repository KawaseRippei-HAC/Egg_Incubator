namespace Gameplay
{
    using UnityEngine;

    public class SweetSpotController : MonoBehaviour
    {
        public float changeInterval = 4f; // 移動する間隔
        public float spawnRadius = 0.3f;    // フライパン上の出現範囲
        
        [Tooltip("サークルが移動するスピード（数値が高いほど速い）")]
        public float moveSpeed = 1.5f;

        public bool IsEggInside { get; private set; }

        private float timer;
        private Vector3 targetLocalPosition; // ★追加：移動先の目標座標

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
                // ▼ 修正: 古いメソッドではなく、新しい目的地を設定するメソッドを呼ぶ
                SetNewTargetPosition();
                timer = 0;
            }
            
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPosition, Time.deltaTime * moveSpeed);
        }
        
        private void SetNewTargetPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            targetLocalPosition = new Vector3(randomCircle.x, -0.1f, randomCircle.y);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player")) IsEggInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) IsEggInside = false;
        }
        public void ActivateSpot()
        {
            gameObject.SetActive(true); // オブジェクトを表示
            SetNewTargetPosition();
            transform.localPosition = targetLocalPosition;
            timer = 0f;
            IsEggInside = false;
        }

        // --- 追加：PLAYING終了時に呼ばれる ---
        public void DeactivateSpot()
        {
            IsEggInside = false;
            gameObject.SetActive(false); // オブジェクトを非表示
        }
    }
}