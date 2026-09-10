using System;
using UnityEngine;
using UnityEngine.AI;
using Utils;

[RequireComponent(typeof(NavMeshAgent))]
public class EntityRoomMoverModule : EntityModule
{
    private const float MovingVelocityThreshold = 0.1f;
    private const float ArrivalVelocityThreshold = 0.01f;
    private const float NavMeshSampleDistance = 2f;

    [SerializeField] private NavMeshAgent m_agent;

    private EntityAnimationModule m_animationModule;
    private Action m_onDestinationReached;
    private bool m_hasDestination;

    public bool IsMoving => m_hasDestination;
    public bool CanMove => enabled && m_agent.isActiveAndEnabled && m_agent.isOnNavMesh;
    public Vector3 Destination => m_agent.destination;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out m_animationModule);

        m_hasDestination = false;
        m_onDestinationReached = null;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        StopMoving();
        enabled = true;
    }

    public override void OnCombatEnter()
    {
        base.OnCombatEnter();

        StopMoving();

        m_agent.enabled = false;
        Owner.Collider.enabled = false;
        enabled = false;
    }

    public override void OnCombatExit()
    {
        base.OnCombatExit();

        m_agent.enabled = true;

        if (!m_agent.Warp(transform.position))
            this.LogWarning($"Could not place the agent back on the NavMesh at {transform.position}.");

        Owner.Collider.enabled = true;
        enabled = true;
    }

    public bool MoveTo(Vector3 _worldPosition, Action _onDestinationReached = null)
    {
        return MoveTo(_worldPosition, 0f, _onDestinationReached);
    }

    public bool MoveTo(Vector3 _worldPosition, float _stopDistance, Action _onDestinationReached = null)
    {
        if (!CanMove)
            return false;

        if (!NavMesh.SamplePosition(_worldPosition, out NavMeshHit _hit, NavMeshSampleDistance, NavMesh.AllAreas))
            return false;

        m_agent.stoppingDistance = _stopDistance;
        m_agent.isStopped = false;

        if (!m_agent.SetDestination(_hit.position))
            return false;

        m_onDestinationReached = _onDestinationReached;
        m_hasDestination = true;

        return true;
    }

    public void StopMoving()
    {
        m_hasDestination = false;
        m_onDestinationReached = null;

        if (m_agent.isActiveAndEnabled && m_agent.isOnNavMesh)
            m_agent.ResetPath();
    }

    private void Reset()
    {
        m_agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        UpdateLocomotionAnimation();
        UpdateDestination();
    }

    private void UpdateLocomotionAnimation()
    {
        if (m_animationModule == null)
            return;

        m_animationModule.SetLocomotionSpeed(m_agent.velocity.magnitude > MovingVelocityThreshold ? 1f : 0f);
    }

    private void UpdateDestination()
    {
        if (!m_hasDestination || m_agent.pathPending)
            return;

        if (m_agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            this.LogWarning($"No complete path to {m_agent.destination}, movement aborted.");
            StopMoving();
            return;
        }

        if (m_agent.remainingDistance > m_agent.stoppingDistance)
            return;

        if (m_agent.hasPath && m_agent.velocity.sqrMagnitude > ArrivalVelocityThreshold)
            return;

        ReachDestination();
    }

    private void ReachDestination()
    {
        Action _onDestinationReached = m_onDestinationReached;

        StopMoving();

        _onDestinationReached?.Invoke();
    }
}
