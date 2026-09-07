namespace Gameplay
{
    using UnityEngine;

    public class SweetSpotController : MonoBehaviour
    {
        public float changeInterval = 4f;
        public float spawnRadius = 0.3f;    
        
        [Tooltip("サークルが移動するスピード（数値が高いほど速い）")]
        public float moveSpeed = 1.5f;

        [Header("リスクと報酬")]
        // ★このゲームで唯一の能動的な選択肢。
        // 元は「入ると3倍速」というだけのボーナスだったが、進行度が上がると難易度も
        // 上がる仕組みなので、実際にはハイリスク・ハイリターンになっていた。
        // それがプレイヤーに全く伝わっていなかったため、リスクを見せたうえで
        // 報酬側も伸ばし、「攻めるか守るか」の選択にする。
        [Tooltip("入った直後のタイマー加速倍率")]
        public float baseSpeedMultiplier = 3f;

        [Tooltip("居続けたときに到達する最大の加速倍率")]
        public float maxSpeedMultiplier = 5f;

        [Tooltip("スポットの外にいるときのタイマー進行倍率。\n" +
                 "★0にすると『スポットにいる間しか孵化が進まない』ルールになる。\n" +
                 "卵を放置しても勝てる問題への対処で、プレイヤーは必ず取りに行く必要がある。\n" +
                 "0.2などにすると『外でもゆっくりは進む』救済寄りの設定にできる")]
        public float outsideSpeedMultiplier = 0f;

        [Tooltip("baseからmaxまで伸びきるのにかかる連続滞在秒数。外に出ると0に戻る")]
        public float rampUpSeconds = 3f;

        [Tooltip("滞在中に卵の暴れる力へ掛ける倍率。1より大きいほど『おいしいが危ない』場所になる")]
        public float forceMultiplierWhileInside = 1.5f;

        [Tooltip("出現位置の内側の下限(spawnRadiusに対する割合)。0.5なら中心寄り半分には出ない。\n" +
                 "中心の安全地帯に出さないことで『取りに行く=端に寄る』構造を作る")]
        [Range(0f, 0.95f)] public float minSpawnRadiusRatio = 0.5f;

        /// <summary>現在のタイマー加速倍率。GameManagerが毎フレーム参照する</summary>
        public float CurrentSpeedMultiplier { get; private set; } = 1f;

        /// <summary>卵が中にいる間、難易度のforceMultiplierへ掛ける倍率(外なら1)</summary>
        public float CurrentForceMultiplier => IsEggInside ? forceMultiplierWhileInside : 1f;

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
        private float _dwellTime;
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

            UpdateDwell();
            UpdateAppearance();
        }

        /// <summary>
        /// 連続滞在時間に応じてタイマー加速倍率を伸ばす。
        /// 一度でも外に出たら0へ戻るので、「入り直し」では稼げない。
        /// </summary>
        private void UpdateDwell()
        {
            if (IsEggInside)
            {
                _dwellTime += Time.deltaTime;
                float t = rampUpSeconds > 0f ? Mathf.Clamp01(_dwellTime / rampUpSeconds) : 1f;
                CurrentSpeedMultiplier = Mathf.Lerp(baseSpeedMultiplier, maxSpeedMultiplier, t);

                // 倍率が伸びていることを音でも伝える(SEのピッチを上げる)
                if (_audioSource != null)
                {
                    _audioSource.pitch = Mathf.Lerp(1f, 1.35f, t);
                }
            }
            else
            {
                _dwellTime = 0f;
                CurrentSpeedMultiplier = outsideSpeedMultiplier;
                if (_audioSource != null) _audioSource.pitch = 1f;
            }
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
            // 中心付近には出さず、minSpawnRadiusRatio〜1.0のリング上に出す。
            // 「取りに行く = フライパンの端に寄る = 落ちるリスクを取る」構造を作るため。
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            float r = Random.Range(spawnRadius * minSpawnRadiusRatio, spawnRadius);

            targetLocalPosition = new Vector3(dir.x * r, heightOnPan, dir.y * r);
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

        [Tooltip("この難易度でスイートスポットを使うか。DifficultySelectorが設定する。\n" +
                 "falseにすると出現しなくなり、時間を早送りする手段が無い純粋な耐久になる")]
        public bool spotEnabled = true;

        /// <summary>PLAYINGに入った瞬間にGameManager.StartGame()から呼ぶ。ここでのみ表示される。</summary>
        public void ActivateSpot()
        {
            if (!spotEnabled)
            {
                // スポットを出さない設定で、かつ外の進行倍率が0だと
                // 孵化する手段が無くなり詰む。その場合だけ通常進行に戻す。
                if (outsideSpeedMultiplier <= 0f)
                {
                    Debug.LogWarning($"[{nameof(SweetSpotController)}] spotEnabled=false かつ " +
                                     "outsideSpeedMultiplier=0 では孵化できないため、外の進行を1.0に戻します。", this);
                    outsideSpeedMultiplier = 1f;
                }
                DeactivateSpot();
                return;
            }

            gameObject.SetActive(true);
            SetNewTargetPosition();
            // リング上に出るようになったので、いきなりそこへワープすると目立つ。
            // 中心から出現させてMoveTowardsで寄っていく形にする。
            transform.localPosition = new Vector3(0f, heightOnPan, 0f);
            timer = 0f;
            _pulseTime = 0f;
            _dwellTime = 0f;
            CurrentSpeedMultiplier = 1f;
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
            _dwellTime = 0f;
            // PLAYING外(タイトル/リザルト)ではタイマー自体が止まっているので値は影響しないが、
            // 外にいる扱いなので外の倍率に合わせておく
            CurrentSpeedMultiplier = outsideSpeedMultiplier;

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            gameObject.SetActive(false);
        }
    }
}