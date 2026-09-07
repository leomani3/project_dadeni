using System;
using Stats;
using UnityEngine;
using Utils;

[RequireComponent(typeof(Rigidbody))]
public class EntityMoveModule : EntityModule
{
    private const float RotationLerpSpeed = 20f;
    private const float MovingVelocityThreshold = 0.1f;
    private const float MinimumArrivalDistance = 0.01f;

    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private float m_fallbackMoveSpeed = 4f;

    private EntityStatModule m_statModule;
    private EntityAnimationModule m_animationModule;
    private Vector3 m_destination;
    private float m_arrivalDistance;
    private Action m_onDestinationReached;
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
        m_onDestinationReached = null;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        StopMoving();
    }

    public void MoveTo(Vector3 _worldPosition, Action _onDestinationReached = null)
    {
        MoveTo(_worldPosition, 0f, _onDestinationReached);
    }

    public void MoveTo(Vector3 _worldPosition, float _stopDistance, Action _onDestinationReached = null)
    {
        if (m_rigidbody == null)
            return;

        m_destination = new Vector3(_worldPosition.x, m_rigidbody.position.y, _worldPosition.z);
        m_arrivalDistance = Mathf.Max(_stopDistance, MinimumArrivalDistance);
        m_onDestinationReached = _onDestinationReached;
        m_hasDestination = true;
    }

    public void StopMoving()
    {
        m_hasDestination = false;
        m_onDestinationReached = null;

        if (m_rigidbody != null)
            m_rigidbody.linearVelocity = new Vector3(0f, m_rigidbody.linearVelocity.y, 0f);
    }

    private void Reset()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Owner == null || m_rigidbody == null)
            return;

        UpdateLocomotionAnimation();
        AdvanceTowardsDestination();
    }

    private void UpdateLocomotionAnimation()
    {
        if (m_animationModule == null)
            return;

        Vector3 _horizontalVelocity = m_rigidbody.linearVelocity;
        _horizontalVelocity.y = 0f;

        m_animationModule.SetLocomotionSpeed(_horizontalVelocity.magnitude > MovingVelocityThreshold ? 1f : 0f);
    }

    private void AdvanceTowardsDestination()
    {
        if (!m_hasDestination)
            return;

        Vector3 _toDestination = m_destination - m_rigidbody.position;
        _toDestination.y = 0f;

        float _remainingDistance = _toDestination.magnitude;

        if (_remainingDistance <= m_arrivalDistance)
        {
            ReachDestination();
            return;
        }

        Vector3 _direction = _toDestination / _remainingDistance;
        float _speed = Mathf.Min(GetMoveSpeed(), (_remainingDistance - m_arrivalDistance) / Time.fixedDeltaTime);

        m_rigidbody.linearVelocity = new Vector3(_direction.x * _speed, m_rigidbody.linearVelocity.y, _direction.z * _speed);
        m_rigidbody.MoveRotation(Quaternion.Slerp(m_rigidbody.rotation, Quaternion.LookRotation(_direction), Time.fixedDeltaTime * RotationLerpSpeed));
    }

    private void ReachDestination()
    {
        Action _onDestinationReached = m_onDestinationReached;

        StopMoving();

        _onDestinationReached?.Invoke();
    }

    private float GetMoveSpeed()
    {
        if (m_statModule == null || StatManager.Instance == null)
            return m_fallbackMoveSpeed;

        float _statSpeed = m_statModule.GetValue(StatType.MoveSpeed);
        return _statSpeed > 0f ? _statSpeed : m_fallbackMoveSpeed;
    }
}
