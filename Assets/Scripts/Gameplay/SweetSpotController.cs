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

        [Header("見た目")]
        [Tooltip("拍動の周期(秒)。プレイヤーの目を引くためにゆっくり明滅・伸縮させる")]
        public float pulsePeriod = 1.2f;

        [Tooltip("拍動でどれだけ大きくなるか。0.15なら最大15%大きくなる")]
        [Range(0f, 0.5f)] public float pulseScale = 0.15f;

        [Tooltip("通常時の色。M_SweetSpotのBaseColorを実行時に上書きする")]
        public Color idleColor = new Color(0.35f, 1f, 0.45f, 0.55f);

        [Tooltip("卵が入っている間の色。入った瞬間が分かるように強く光らせる")]
        public Color activeColor = new Color(0.75f, 1f, 0.55f, 0.95f);

        [Tooltip("色が切り替わる速さ")]
        public float colorLerpSpeed = 10f;

        public bool IsEggInside { get; private set; }

        private float timer;
        private Vector3 targetLocalPosition;

        private AudioSource _audioSource;
        private Renderer _renderer;
        private Material _materialInstance;
        private Vector3 _baseScale;
        private float _pulseTime;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            // ★以前は無条件に AddComponent していたため、シーンに既にAudioSourceが
            // 付いているこのオブジェクトでは実行のたびに2つ目が増えていた。
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = healSE;
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;

            _baseScale = transform.localScale;

            // マテリアルはアセット共有なので、実行時にインスタンス化してから色を触る
            // (そのまま書き換えるとアセットが汚れ、エディタ停止後も色が残る)
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _materialInstance = _renderer.material;
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

            UpdateAppearance();
        }

        /// <summary>
        /// 拍動と色でスポットを目立たせる。
        /// 元はURP/Litの半透明ディスクで照明に沈み、Emissionも切れていて
        /// 「ここに入れ」という意図が全く伝わらない見た目だった。
        /// </summary>
        private void UpdateAppearance()
        {
            if (pulsePeriod > 0f && pulseScale > 0f)
            {
                _pulseTime += Time.deltaTime;
                // 0→1→0を繰り返す(Sinを0..1へ写像)
                float wave = (Mathf.Sin(_pulseTime / pulsePeriod * Mathf.PI * 2f) + 1f) * 0.5f;
                float s = 1f + wave * pulseScale;
                // 厚み(Y)は拍動させない。薄い円盤なので伸ばすと浮いて見える
                transform.localScale = new Vector3(_baseScale.x * s, _baseScale.y, _baseScale.z * s);
            }

            if (_materialInstance != null && _materialInstance.HasProperty(BaseColorId))
            {
                Color target = IsEggInside ? activeColor : idleColor;
                Color current = _materialInstance.GetColor(BaseColorId);
                _materialInstance.SetColor(BaseColorId,
                    Color.Lerp(current, target, Time.deltaTime * colorLerpSpeed));
            }
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

        /// <summary>PLAYINGに入った瞬間にGameManager.StartGame()から呼ぶ。ここでのみ表示される。</summary>
        public void ActivateSpot()
        {
            gameObject.SetActive(true);
            SetNewTargetPosition();
            transform.localPosition = targetLocalPosition;
            timer = 0f;
            _pulseTime = 0f;
            IsEggInside = false;

            // 前ラウンドの拍動・色が残らないよう初期状態へ戻す
            if (_baseScale != Vector3.zero) transform.localScale = _baseScale;
            if (_materialInstance != null && _materialInstance.HasProperty(BaseColorId))
            {
                _materialInstance.SetColor(BaseColorId, idleColor);
            }
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