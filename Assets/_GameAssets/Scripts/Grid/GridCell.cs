using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public class GridCell : MonoBehaviour
    {
        private const float SpawnGizmoSize = 0.8f;
        private const float SpawnGizmoHeight = 0.05f;

        private static readonly Color AllySpawnColor = new(0.2f, 0.9f, 0.4f, 1f);
        private static readonly Color EnemySpawnColor = new(1f, 0.3f, 0.25f, 1f);

        [SerializeField] private Vector2Int m_coordinate;
        [SerializeField] private CellType m_cellType;
        [SerializeField] private GameObject m_obstacleVisual;
        [SerializeField, Required] private MeshFilter m_obstacleMeshFilter;
        [SerializeField, Required] private MeshRenderer m_obstacleRenderer;
        [SerializeField, Required] private MeshFilter m_tileMeshFilter;
        [SerializeField, Required] private MeshRenderer m_tileRenderer;

        private readonly Dictionary<GridDirection, GridCell> m_neighbors = new();

        public Vector2Int Coordinate => m_coordinate;
        public CellType CellType => m_cellType;
        public Entity Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;
        public bool IsObstacle => m_cellType == CellType.Obstacle;
        public bool IsWalkable => !IsObstacle && !IsOccupied;

        private void Awake()
        {
            UpdateObstacleVisual();
        }

        private void OnValidate()
        {
            UpdateObstacleVisual();
        }

        public void SetCoordinate(Vector2Int _coordinate)
        {
            m_coordinate = _coordinate;
        }

        public void SetCellType(CellType _cellType)
        {
            m_cellType = _cellType;
            UpdateObstacleVisual();
        }

        public void SetTileVisual(Mesh _mesh, float _yRotation, Material _material)
        {
            m_tileMeshFilter.sharedMesh = _mesh;
            m_tileMeshFilter.transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
            m_tileRenderer.sharedMaterial = _material;
        }

        public void SetObstacleVisual(Mesh _mesh, float _yRotation, Material _material)
        {
            m_obstacleMeshFilter.sharedMesh = _mesh;
            m_obstacleMeshFilter.transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
            m_obstacleRenderer.sharedMaterial = _material;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TryGetSpawnGizmoColor(out Color _color))
                return;

            Vector3 _center = transform.position + Vector3.up * SpawnGizmoHeight;
            Vector3 _size = new(SpawnGizmoSize, 0f, SpawnGizmoSize);

            Gizmos.color = new Color(_color.r, _color.g, _color.b, 0.25f);
            Gizmos.DrawCube(_center, _size);

            Gizmos.color = _color;
            Gizmos.DrawWireCube(_center, _size);
        }

        private bool TryGetSpawnGizmoColor(out Color _color)
        {
            switch (m_cellType)
            {
                case CellType.AllySpawn:
                    _color = AllySpawnColor;
                    return true;

                case CellType.EnemySpawn:
                    _color = EnemySpawnColor;
                    return true;

                default:
                    _color = default;
                    return false;
            }
        }
#endif

        private void UpdateObstacleVisual()
        {
            if (m_obstacleVisual != null)
                m_obstacleVisual.SetActive(IsObstacle);
        }

        public void SetNeighbor(GridDirection _direction, GridCell _cell)
        {
            m_neighbors[_direction] = _cell;
        }

        public void ClearNeighbors()
        {
            m_neighbors.Clear();
        }

        public GridCell GetNeighbor(GridDirection _direction)
        {
            return m_neighbors.TryGetValue(_direction, out GridCell _cell) ? _cell : null;
        }

        public IEnumerable<GridCell> GetNeighbors(bool _includeDiagonals = true)
        {
            GridDirection[] _directions = _includeDiagonals ? GridDirectionUtility.All : GridDirectionUtility.Orthogonal;
            foreach (GridDirection _direction in _directions)
            {
                GridCell _cell = GetNeighbor(_direction);
                if (_cell != null)
                    yield return _cell;
            }
        }

        public bool TrySetOccupant(Entity _entity)
        {
            if (IsOccupied || _entity == null)
                return false;

            Occupant = _entity;

            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.SetCurrentCell(this);

            return true;
        }

        public void ClearOccupant()
        {
            Occupant = null;
        }
    }
}
