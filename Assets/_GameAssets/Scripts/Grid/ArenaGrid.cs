using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Deckbuilder.Grid
{
    public class ArenaGrid : MonoBehaviour, IGridTopology
    {
        [SerializeField] private Transform m_cellsRoot;
        [SerializeField, Required] private ArenaLayout m_layout;
        [SerializeField, Required] private GridCell m_cellPrefab;
        [SerializeField, Min(0.01f)] private float m_cellPrefabFootprint = 1f;
        [SerializeField, Required] private Material m_highlightMaterial;
        [SerializeField, Required] private Mesh[] m_tileMeshes;
        [SerializeField, Required] private Mesh[] m_obstacleMeshes;
        [SerializeField, Required] private Material m_lightTileMaterial;
        [SerializeField, Required] private Material m_darkTileMaterial;
        [SerializeField, Required] private Material m_obstacleMaterial;
        [SerializeField] private Color m_tileBaseColor = new(0.784f, 0.682f, 0.494f);
        [SerializeField, Range(0f, 1f)] private float m_tileToneContrast = 0.22f;
        [SerializeField, HideInInspector] private Color m_appliedTileBaseColor;
        [SerializeField, HideInInspector] private float m_appliedTileToneContrast = -1f;

        private const int MeshHashSalt = 1;
        private const int RotationHashSalt = 2;
        private const int ObstacleMeshHashSalt = 3;
        private const int ObstacleRotationHashSalt = 4;
        private const float ObstacleLightnessShift = -0.085f;
        private const float ObstacleChromaScale = 1.06f;
        private const float LightChromaScale = 0.94f;
        private const float DarkChromaScale = 1.07f;
        private const float TemperatureSplit = 0.02f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private readonly Dictionary<Vector2Int, GridCell> m_cellsByCoordinate = new();
        private readonly Dictionary<CellType, List<GridCell>> m_cellsByType = new();
        private readonly List<GridCell> m_cells = new();
        private readonly List<CellHighlightSource> m_highlightSources = new();
        private readonly CellHighlightMeshBuilder m_highlightMeshBuilder = new();
        private readonly List<Vector2Int> m_coordinateBuffer = new();

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

            CellSize = m_layout.CellSize;

#if UNITY_EDITOR
            ApplyTileTones();
#endif
            SpawnCellsFromLayout();
            CellsRoot.GetComponentsInChildren(true, m_cells);

            foreach (GridCell _cell in m_cells)
            {
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

        private void SpawnCellsFromLayout()
        {
            for (int _i = CellsRoot.childCount - 1; _i >= 0; _i--)
                DestroyCellObject(CellsRoot.GetChild(_i));

            float _cellScale = m_layout.CellSize / m_cellPrefabFootprint;

            foreach (ArenaLayout.CellEntry _entry in m_layout.Cells)
            {
                GridCell _cell = InstantiateCell();
                _cell.transform.SetParent(CellsRoot, false);
                _cell.transform.localPosition = m_layout.CoordinateToLocalPosition(_entry.coordinate);
                _cell.transform.localScale = Vector3.one * _cellScale;
                _cell.name = $"Cell {_entry.coordinate.x},{_entry.coordinate.y}";
                _cell.SetCoordinate(_entry.coordinate);
                _cell.SetCellType(_entry.type);
                _cell.SetTileVisual(
                    m_tileMeshes[CoordinateHash(_entry.coordinate, MeshHashSalt) % m_tileMeshes.Length],
                    90f * (CoordinateHash(_entry.coordinate, RotationHashSalt) % 4),
                    IsLightTile(_entry.coordinate) ? m_lightTileMaterial : m_darkTileMaterial);

                _cell.SetObstacleVisual(
                    m_obstacleMeshes[CoordinateHash(_entry.coordinate, ObstacleMeshHashSalt) % m_obstacleMeshes.Length],
                    90f * (CoordinateHash(_entry.coordinate, ObstacleRotationHashSalt) % 4),
                    m_obstacleMaterial);
            }
        }

        private static bool IsLightTile(Vector2Int _coordinate)
        {
            return ((_coordinate.x + _coordinate.y) & 1) == 0;
        }

        private static int CoordinateHash(Vector2Int _coordinate, int _salt)
        {
            unchecked
            {
                int _hash = (_coordinate.x * 73856093) ^ (_coordinate.y * 19349663) ^ (_salt * 83492791);
                _hash = (_hash ^ (_hash >> 13)) * 1274126177;
                return (_hash ^ (_hash >> 16)) & int.MaxValue;
            }
        }

        public void ApplyTileTones()
        {
            DeriveTileTones(m_tileBaseColor, m_tileToneContrast, out Color _light, out Color _dark);
            SetTileMaterialColor(m_lightTileMaterial, _light);
            SetTileMaterialColor(m_darkTileMaterial, _dark);
            SetTileMaterialColor(m_obstacleMaterial, DeriveObstacleTone(_dark));

            m_appliedTileBaseColor = m_tileBaseColor;
            m_appliedTileToneContrast = m_tileToneContrast;
        }

        public static Color DeriveObstacleTone(Color _darkLinear)
        {
            Vector3 _oklab = LinearToOklab(new Vector3(_darkLinear.r, _darkLinear.g, _darkLinear.b));

            return OklabToLinear(new Vector3(
                Mathf.Max(0f, _oklab.x + ObstacleLightnessShift),
                _oklab.y * ObstacleChromaScale,
                _oklab.z * ObstacleChromaScale));
        }

        private static void SetTileMaterialColor(Material _material, Color _linearColor)
        {
            Color _value = _linearColor.gamma;
            bool _changed = false;

            if (_material.HasProperty(BaseColorId) && _material.GetColor(BaseColorId) != _value)
            {
                _material.SetColor(BaseColorId, _value);
                _changed = true;
            }

            if (_material.HasProperty(LegacyColorId) && _material.GetColor(LegacyColorId) != _value)
            {
                _material.SetColor(LegacyColorId, _value);
                _changed = true;
            }

#if UNITY_EDITOR
            if (_changed)
                UnityEditor.EditorUtility.SetDirty(_material);
#endif
        }

        public static void DeriveTileTones(Color _baseColor, float _contrast, out Color _light, out Color _dark)
        {
            Color _linear = _baseColor.linear;
            Vector3 _oklab = LinearToOklab(new Vector3(_linear.r, _linear.g, _linear.b));

            _light = OklabToLinear(new Vector3(
                _oklab.x + _contrast * 0.5f * (1f - _oklab.x),
                _oklab.y * LightChromaScale,
                _oklab.z * LightChromaScale + _contrast * TemperatureSplit));

            _dark = OklabToLinear(new Vector3(
                _oklab.x - _contrast * 0.5f * _oklab.x,
                _oklab.y * DarkChromaScale,
                _oklab.z * DarkChromaScale - _contrast * TemperatureSplit));
        }

        private static Vector3 LinearToOklab(Vector3 _linear)
        {
            float _long = 0.4122214708f * _linear.x + 0.5363325363f * _linear.y + 0.0514459929f * _linear.z;
            float _medium = 0.2119034982f * _linear.x + 0.6806995451f * _linear.y + 0.1073969566f * _linear.z;
            float _short = 0.0883024619f * _linear.x + 0.2817188376f * _linear.y + 0.6299787005f * _linear.z;

            float _longRoot = CubeRoot(_long);
            float _mediumRoot = CubeRoot(_medium);
            float _shortRoot = CubeRoot(_short);

            return new Vector3(
                0.2104542553f * _longRoot + 0.7936177850f * _mediumRoot - 0.0040720468f * _shortRoot,
                1.9779984951f * _longRoot - 2.4285922050f * _mediumRoot + 0.4505937099f * _shortRoot,
                0.0259040371f * _longRoot + 0.7827717662f * _mediumRoot - 0.8086757660f * _shortRoot);
        }

        private static Color OklabToLinear(Vector3 _oklab)
        {
            float _longRoot = _oklab.x + 0.3963377774f * _oklab.y + 0.2158037573f * _oklab.z;
            float _mediumRoot = _oklab.x - 0.1055613458f * _oklab.y - 0.0638541728f * _oklab.z;
            float _shortRoot = _oklab.x - 0.0894841775f * _oklab.y - 1.2914855480f * _oklab.z;

            float _long = _longRoot * _longRoot * _longRoot;
            float _medium = _mediumRoot * _mediumRoot * _mediumRoot;
            float _short = _shortRoot * _shortRoot * _shortRoot;

            return new Color(
                Mathf.Max(0f, 4.0767416621f * _long - 3.3077115913f * _medium + 0.2309699292f * _short),
                Mathf.Max(0f, -1.2684380046f * _long + 2.6097574011f * _medium - 0.3413193965f * _short),
                Mathf.Max(0f, -0.0041960863f * _long - 0.7034186147f * _medium + 1.7076147010f * _short),
                1f);
        }

        private static float CubeRoot(float _value)
        {
            return Mathf.Sign(_value) * Mathf.Pow(Mathf.Abs(_value), 1f / 3f);
        }

        private GridCell InstantiateCell()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return ((GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(m_cellPrefab.gameObject)).GetComponent<GridCell>();
#endif
            return Instantiate(m_cellPrefab);
        }

        private static void DestroyCellObject(Transform _cell)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(_cell.gameObject);
                return;
            }
#endif
            _cell.SetParent(null, false);
            Destroy(_cell.gameObject);
        }

#if UNITY_EDITOR
        [Button]
        private void RebuildFromLayout()
        {
            Build();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (m_appliedTileBaseColor == m_tileBaseColor && Mathf.Approximately(m_appliedTileToneContrast, m_tileToneContrast))
                return;

            ApplyTileTones();
        }
#endif

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
                RunRandom.GetStream(RandomChannel.Combat).Shuffle(_freeCells);

            return _freeCells;
        }

        public GridCell GetFreeSpawnCell(CellType _spawnCellType, bool _random = false)
        {
            List<GridCell> _freeCells = GetFreeSpawnCells(_spawnCellType, _random);
            return _freeCells.Count > 0 ? _freeCells[0] : null;
        }

        public GridCell GetCellUnderCursor(LayerMask _cellLayerMask)
        {
            if (UIManager.Instance.IsOverUI)
                return null;

            if (CameraManager.Instance == null || Mouse.current == null)
                return null;

            Camera _camera = CameraManager.Instance.MainCam;
            if (_camera == null)
                return null;

            Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            return Physics.Raycast(_ray, out RaycastHit _hit, Mathf.Infinity, _cellLayerMask)
                ? _hit.collider.GetComponentInParent<GridCell>()
                : null;
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

        public void GetCellsInZone(Vector2Int _origin, GridShape _shape, int _size, int _minSize, List<GridCell> _results)
        {
            _results.Clear();

            foreach (Vector2Int _coordinate in GridShapeUtility.GetCoordinates(_origin, _shape, _size, _minSize))
            {
                if (m_cellsByCoordinate.TryGetValue(_coordinate, out GridCell _cell))
                    _results.Add(_cell);
            }
        }

        public int GetManhattanDistance(GridCell _a, GridCell _b)
        {
            return GridShapeUtility.ManhattanDistance(_a.Coordinate, _b.Coordinate);
        }

        public bool Contains(Vector2Int _coordinate)
        {
            return m_cellsByCoordinate.ContainsKey(_coordinate);
        }

        public CellFlags GetFlags(Vector2Int _coordinate)
        {
            if (!m_cellsByCoordinate.TryGetValue(_coordinate, out GridCell _cell))
                return CellFlags.None;

            CellFlags _flags = CellFlags.None;

            if (_cell.IsObstacle)
                _flags |= CellFlags.Obstacle;

            if (_cell.IsOccupied)
                _flags |= CellFlags.Occupied;

            return _flags;
        }

        public List<GridCell> FindPath(GridCell _start, GridCell _goal, bool _ignoreOccupants = false, bool _randomize = false)
        {
            if (_start == null || _goal == null)
                return null;

            RandomStream _random = _randomize ? RunRandom.GetStream(RandomChannel.Combat) : null;

            if (!GridPathfinder.TryFindPath(this, _start.Coordinate, _goal.Coordinate, m_coordinateBuffer, _ignoreOccupants, _random))
                return null;

            List<GridCell> _path = new List<GridCell>(m_coordinateBuffer.Count);
            foreach (Vector2Int _coordinate in m_coordinateBuffer)
                _path.Add(GetCell(_coordinate));

            return _path;
        }

        public void GetReachableCells(GridCell _start, int _maxSteps, List<GridCell> _results, bool _ignoreOccupants = false)
        {
            _results.Clear();

            if (_start == null)
                return;

            GridPathfinder.GetReachableCoordinates(this, _start.Coordinate, _maxSteps, m_coordinateBuffer, _ignoreOccupants);

            foreach (Vector2Int _coordinate in m_coordinateBuffer)
            {
                GridCell _cell = GetCell(_coordinate);

                if (_cell != null)
                    _results.Add(_cell);
            }
        }

        public bool HasLineOfSight(GridCell _from, GridCell _to, out GridCell _blockingCell, Entity _ignoreOccupant = null)
        {
            _blockingCell = null;

            if (_from == null || _to == null)
                return false;

            Vector2Int? _ignoreOccupantAt = null;

            if (_ignoreOccupant != null && _ignoreOccupant.TryGetModule(out EntityCombatMoverModule _mover) && _mover.CurrentCell != null)
                _ignoreOccupantAt = _mover.CurrentCell.Coordinate;

            if (GridLineOfSight.HasLineOfSight(this, _from.Coordinate, _to.Coordinate, out Vector2Int _blockingCoordinate, _ignoreOccupantAt))
                return true;

            _blockingCell = GetCell(_blockingCoordinate);
            return false;
        }

        public CellHighlightSource CreateHighlightSource(CellHighlightStyle _style)
        {
            CellHighlightSource _source = new CellHighlightSource(this, m_highlightMaterial, _style);
            m_highlightSources.Add(_source);

            return _source;
        }

        private void LateUpdate()
        {
            for (int _i = m_highlightSources.Count - 1; _i >= 0; _i--)
            {
                CellHighlightSource _source = m_highlightSources[_i];

                if (_source.IsReleased)
                {
                    m_highlightSources.RemoveAt(_i);
                    continue;
                }

                if (_source.IsDirty)
                    _source.Rebuild(m_highlightMeshBuilder);
            }
        }

        private void OnDestroy()
        {
            foreach (CellHighlightSource _source in m_highlightSources)
                _source.Release();

            m_highlightSources.Clear();
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
