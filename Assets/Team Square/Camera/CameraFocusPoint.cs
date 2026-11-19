using UnityEngine;

public class CameraFocusPoint : MonoBehaviour
{
    private void Awake()
    {
        CameraManager.Instance.FocusOn(transform);
    }
}
