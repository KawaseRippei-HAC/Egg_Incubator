using UnityEngine;

/// <summary>
/// このオブジェクトを常にメインカメラの方を向かせる(ビルボード)。
/// EggRadialBurstのような、常に画面に正対して見せたい2D的なエフェクト板に使う。
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    private Camera _targetCamera;

    private void OnEnable()
    {
        _targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
            if (_targetCamera == null) return;
        }

        // カメラの方を向く(奥行きの回転のみ、カメラのUpに合わせる)
        transform.rotation = _targetCamera.transform.rotation;
    }
}
