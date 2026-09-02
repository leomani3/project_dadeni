using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Deckbuilder.Grid
{
    public class GridBuilder : MonoBehaviour
    {
        [SerializeField] private GridCell m_cellPrefab;
        [SerializeField] private Transform m_cellsParent;
        [SerializeField] private Vector2Int m_size = new(8, 8);
        [SerializeField] private Vector2Int m_origin = Vector2Int.zero;

        public void EnsureBuilt()
        {
            Transform _parent = m_cellsParent != null ? m_cellsParent : transform;
            if (_parent.childCount == 0)
                BuildGrid();
        }

        [ContextMenu("Build Grid")]
        public void BuildGrid()
        {
            if (m_cellPrefab == null)
            {
                Debug.LogError("No cell prefab assigned.", this);
                return;
            }

            ClearGrid();

            Transform _parent = m_cellsParent != null ? m_cellsParent : transform;
            float _cellSize = GetCellSize();

            for (int _x = 0; _x < m_size.x; _x++)
            {
                for (int _y = 0; _y < m_size.y; _y++)
                {
                    Vector2Int _coordinate = m_origin + new Vector2Int(_x, _y);
                    GridCell _cell = InstantiateCell(_parent);
                    _cell.name = $"Cell_{_coordinate.x}_{_coordinate.y}";
                    _cell.transform.position = new Vector3(_coordinate.x * _cellSize, 0f, _coordinate.y * _cellSize);
                }
            }
        }

        [ContextMenu("Clear Grid")]
        public void ClearGrid()
        {
            Transform _parent = m_cellsParent != null ? m_cellsParent : transform;

            for (int _i = _parent.childCount - 1; _i >= 0; _i--)
            {
                GameObject _child = _parent.GetChild(_i).gameObject;

                if (Application.isPlaying)
                    Destroy(_child);
                else
                    DestroyImmediate(_child);
            }
        }

        private float GetCellSize()
        {
            GridManager _gridManager = GridManager.Instance;
            return _gridManager != null ? _gridManager.CellSize : 1f;
        }

        private GridCell InstantiateCell(Transform _parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return ((GameObject)PrefabUtility.InstantiatePrefab(m_cellPrefab.gameObject, _parent)).GetComponent<GridCell>();
#endif
            return Instantiate(m_cellPrefab, _parent);
        }
    }
}
