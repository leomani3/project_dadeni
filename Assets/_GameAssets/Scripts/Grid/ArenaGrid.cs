using System.Collections.Generic;
using Deckbuilder.Grid.Highlighting;
using UnityEngine;
using Utils;

namespace Deckbuilder.Grid
{
    public class ArenaGrid : MonoBehaviour
    {
        [SerializeField] private float m_cellSize = 1f;
        [SerializeField] private Transform m_cellsRoot;

        private readonly Dictionary<Vector2Int, GridCell> m_cellsByCoordinate = new();
        private readonly Dictionary<CellType, List<GridCell>> m_cellsByType = new();
        private readonly List<GridCell> m_cells = new();

        public IReadOnlyList<GridCell> Cells => m_cells;
        public float CellSize => m_cellSize;
        public Transform CellsRoot => m_cellsRoot != null ? m_cellsRoot : transform;

        public bool IsBuilt => m_cells.Count > 0;

        public void BuildIfNeeded()
        {
            if (!IsBuilt)
                Build();
        }

        public void Build()
        {
            m_cellsByCoordinate.Clear();
            m_cellsByType.Clear();
            m_cells.Clear();
            CellsRoot.GetComponentsInChildren(true, m_cells);

            foreach (GridCell _cell in m_cells)
            {
                _cell.SetCoordinate(PositionToCoordinate(_cell.transform.position));
                _cell.HighlightVisual.SetCellSize(m_cellSize);

                if (!m_cellsByCoordinate.TryAdd(_cell.Coordinate, _cell))
                    this.LogError($"Duplicate grid coordinate {_cell.Coordinate} found on {_cell.name}.");

                if (_cell.CellType != CellType.Empty)
                    GetCellListOfType(_cell.CellType).Add(_cell);
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

        public Vector2Int PositionToCoordinate(Vector3 _worldPosition)
        {
            Vector3 _localPosition = CellsRoot.InverseTransformPoint(_worldPosition);
            return new Vector2Int(
                Mathf.RoundToInt(_localPosition.x / m_cellSize),
                Mathf.RoundToInt(_localPosition.z / m_cellSize));
        }

        public Vector3 CoordinateToPosition(Vector2Int _coordinate)
        {
            return CellsRoot.TransformPoint(new Vector3(_coordinate.x * m_cellSize, 0f, _coordinate.y * m_cellSize));
        }

        public IReadOnlyList<GridCell> GetCellsOfType(CellType _cellType)
        {
            return GetCellListOfType(_cellType);
        }

        private List<GridCell> GetCellListOfType(CellType _cellType)
        {
            if (!m_cellsByType.TryGetValue(_cellType, out List<GridCell> _cells))
                m_cellsByType[_cellType] = _cells = new List<GridCell>();

            return _cells;
        }

        public IReadOnlyList<GridCell> GetAllySpawnCells()
        {
            return GetCellsOfType(CellType.AllySpawn);
        }

        public IReadOnlyList<GridCell> GetEnemySpawnCells()
        {
            return GetCellsOfType(CellType.EnemySpawn);
        }

        public List<GridCell> GetFreeSpawnCells(CellType _spawnCellType, bool _randomOrder = false)
        {
            List<GridCell> _freeCells = new();

            foreach (GridCell _cell in GetCellsOfType(_spawnCellType))
            {
                if (!_cell.IsOccupied)
                    _freeCells.Add(_cell);
            }

            if (_randomOrder)
                Shuffle(_freeCells);

            return _freeCells;
        }

        public GridCell GetFreeSpawnCell(CellType _spawnCellType, bool _random = false)
        {
            List<GridCell> _freeCells = GetFreeSpawnCells(_spawnCellType, _random);
            return _freeCells.Count > 0 ? _freeCells[0] : null;
        }

        private static void Shuffle(List<GridCell> _cells)
        {
            for (int _i = _cells.Count - 1; _i > 0; _i--)
            {
                int _swapIndex = Random.Range(0, _i + 1);
                (_cells[_i], _cells[_swapIndex]) = (_cells[_swapIndex], _cells[_i]);
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
