using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Deckbuilder.Grid
{
    public class ArenaGrid : MonoBehaviour
    {
        private const float MinimumCellGap = 0.001f;

        [SerializeField] private Transform m_cellsRoot;

        private readonly Dictionary<Vector2Int, GridCell> m_cellsByCoordinate = new();
        private readonly Dictionary<CellType, List<GridCell>> m_cellsByType = new();
        private readonly List<GridCell> m_cells = new();
        private readonly Dictionary<HighlightLayer, HashSet<GridCell>> m_highlightedCellsByLayer = new();
        private readonly HashSet<GridCell> m_highlightBuffer = new();

        public IReadOnlyList<GridCell> Cells => m_cells;
        public float CellSize { get; private set; } = 1f;
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

            CellSize = MeasureCellSpacing();

            foreach (GridCell _cell in m_cells)
            {
                _cell.SetCoordinate(PositionToCoordinate(_cell.transform.position));

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

            ValidateGridContinuity();
        }

        private float MeasureCellSpacing()
        {
            List<float> _localX = new List<float>(m_cells.Count);
            List<float> _localZ = new List<float>(m_cells.Count);

            foreach (GridCell _cell in m_cells)
            {
                Vector3 _localPosition = CellsRoot.InverseTransformPoint(_cell.transform.position);
                _localX.Add(_localPosition.x);
                _localZ.Add(_localPosition.z);
            }

            float _spacing = Mathf.Min(GetSmallestGap(_localX), GetSmallestGap(_localZ));

            if (float.IsPositiveInfinity(_spacing))
            {
                this.LogError($"Could not measure a cell spacing from {m_cells.Count} cells, they all share the same position.");
                return 1f;
            }

            return _spacing;
        }

        private static float GetSmallestGap(List<float> _positions)
        {
            _positions.Sort();

            float _smallestGap = float.PositiveInfinity;

            for (int _i = 1; _i < _positions.Count; _i++)
            {
                float _gap = _positions[_i] - _positions[_i - 1];

                if (_gap > MinimumCellGap)
                    _smallestGap = Mathf.Min(_smallestGap, _gap);
            }

            return _smallestGap;
        }

        private void ValidateGridContinuity()
        {
            if (m_cells.Count < 2)
                return;

            foreach (GridCell _cell in m_cells)
            {
                if (!HasOrthogonalNeighbor(_cell))
                    this.LogError($"{_cell.name} at coordinate {_cell.Coordinate} has no orthogonal neighbour, movement cannot path through it.");
            }
        }

        private static bool HasOrthogonalNeighbor(GridCell _cell)
        {
            foreach (GridDirection _direction in GridDirectionUtility.Orthogonal)
            {
                if (_cell.GetNeighbor(_direction) != null)
                    return true;
            }

            return false;
        }

        public Vector2Int PositionToCoordinate(Vector3 _worldPosition)
        {
            Vector3 _localPosition = CellsRoot.InverseTransformPoint(_worldPosition);
            return new Vector2Int(
                Mathf.RoundToInt(_localPosition.x / CellSize),
                Mathf.RoundToInt(_localPosition.z / CellSize));
        }

        public Vector3 CoordinateToPosition(Vector2Int _coordinate)
        {
            return CellsRoot.TransformPoint(new Vector3(_coordinate.x * CellSize, 0f, _coordinate.y * CellSize));
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

        public void SetHighlightLayer(HighlightLayer _layer, IEnumerable<GridCell> _cells)
        {
            HashSet<GridCell> _highlightedCells = GetHighlightedCells(_layer);

            m_highlightBuffer.Clear();
            foreach (GridCell _cell in _cells)
                m_highlightBuffer.Add(_cell);

            foreach (GridCell _cell in _highlightedCells)
            {
                if (!m_highlightBuffer.Contains(_cell))
                    _cell.SetHighlight(_layer, false);
            }

            foreach (GridCell _cell in m_highlightBuffer)
            {
                if (!_highlightedCells.Contains(_cell))
                    _cell.SetHighlight(_layer, true);
            }

            _highlightedCells.Clear();
            _highlightedCells.UnionWith(m_highlightBuffer);
        }

        public void ClearHighlightLayer(HighlightLayer _layer)
        {
            HashSet<GridCell> _highlightedCells = GetHighlightedCells(_layer);

            foreach (GridCell _cell in _highlightedCells)
                _cell.SetHighlight(_layer, false);

            _highlightedCells.Clear();
        }

        public void ClearAllHighlights()
        {
            foreach (HashSet<GridCell> _highlightedCells in m_highlightedCellsByLayer.Values)
                _highlightedCells.Clear();

            foreach (GridCell _cell in m_cells)
                _cell.HideAllHighlights();
        }

        private HashSet<GridCell> GetHighlightedCells(HighlightLayer _layer)
        {
            if (!m_highlightedCellsByLayer.TryGetValue(_layer, out HashSet<GridCell> _highlightedCells))
                m_highlightedCellsByLayer[_layer] = _highlightedCells = new HashSet<GridCell>();

            return _highlightedCells;
        }

        public bool MoveEntity(Entity _entity, GridCell _targetCell)
        {
            if (_targetCell == null || _targetCell.IsOccupied)
                return false;

            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.CurrentCell?.ClearOccupant();

            return _targetCell.TrySetOccupant(_entity);
        }
    }
}
