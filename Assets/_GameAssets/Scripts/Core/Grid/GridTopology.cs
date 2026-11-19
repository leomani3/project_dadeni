using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public class GridTopology : IGridTopology
    {
        public const char MissingGlyph = ' ';
        public const char EmptyGlyph = '.';
        public const char ObstacleGlyph = '#';
        public const char OccupiedGlyph = 'o';
        public const char ObstacleAndOccupiedGlyph = 'O';

        private readonly Dictionary<Vector2Int, CellFlags> m_cells = new();

        public int Count => m_cells.Count;
        public IEnumerable<Vector2Int> Coordinates => m_cells.Keys;

        public bool Contains(Vector2Int _coordinate)
        {
            return m_cells.ContainsKey(_coordinate);
        }

        public CellFlags GetFlags(Vector2Int _coordinate)
        {
            return m_cells.TryGetValue(_coordinate, out CellFlags _flags) ? _flags : CellFlags.None;
        }

        public void SetCell(Vector2Int _coordinate, CellFlags _flags = CellFlags.None)
        {
            m_cells[_coordinate] = _flags;
        }

        public void RemoveCell(Vector2Int _coordinate)
        {
            m_cells.Remove(_coordinate);
        }

        public void Clear()
        {
            m_cells.Clear();
        }

        public static GridTopology Rectangle(int _width, int _height)
        {
            GridTopology _topology = new GridTopology();

            for (int _x = 0; _x < _width; _x++)
            {
                for (int _y = 0; _y < _height; _y++)
                    _topology.SetCell(new Vector2Int(_x, _y));
            }

            return _topology;
        }

        public static GridTopology Parse(string _map)
        {
            string[] _lines = _map.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            List<string> _rows = new List<string>();
            foreach (string _line in _lines)
            {
                if (_line.Trim().Length > 0)
                    _rows.Add(_line);
            }

            GridTopology _topology = new GridTopology();

            for (int _row = 0; _row < _rows.Count; _row++)
            {
                string _text = _rows[_row];
                int _y = _rows.Count - 1 - _row;

                for (int _x = 0; _x < _text.Length; _x++)
                {
                    if (!TryParseGlyph(_text[_x], out CellFlags _flags))
                        continue;

                    _topology.SetCell(new Vector2Int(_x, _y), _flags);
                }
            }

            return _topology;
        }

        public string ToDebugString()
        {
            if (m_cells.Count == 0)
                return string.Empty;

            int _minX = int.MaxValue, _maxX = int.MinValue, _minY = int.MaxValue, _maxY = int.MinValue;

            foreach (Vector2Int _coordinate in m_cells.Keys)
            {
                _minX = Mathf.Min(_minX, _coordinate.x);
                _maxX = Mathf.Max(_maxX, _coordinate.x);
                _minY = Mathf.Min(_minY, _coordinate.y);
                _maxY = Mathf.Max(_maxY, _coordinate.y);
            }

            StringBuilder _builder = new StringBuilder();

            for (int _y = _maxY; _y >= _minY; _y--)
            {
                for (int _x = _minX; _x <= _maxX; _x++)
                    _builder.Append(ToGlyph(new Vector2Int(_x, _y)));

                if (_y > _minY)
                    _builder.Append('\n');
            }

            return _builder.ToString();
        }

        private char ToGlyph(Vector2Int _coordinate)
        {
            if (!m_cells.TryGetValue(_coordinate, out CellFlags _flags))
                return MissingGlyph;

            bool _isObstacle = (_flags & CellFlags.Obstacle) != 0;
            bool _isOccupied = (_flags & CellFlags.Occupied) != 0;

            if (_isObstacle && _isOccupied)
                return ObstacleAndOccupiedGlyph;

            if (_isObstacle)
                return ObstacleGlyph;

            return _isOccupied ? OccupiedGlyph : EmptyGlyph;
        }

        private static bool TryParseGlyph(char _glyph, out CellFlags _flags)
        {
            switch (_glyph)
            {
                case EmptyGlyph:
                    _flags = CellFlags.None;
                    return true;

                case ObstacleGlyph:
                    _flags = CellFlags.Obstacle;
                    return true;

                case OccupiedGlyph:
                    _flags = CellFlags.Occupied;
                    return true;

                case ObstacleAndOccupiedGlyph:
                    _flags = CellFlags.Obstacle | CellFlags.Occupied;
                    return true;

                default:
                    _flags = CellFlags.None;
                    return false;
            }
        }
    }
}
