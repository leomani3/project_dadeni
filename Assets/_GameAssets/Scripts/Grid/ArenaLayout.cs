using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    [CreateAssetMenu(fileName = "ArenaLayout", menuName = "Deckbuilder/Arena Layout")]
    public class ArenaLayout : ScriptableObject, IGridTopology
    {
        [Serializable]
        public struct CellEntry
        {
            public Vector2Int coordinate;
            public CellType type;
        }

        private const float CellCountEpsilon = 0.0001f;

        [SerializeField, Min(0.01f)] private float m_cellSize = 1.75f;
        [SerializeField] private Vector2 m_areaSize = new Vector2(15.75f, 15.75f);
        [SerializeField] private List<CellEntry> m_cells = new List<CellEntry>();

        private readonly Dictionary<Vector2Int, CellType> m_lookup = new Dictionary<Vector2Int, CellType>();
        private bool m_lookupBuilt;

        public float CellSize => m_cellSize;
        public Vector2 AreaSize => m_areaSize;
        public IReadOnlyList<CellEntry> Cells => m_cells;
        public int CellCount => m_cells.Count;

        private void OnEnable()
        {
            m_lookupBuilt = false;
        }

        private void OnValidate()
        {
            m_lookupBuilt = false;
        }

        public void SetCellSize(float _cellSize)
        {
            m_cellSize = Mathf.Max(0.01f, _cellSize);
        }

        public void SetAreaSize(Vector2 _areaSize)
        {
            m_areaSize = new Vector2(Mathf.Clamp(_areaSize.x, 0f, 512f), Mathf.Clamp(_areaSize.y, 0f, 512f));
        }

        public Vector2Int GetAreaCellCount()
        {
            return new Vector2Int(
                Mathf.Max(1, Mathf.FloorToInt(m_areaSize.x / m_cellSize + CellCountEpsilon)),
                Mathf.Max(1, Mathf.FloorToInt(m_areaSize.y / m_cellSize + CellCountEpsilon)));
        }

        public RectInt GetAreaBounds()
        {
            Vector2Int _count = GetAreaCellCount();

            return new RectInt(-Mathf.FloorToInt(_count.x * 0.5f), -Mathf.FloorToInt(_count.y * 0.5f), _count.x, _count.y);
        }

        public bool IsInsideArea(Vector2Int _coordinate)
        {
            return GetAreaBounds().Contains(_coordinate);
        }

        public int RemoveCellsOutsideArea()
        {
            RectInt _area = GetAreaBounds();
            int _removed = 0;

            for (int _i = m_cells.Count - 1; _i >= 0; _i--)
            {
                if (_area.Contains(m_cells[_i].coordinate))
                    continue;

                m_cells.RemoveAt(_i);
                _removed++;
            }

            if (_removed > 0)
                m_lookupBuilt = false;

            return _removed;
        }

        public bool Contains(Vector2Int _coordinate)
        {
            EnsureLookup();

            return m_lookup.ContainsKey(_coordinate);
        }

        public bool TryGetCellType(Vector2Int _coordinate, out CellType _type)
        {
            EnsureLookup();

            return m_lookup.TryGetValue(_coordinate, out _type);
        }

        public CellFlags GetFlags(Vector2Int _coordinate)
        {
            EnsureLookup();

            if (!m_lookup.TryGetValue(_coordinate, out CellType _type))
                return CellFlags.None;

            return _type == CellType.Obstacle ? CellFlags.Obstacle : CellFlags.None;
        }

        public int CountOfType(CellType _type)
        {
            int _count = 0;

            foreach (CellEntry _entry in m_cells)
            {
                if (_entry.type == _type)
                    _count++;
            }

            return _count;
        }

        public bool SetCell(Vector2Int _coordinate, CellType _type)
        {
            for (int _i = 0; _i < m_cells.Count; _i++)
            {
                if (m_cells[_i].coordinate != _coordinate)
                    continue;

                if (m_cells[_i].type == _type)
                    return false;

                CellEntry _entry = m_cells[_i];
                _entry.type = _type;
                m_cells[_i] = _entry;
                m_lookupBuilt = false;

                return true;
            }

            m_cells.Add(new CellEntry { coordinate = _coordinate, type = _type });
            m_lookupBuilt = false;

            return true;
        }

        public bool RemoveCell(Vector2Int _coordinate)
        {
            for (int _i = 0; _i < m_cells.Count; _i++)
            {
                if (m_cells[_i].coordinate != _coordinate)
                    continue;

                m_cells.RemoveAt(_i);
                m_lookupBuilt = false;

                return true;
            }

            return false;
        }

        public void Clear()
        {
            m_cells.Clear();
            m_lookupBuilt = false;
        }

        public RectInt GetBounds()
        {
            if (m_cells.Count == 0)
                return new RectInt(0, 0, 0, 0);

            Vector2Int _min = m_cells[0].coordinate;
            Vector2Int _max = m_cells[0].coordinate;

            foreach (CellEntry _entry in m_cells)
            {
                _min = Vector2Int.Min(_min, _entry.coordinate);
                _max = Vector2Int.Max(_max, _entry.coordinate);
            }

            return new RectInt(_min.x, _min.y, _max.x - _min.x + 1, _max.y - _min.y + 1);
        }

        public Vector3 CoordinateToLocalPosition(Vector2Int _coordinate)
        {
            return new Vector3(_coordinate.x * m_cellSize, 0f, _coordinate.y * m_cellSize);
        }

        private void EnsureLookup()
        {
            if (m_lookupBuilt)
                return;

            m_lookup.Clear();

            foreach (CellEntry _entry in m_cells)
                m_lookup[_entry.coordinate] = _entry.type;

            m_lookupBuilt = true;
        }
    }
}
