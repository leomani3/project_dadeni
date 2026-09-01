using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRegister : MonoBehaviour
{
    [SerializeField] private Camera _sceneCamera;

    private void Awake()
    {
        if (CameraManager.Instance != null)
            CameraManager.Instance.RegisterMainCamera(_sceneCamera);
    }

    private void Reset()
    {
        _sceneCamera = GetComponent<Camera>();
    }
}
