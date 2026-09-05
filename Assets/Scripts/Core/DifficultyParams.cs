namespace Proto.Core
{
    // [System.Serializable] が無いとUnityがこの構造体をシリアライズしないため、
    // これを持つフィールド(EggController.difficulty)はInspectorに表示されず、
    // 実行時も全フィールドが0のまま始まってしまう。
    // maxLinearSpeed=0でゲームが始まるとEggController.LimitVelocity()が
    // 卵の水平速度を毎FixedUpdateゼロにクランプするため、必ず付けておくこと。
    [System.Serializable]
    public struct DifficultyParams
    {
        public float forceMultiplier;
        public float forceInterval;
        public float directionVariance;
        public float maxLinearSpeed;
        public float maxAngularSpeed;
    }
}