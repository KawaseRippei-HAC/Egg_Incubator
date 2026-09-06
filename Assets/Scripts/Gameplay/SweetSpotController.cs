namespace Gameplay
{
    using UnityEngine;

    public class SweetSpotController : MonoBehaviour
    {
        public float changeInterval = 4f;
        public float spawnRadius = 0.3f;    
        
        [Tooltip("サークルが移動するスピード（数値が高いほど速い）")]
        public float moveSpeed = 1.5f;

        [Tooltip("フライパン面に接する高さ(親pan_mainのローカルY)。\n" +
                 "★エリアが面に沈む・浮く場合はここを調整する。\n" +
                 "以前はSetNewTargetPosition()内に-0.1がハードコードされており、\n" +
                 "ワールドで約0.05ぶんパンの中に沈んでいた。\n" +
                 "親のYスケールが0.5なので、この値1 = ワールド0.5に相当する。\n" +
                 "SweetSpotVisualの底がちょうどこの高さに来るよう組んである")]
        public float heightOnPan = 0.0012f;

        // ▼ 変更：AudioSourceではなく、直接音源ファイル（AudioClip）をセットできるようにしました
        [Header("Audio")]
        [Tooltip("回復中になり続けるSEをセットしてください")]
        public AudioClip healSE;

        [Header("見た目")]
        [Tooltip("拍動の周期(秒)。プレイヤーの目を引くためにゆっくり明滅・伸縮させる")]
        public float pulsePeriod = 1.2f;

        [Tooltip("拍動でどれだけ大きくなるか。0.06なら最大6%大きくなる。\n" +
                 "シェーダー(Custom/SweetSpotArea)側に波紋があるので控えめでよい")]
        [Range(0f, 0.5f)] public float pulseScale = 0.06f;

        [Tooltip("通常時の色。M_SweetSpotの_BaseColorを実行時に上書きする")]
        public Color idleColor = new Color(0.30f, 1f, 0.45f, 0.35f);

        [Tooltip("卵が入っている間の色")]
        public Color activeColor = new Color(0.65f, 1f, 0.55f, 0.55f);

        [Tooltip("通常時の明るさ。シェーダーの_Intensityへ入る")]
        public float idleIntensity = 1f;

        [Tooltip("卵が入っている間の明るさ。入った瞬間がはっきり分かるよう強めにする")]
        public float activeIntensity = 2.2f;

        [Tooltip("色・明るさが切り替わる速さ")]
        public float colorLerpSpeed = 10f;

        public bool IsEggInside { get; private set; }

        private float timer;
        private Vector3 targetLocalPosition;

        private AudioSource _audioSource;
        private Renderer _renderer;
        private Transform _visual;
        private Material _materialInstance;
        private Vector3 _baseScale;
        private float _pulseTime;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private void Awake()
        {
            // ★以前は無条件に AddComponent していたため、シーンに既にAudioSourceが
            // 付いているこのオブジェクトでは実行のたびに2つ目が増えていた。
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = healSE;
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;

            // 描画は子(SweetSpotVisual)に分離してある。
            // 親のスケールを触るとCapsuleCollider(トリガー範囲)まで一緒に伸びてしまうため、
            // 拍動は子のTransformに対してかける。
            _renderer = GetComponentInChildren<Renderer>(true);
            if (_renderer != null)
            {
                _visual = _renderer.transform;
                _baseScale = _visual.localScale;

                // マテリアルはアセット共有なので、実行時にインスタンス化してから色を触る
                // (そのまま書き換えるとアセットが汚れ、エディタ停止後も色が残る)
                _materialInstance = _renderer.material;
            }
            else
            {
                Debug.LogWarning($"[{nameof(SweetSpotController)}] 描画用のRendererが子に見つかりません。", this);
            }
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
            if (_visual != null && pulsePeriod > 0f && pulseScale > 0f)
            {
                _pulseTime += Time.deltaTime;
                // 0→1→0を繰り返す(Sinを0..1へ写像)
                float wave = (Mathf.Sin(_pulseTime / pulsePeriod * Mathf.PI * 2f) + 1f) * 0.5f;
                float s = 1f + wave * pulseScale;
                // 高さ(Y)は拍動させない。枠の伸びる高さが呼吸すると落ち着かないため
                _visual.localScale = new Vector3(_baseScale.x * s, _baseScale.y, _baseScale.z * s);
            }

            if (_materialInstance == null) return;

            float k = Time.deltaTime * colorLerpSpeed;

            if (_materialInstance.HasProperty(BaseColorId))
            {
                Color target = IsEggInside ? activeColor : idleColor;
                _materialInstance.SetColor(BaseColorId,
                    Color.Lerp(_materialInstance.GetColor(BaseColorId), target, k));
            }

            // Custom/SweetSpotArea の _Intensity。卵が入っている間だけ全体を強く光らせる
            if (_materialInstance.HasProperty(IntensityId))
            {
                float target = IsEggInside ? activeIntensity : idleIntensity;
                _materialInstance.SetFloat(IntensityId,
                    Mathf.Lerp(_materialInstance.GetFloat(IntensityId), target, k));
            }
        }
        
        private void SetNewTargetPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            targetLocalPosition = new Vector3(randomCircle.x, heightOnPan, randomCircle.y);
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
            if (_visual != null && _baseScale != Vector3.zero) _visual.localScale = _baseScale;
            if (_materialInstance != null)
            {
                if (_materialInstance.HasProperty(BaseColorId)) _materialInstance.SetColor(BaseColorId, idleColor);
                if (_materialInstance.HasProperty(IntensityId)) _materialInstance.SetFloat(IntensityId, idleIntensity);
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