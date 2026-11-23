using MyBox;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField, Required] private Camera mainCam;
    [SerializeField, Required] private CinemachineBrain _brain;

    [Header("Focus Movement")]
    [SerializeField] private float _focusSmoothTime = 0.25f;
    [SerializeField] private float _maxFocusSpeed = 25f;

    private Transform _focusTarget;
    private Transform _drivenAnchor;
    private Vector3 _focusVelocity;

    public Camera MainCam => mainCam;

    private void Reset()
    {
        mainCam = GetComponentInChildren<Camera>();
        _brain = GetComponentInChildren<CinemachineBrain>();
    }

    public void FocusOn(Transform _focusPoint)
    {
        SetFocus(_focusPoint);
    }

    public void FollowTarget(Transform _target)
    {
        SetFocus(_target);
    }

    private void SetFocus(Transform _target)
    {
        _focusTarget = _target;
        _focusVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        Transform _liveAnchor = GetLiveAnchor();

        if (_liveAnchor == null || _focusTarget == null)
        {
            _drivenAnchor = _liveAnchor;
            return;
        }

        if (_liveAnchor != _drivenAnchor)
        {
            _drivenAnchor = _liveAnchor;
            _focusVelocity = Vector3.zero;
            _liveAnchor.position = _focusTarget.position;
            return;
        }

        _liveAnchor.position = Vector3.SmoothDamp(_liveAnchor.position, _focusTarget.position, ref _focusVelocity, _focusSmoothTime, _maxFocusSpeed);
    }

    private Transform GetLiveAnchor()
    {
        CinemachineVirtualCameraBase _liveCamera = _brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;

        return _liveCamera == null ? null : _liveCamera.Follow;
    }
}
