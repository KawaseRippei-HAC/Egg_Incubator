namespace Gameplay
{
    using UnityEngine;

    public class SweetSpotController : MonoBehaviour
    {
        public float changeInterval = 4f;
        public float spawnRadius = 0.3f;    
        
        [Tooltip("サークルが移動するスピード（数値が高いほど速い）")]
        public float moveSpeed = 1.5f;

        // ▼ 変更：AudioSourceではなく、直接音源ファイル（AudioClip）をセットできるようにしました
        [Header("Audio")]
        [Tooltip("回復中になり続けるSEをセットしてください")]
        public AudioClip healSE;

        public bool IsEggInside { get; private set; }

        private float timer;
        private Vector3 targetLocalPosition;
        
        // 裏で自動生成するAudioSource用
        private AudioSource _audioSource;

        private void Awake()
        {
            // ゲーム開始時に、自分自身にAudioSourceを自動追加して初期設定を行う
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = healSE;
            _audioSource.playOnAwake = false; // 勝手に鳴らないようにする
            _audioSource.loop = true;         // ループ再生をオンにする
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                IsEggInside = true;
                
                // SEがセットされていて、まだ鳴っていなければ再生
                if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                IsEggInside = false;
                
                if (_audioSource != null)
                {
                    _audioSource.Stop();
                }
            }
        }

        public void ActivateSpot()
        {
            gameObject.SetActive(true);
            SetNewTargetPosition();
            transform.localPosition = targetLocalPosition;
            timer = 0f;
            IsEggInside = false;
        }

        public void DeactivateSpot()
        {
            IsEggInside = false;

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            gameObject.SetActive(false);
        }
    }
}