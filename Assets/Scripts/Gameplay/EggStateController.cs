using UnityEngine;

namespace Gameplay
{
    public class EggStateController : MonoBehaviour
    {
        [SerializeField] private float crackProgress = 0f;
        private bool isBroken = false;
        private bool isHatched = false;

        public void SetCrackProgress(float progress)
        {
            crackProgress = Mathf.Clamp01(progress);
        
            // TODO: C担当が用意するRenderer/Material/Shaderへの視覚表現の接続先が必要
            // ※存在しないShader Property名を推測で作ってはいけないルールのため、ここでは値の保持のみ
        }

        public void Break()
        {
            if (isBroken) return;
            isBroken = true;
            Debug.Log("[EggStateController] 卵が割れました！");
        }

        public void Hatch()
        {
            if (isHatched) return;
            isHatched = true;
            Debug.Log("[EggStateController] 卵が孵化しました！");
        }

        public void ResetState()
        {
            crackProgress = 0f;
            isBroken = false;
            isHatched = false;
        }
    }
}