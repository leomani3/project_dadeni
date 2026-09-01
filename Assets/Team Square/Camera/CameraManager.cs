using MyBox;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private CinemachineCamera _virtualCamera;

    public Camera MainCam => mainCam;
    public CinemachineCamera VirtualCamera => _virtualCamera;

    public void RegisterMainCamera(Camera sceneCamera)
    {
        if (sceneCamera == null) return;

        if (mainCam != null && mainCam != sceneCamera)
            Destroy(mainCam.gameObject);

        if (_virtualCamera != null)
        {
            _virtualCamera.transform.position = sceneCamera.transform.position;
            _virtualCamera.transform.rotation = sceneCamera.transform.rotation;
        }

        mainCam = sceneCamera;
        mainCam.transform.SetParent(transform);
    }
}
