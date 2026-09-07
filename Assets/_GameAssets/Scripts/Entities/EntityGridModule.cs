using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using UnityEngine;

public class EntityGridModule : EntityModule
{
    [SerializeField] private float m_moveSpeed = 5f;
    [SerializeField] private float m_rotationSpeed = 10f;

    public Arena Arena { get; private set; }
    public ArenaGrid Grid => Arena != null ? Arena.Grid : null;
    public GridCell CurrentCell { get; private set; }
    public GridCell DestinationCell { get; private set; }
    public bool IsMoving { get; private set; }

    public GridCell EffectiveCell => IsMoving && DestinationCell != null ? DestinationCell : CurrentCell;

    private Coroutine m_moveRoutine;
    private bool m_cancelMoveRequested;

    public void SetArena(Arena _arena)
    {
        Arena = _arena;
    }

    public void SetCurrentCell(GridCell _cell)
    {
        CurrentCell = _cell;
    }

    public void FaceTowards(Vector3 _position)
    {
        Vector3 _direction = _position - transform.position;
        _direction.y = 0f;

        if (_direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    public bool MoveTo(GridCell _destination, bool _ignoreOccupants = false, bool _randomizePath = false)
    {
        if (IsMoving || _destination == null || Grid == null || CurrentCell == null)
            return false;

        List<GridCell> _path = Grid.FindPath(CurrentCell, _destination, _ignoreOccupants, _randomizePath);
        if (_path == null || _path.Count < 2)
            return false;

        DestinationCell = _destination;
        m_cancelMoveRequested = false;
        m_moveRoutine = StartCoroutine(FollowPath(_path));
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

        if (m_moveRoutine != null)
        {
            StopCoroutine(m_moveRoutine);
            m_moveRoutine = null;
        }

        if (CurrentCell != null && CurrentCell.Occupant == Owner)
            CurrentCell.ClearOccupant();

        Arena = null;
        CurrentCell = null;
        DestinationCell = null;
        IsMoving = false;
        m_cancelMoveRequested = false;
    }

    private IEnumerator FollowPath(List<GridCell> _path)
    {
        IsMoving = true;

        for (int _i = 1; _i < _path.Count; _i++)
        {
            GridCell _targetCell = _path[_i];
            yield return MoveToCell(_targetCell);
            Grid.MoveEntity(Owner, _targetCell);

            if (m_cancelMoveRequested)
                break;
        }

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
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * m_rotationSpeed);

            yield return null;
        }

        transform.position = _endPosition;
    }
}
