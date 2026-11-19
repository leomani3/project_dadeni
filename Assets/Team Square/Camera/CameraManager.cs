using MyBox;
using Sirenix.OdinInspector;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField, Required] private Camera mainCam;
    [SerializeField, Required] private Transform _focusAnchor;

    [Header("Focus Movement")]
    [SerializeField] private float _focusSmoothTime = 0.25f;
    [SerializeField] private float _maxFocusSpeed = 25f;

    private Transform _focusTarget;
    private bool _hasFocus;
    private Vector3 _focusVelocity;

    public Camera MainCam => mainCam;

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

        if (_hasFocus) return;

        _hasFocus = true;
        _focusAnchor.position = _target.position;
    }

    private void LateUpdate()
    {
        if (_focusTarget == null) return;

        _focusAnchor.position = Vector3.SmoothDamp(_focusAnchor.position, _focusTarget.position, ref _focusVelocity, _focusSmoothTime, _maxFocusSpeed);
    }
}
