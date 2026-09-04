using Stats;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class EntityClickMoveModule : EntityModule
{
    private const float RotationLerpSpeed = 20f;

    [SerializeField] private LayerMask m_groundLayerMask = ~0;
    [SerializeField] private float m_fallbackMoveSpeed = 4f;
    [SerializeField] private bool m_playClickFeedback = true;

    private EntityStatModule m_statModule;
    private EntityAnimationModule m_animationModule;
    private Vector3 m_destination;
    private bool m_hasDestination;

    public bool IsMoving => m_hasDestination;
    public Vector3 Destination => m_destination;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out m_statModule);
        Owner.TryGetModule(out m_animationModule);

        m_destination = transform.position;
        m_hasDestination = false;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        m_hasDestination = false;
    }

    public void MoveTo(Vector3 _worldPosition)
    {
        m_destination = new Vector3(_worldPosition.x, transform.position.y, _worldPosition.z);
        m_hasDestination = true;
    }

    public void StopMoving()
    {
        m_hasDestination = false;
    }

    private void Update()
    {
        if (Owner == null)
            return;

        ReadMoveInput();
        AdvanceTowardsDestination();
    }

    private void ReadMoveInput()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!TryGetGroundPointUnderCursor(out Vector3 _groundPoint))
            return;

        MoveTo(_groundPoint);

        if (m_playClickFeedback && Mouse.current.leftButton.wasPressedThisFrame)
            PlayClickFeedback(_groundPoint);
    }

    private void PlayClickFeedback(Vector3 _groundPoint)
    {
        if (ClickFeedbackManager.Instance != null)
            ClickFeedbackManager.Instance.PlayMove(_groundPoint);
    }

    private bool TryGetGroundPointUnderCursor(out Vector3 _groundPoint)
    {
        _groundPoint = Vector3.zero;

        Camera _camera = CameraManager.Instance != null ? CameraManager.Instance.MainCam : Camera.main;
        if (_camera == null)
            return false;

        Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(_ray, out RaycastHit _hit, Mathf.Infinity, m_groundLayerMask))
            return false;

        if (Owner.Collider != null && _hit.collider == Owner.Collider)
            return false;

        _groundPoint = _hit.point;
        return true;
    }

    private void AdvanceTowardsDestination()
    {
        Vector3 _toDestination = m_destination - transform.position;
        _toDestination.y = 0f;

        float _remainingDistance = _toDestination.magnitude;

        if (!m_hasDestination || _remainingDistance <= 0.0001f)
        {
            m_hasDestination = false;

            if (m_animationModule != null)
                m_animationModule.SetLocomotionSpeed(0f);

            return;
        }

        Vector3 _direction = _toDestination / _remainingDistance;
        float _step = Mathf.Min(GetMoveSpeed() * Time.deltaTime, _remainingDistance);

        transform.position += _direction * _step;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_direction), Time.deltaTime * RotationLerpSpeed);

        if (m_animationModule != null)
            m_animationModule.SetLocomotionSpeed(1f);
    }

    private float GetMoveSpeed()
    {
        if (m_statModule == null || StatManager.Instance == null)
            return m_fallbackMoveSpeed;

        float _statSpeed = m_statModule.GetValue(StatType.MoveSpeed);
        return _statSpeed > 0f ? _statSpeed : m_fallbackMoveSpeed;
    }
}
