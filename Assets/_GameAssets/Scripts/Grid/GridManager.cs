using System.Collections.Generic;
using MyBox;
using UnityEngine;
using Utils;

namespace Deckbuilder.Grid
{
    public class GridManager : Singleton<GridManager>
    {
        [SerializeField] private float m_cellSize = 1f;
        [SerializeField] private GridBuilder m_builder;

        private readonly Dictionary<Vector2Int, GridCell> m_cellsByCoordinate = new();
        private readonly List<GridCell> m_cells = new();

        public IReadOnlyList<GridCell> Cells => m_cells;
        public float CellSize => m_cellSize;

        public void Init()
        {
            if (m_builder != null)
                m_builder.EnsureBuilt();

            BuildGrid();
        }

        public static Vector2Int PositionToCoordinate(Vector3 _position)
        {
            float _size = Instance != null ? Instance.m_cellSize : 1f;
            return new Vector2Int(
                Mathf.RoundToInt(_position.x / _size),
                Mathf.RoundToInt(_position.z / _size));
        }

        public Vector3 CoordinateToPosition(Vector2Int _coordinate)
        {
            return new Vector3(_coordinate.x * m_cellSize, 0f, _coordinate.y * m_cellSize);
        }

        private void BuildGrid()
        {
            m_cellsByCoordinate.Clear();
            m_cells.Clear();
            m_cells.AddRange(FindObjectsByType<GridCell>(FindObjectsSortMode.None));

            foreach (GridCell _cell in m_cells)
            {
                if (!m_cellsByCoordinate.TryAdd(_cell.Coordinate, _cell))
                    this.LogError($"Duplicate grid coordinate {_cell.Coordinate} found on {_cell.name}.");
            }

            foreach (GridCell _cell in m_cells)
            {
                _cell.ClearNeighbors();
                foreach (GridDirection _direction in GridDirectionUtility.All)
                {
                    Vector2Int _neighborCoordinate = _cell.Coordinate + GridDirectionUtility.GetOffset(_direction);
                    if (m_cellsByCoordinate.TryGetValue(_neighborCoordinate, out GridCell _neighbor))
                        _cell.SetNeighbor(_direction, _neighbor);
                }
            }
        }

        public GridCell GetCell(Vector2Int _coordinate)
        {
            return m_cellsByCoordinate.TryGetValue(_coordinate, out GridCell _cell) ? _cell : null;
        }

        public GridCell GetCell(float _x, float _y)
        {
            return GetCell(new Vector2Int(Mathf.RoundToInt(_x), Mathf.RoundToInt(_y)));
        }

        public IEnumerable<GridCell> GetCellsInZone(Vector2Int _origin, GridShape _shape, int _size, int _minSize = 0)
        {
            foreach (Vector2Int _coordinate in GridShapeUtility.GetCoordinates(_origin, _shape, _size, _minSize))
            {
                if (m_cellsByCoordinate.TryGetValue(_coordinate, out GridCell _cell))
                    yield return _cell;
            }
        }

        public int GetManhattanDistance(GridCell _a, GridCell _b)
        {
            return GridShapeUtility.ManhattanDistance(_a.Coordinate, _b.Coordinate);
        }

        public List<GridCell> FindPath(GridCell _start, GridCell _goal, bool _ignoreOccupants = false, bool _randomize = false)
        {
            return GridPathfinder.FindPath(_start, _goal, GetCell, _ignoreOccupants, _randomize);
        }

        public List<GridCell> GetReachableCells(GridCell _start, int _maxSteps, bool _ignoreOccupants = false)
        {
            return GridPathfinder.GetReachableCells(_start, _maxSteps, GetCell, _ignoreOccupants);
        }

        public bool HasLineOfSight(GridCell _from, GridCell _to, out GridCell _blockingCell, Entity _ignoreOccupant = null)
        {
            return GridLineOfSight.HasLineOfSight(_from, _to, GetCell, out _blockingCell, _ignoreOccupant);
        }

        public bool MoveEntity(Entity _entity, GridCell _targetCell)
        {
            if (_targetCell == null || _targetCell.IsOccupied)
                return false;

            if (_entity.TryGetModule(out EntityGridModule _gridModule))
                _gridModule.CurrentCell?.ClearOccupant();

            return _targetCell.TrySetOccupant(_entity);
        }
    }
}
