using Stats;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Utils;

[RequireComponent(typeof(Rigidbody))]
public class EntityClickMoveModule : EntityModule
{
    private const float RotationLerpSpeed = 20f;

    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private LayerMask m_groundLayerMask = ~0;
    [SerializeField] private float m_fallbackMoveSpeed = 4f;

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

        if (m_rigidbody == null)
        {
            this.LogError("No Rigidbody assigned, the character will not move nor collide with anything.");
            return;
        }

        m_rigidbody.freezeRotation = true;
        m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        m_destination = m_rigidbody.position;
        m_hasDestination = false;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        StopMoving();
    }

    public void MoveTo(Vector3 _worldPosition)
    {
        if (m_rigidbody == null)
            return;

        m_destination = new Vector3(_worldPosition.x, m_rigidbody.position.y, _worldPosition.z);
        m_hasDestination = true;
    }

    public void StopMoving()
    {
        m_hasDestination = false;

        if (m_rigidbody != null)
            m_rigidbody.linearVelocity = new Vector3(0f, m_rigidbody.linearVelocity.y, 0f);
    }

    private void Reset()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Owner == null || m_rigidbody == null)
            return;

        ReadMoveInput();
    }

    private void FixedUpdate()
    {
        if (Owner == null || m_rigidbody == null)
            return;

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

        if (Mouse.current.leftButton.wasPressedThisFrame)
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
        Vector3 _toDestination = m_destination - m_rigidbody.position;
        _toDestination.y = 0f;

        float _remainingDistance = _toDestination.magnitude;

        if (!m_hasDestination || _remainingDistance <= 0.01f)
        {
            if (m_hasDestination)
                StopMoving();

            if (m_animationModule != null)
                m_animationModule.SetLocomotionSpeed(0f);

            return;
        }

        Vector3 _direction = _toDestination / _remainingDistance;
        float _speed = Mathf.Min(GetMoveSpeed(), _remainingDistance / Time.fixedDeltaTime);

        m_rigidbody.linearVelocity = new Vector3(_direction.x * _speed, m_rigidbody.linearVelocity.y, _direction.z * _speed);
        m_rigidbody.MoveRotation(Quaternion.Slerp(m_rigidbody.rotation, Quaternion.LookRotation(_direction), Time.fixedDeltaTime * RotationLerpSpeed));

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
