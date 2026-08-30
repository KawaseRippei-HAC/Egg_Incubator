using UnityEngine;

/// <summary>
/// GameManagerの_groundImpactDetectorは単一の参照フィールドのため、
/// 複数箇所(Plane・demosceneのtable等)に置いたGroundImpactDetectorのうち
/// 1つしかGameManagerへ通知できない。
/// このスクリプトは追加のGroundImpactDetectorインスタンスの発火を
/// 同じGameManager.HandleGroundImpact()へ中継するだけの薄いアダプタで、
/// GameManager.cs/GroundImpactDetector.cs自体は一切変更しない。
/// </summary>
public class GroundImpactRelay : MonoBehaviour
{
    public GroundImpactDetector detector;
    public Gameplay.GameManager gameManager;

    private void OnEnable()
    {
        if (detector != null && gameManager != null)
        {
            detector.OnGroundImpact += gameManager.HandleGroundImpact;
        }
    }

    private void OnDisable()
    {
        if (detector != null && gameManager != null)
        {
            detector.OnGroundImpact -= gameManager.HandleGroundImpact;
        }
    }
}
