using System;
using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using Stats;
using UnityEngine;

public class EntityCombatMoverModule : EntityModule
{
    public Action<int, int> OnMovementPointsChanged;

    [SerializeField] private float m_moveSpeed = 5f;

    public CombatManager CombatManager { get; private set; }
    public ArenaGrid Grid => CombatManager != null ? CombatManager.Grid : null;
    public GridCell CurrentCell { get; private set; }
    public GridCell DestinationCell { get; private set; }
    public bool IsMoving { get; private set; }
    public int RemainingMovementPoints { get; private set; }
    public int MaxMovementPoints { get; private set; }

    public GridCell EffectiveCell => IsMoving && DestinationCell != null ? DestinationCell : CurrentCell;

    private readonly List<GridCell> m_path = new();
    private EntityAnimationModule m_animationModule;
    private EntityStatModule m_statModule;
    private Coroutine m_moveRoutine;
    private bool m_cancelMoveRequested;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out m_animationModule);
        Owner.TryGetModule(out m_statModule);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        int _max = Mathf.RoundToInt(m_statModule.GetValue(StatType.MovementPoints));
        SetMovementPoints(_max, _max);
    }

    public override void OnCombatExit()
    {
        base.OnCombatExit();

        ResetCombatState();
    }

    public void SetCombatManager(CombatManager _combatManager)
    {
        CombatManager = _combatManager;
    }

    public void SetCurrentCell(GridCell _cell)
    {
        CurrentCell = _cell;
    }

    public void FaceTowards(Vector3 _position)
    {
        Vector3 _direction = _position - transform.position;
        _direction.y = 0f;

        if (_direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(ToCardinalDirection(_direction));
    }

    private static Vector3 ToCardinalDirection(Vector3 _direction)
    {
        if (Mathf.Abs(_direction.x) >= Mathf.Abs(_direction.z))
            return _direction.x >= 0f ? Vector3.right : Vector3.left;

        return _direction.z >= 0f ? Vector3.forward : Vector3.back;
    }

    public bool MoveTo(GridCell _destination, bool _ignoreOccupants = false, bool _randomizePath = false)
    {
        if (IsMoving || _destination == null || Grid == null || CurrentCell == null)
            return false;

        return MoveAlong(Grid.FindPath(CurrentCell, _destination, _ignoreOccupants, _randomizePath));
    }

    public bool MoveAlong(IReadOnlyList<GridCell> _path)
    {
        if (IsMoving || Grid == null || CurrentCell == null)
            return false;

        if (_path == null || _path.Count < 2 || _path[0] != CurrentCell)
            return false;

        int _movementCost = _path.Count - 1;
        if (_movementCost > RemainingMovementPoints)
            return false;

        m_path.Clear();
        m_path.AddRange(_path);

        SetMovementPoints(RemainingMovementPoints - _movementCost, MaxMovementPoints);
        DestinationCell = m_path[^1];
        m_cancelMoveRequested = false;
        m_moveRoutine = StartCoroutine(FollowPath());
        return true;
    }

    public void CancelMove()
    {
        if (IsMoving)
            m_cancelMoveRequested = true;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        OnMovementPointsChanged = null;

        if (CurrentCell != null && CurrentCell.Occupant == Owner)
            CurrentCell.ClearOccupant();

        ResetCombatState();
    }

    private void ResetCombatState()
    {
        if (m_moveRoutine != null)
        {
            StopCoroutine(m_moveRoutine);
            m_moveRoutine = null;
        }

        CombatManager = null;
        CurrentCell = null;
        DestinationCell = null;
        IsMoving = false;
        m_cancelMoveRequested = false;
        SetMovementPoints(0, 0);
    }

    private void SetMovementPoints(int _remaining, int _max)
    {
        RemainingMovementPoints = _remaining;
        MaxMovementPoints = _max;
        OnMovementPointsChanged?.Invoke(RemainingMovementPoints, MaxMovementPoints);
    }

    private IEnumerator FollowPath()
    {
        IsMoving = true;
        m_animationModule.SetLocomotionSpeed(1f);

        for (int _i = 1; _i < m_path.Count; _i++)
        {
            GridCell _targetCell = m_path[_i];
            yield return MoveToCell(_targetCell);
            Grid.MoveEntity(Owner, _targetCell);

            if (m_cancelMoveRequested)
            {
                SetMovementPoints(RemainingMovementPoints + m_path.Count - 1 - _i, MaxMovementPoints);
                break;
            }
        }

        m_animationModule.SetLocomotionSpeed(0f);

        IsMoving = false;
        DestinationCell = null;
        m_cancelMoveRequested = false;
        m_moveRoutine = null;
    }

    private IEnumerator MoveToCell(GridCell _targetCell)
    {
        Vector3 _startPosition = transform.position;
        Vector3 _endPosition = _targetCell.transform.position;

        Vector3 _direction = _endPosition - _startPosition;
        _direction.y = 0f;
        Quaternion _targetRotation = _direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(_direction) : transform.rotation;

        float _distance = Vector3.Distance(_startPosition, _endPosition);
        float _duration = _distance / Mathf.Max(m_moveSpeed, 0.01f);
        float _elapsed = 0f;

        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float _t = Mathf.Clamp01(_elapsed / _duration);

            transform.position = Vector3.Lerp(_startPosition, _endPosition, _t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * 20);

            yield return null;
        }

        transform.position = _endPosition;
    }
}
