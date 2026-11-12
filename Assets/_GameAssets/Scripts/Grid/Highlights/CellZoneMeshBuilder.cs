using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public class CellZoneMeshBuilder
    {
        private static readonly Vector2Int South = new(0, -1);
        private static readonly Vector2Int East = new(1, 0);
        private static readonly Vector2Int North = new(0, 1);
        private static readonly Vector2Int West = new(-1, 0);
        private static readonly Vector2Int NorthEast = new(1, 1);

        private readonly HashSet<Vector2Int> m_coordinates = new();
        private readonly Dictionary<Vector2Int, int> m_fillVertexIndices = new();
        private readonly Dictionary<Vector2Int, List<Vector2Int>> m_boundaryEdges = new();
        private readonly List<Vector2Int> m_boundaryEdgeStarts = new();
        private readonly List<Vector2Int> m_loop = new();
        private readonly List<Vector3> m_vertices = new();
        private readonly List<Color> m_colors = new();
        private readonly List<int> m_triangles = new();

        public void Build(IReadOnlyList<GridCell> _cells, float _cellSize, CellHighlightStyle _style, Mesh _mesh)
        {
            m_vertices.Clear();
            m_colors.Clear();
            m_triangles.Clear();
            m_fillVertexIndices.Clear();
            m_coordinates.Clear();

            foreach (GridCell _cell in _cells)
                m_coordinates.Add(_cell.Coordinate);

            if (_style.FillInterior)
                AppendFill(_cellSize, _style.FillColor);

            if (_style.OutlineWidth > 0f)
                AppendOutline(_cellSize, _style.OutlineWidth * _cellSize, _style.OutlineColor);

            _mesh.Clear();
            _mesh.SetVertices(m_vertices);
            _mesh.SetColors(m_colors);
            _mesh.SetTriangles(m_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void AppendFill(float _cellSize, Color _color)
        {
            foreach (Vector2Int _coordinate in m_coordinates)
            {
                int _southWest = GetFillVertex(_coordinate, _cellSize, _color);
                int _southEast = GetFillVertex(_coordinate + East, _cellSize, _color);
                int _northEast = GetFillVertex(_coordinate + NorthEast, _cellSize, _color);
                int _northWest = GetFillVertex(_coordinate + North, _cellSize, _color);

                m_triangles.Add(_southWest);
                m_triangles.Add(_northWest);
                m_triangles.Add(_northEast);

                m_triangles.Add(_southWest);
                m_triangles.Add(_northEast);
                m_triangles.Add(_southEast);
            }
        }

        private int GetFillVertex(Vector2Int _corner, float _cellSize, Color _color)
        {
            if (m_fillVertexIndices.TryGetValue(_corner, out int _index))
                return _index;

            _index = m_vertices.Count;
            m_fillVertexIndices[_corner] = _index;
            m_vertices.Add(CornerToLocalPosition(_corner, _cellSize));
            m_colors.Add(_color);

            return _index;
        }

        private void AppendOutline(float _cellSize, float _width, Color _color)
        {
            CollectBoundaryEdges();

            while (TryTakeLoop())
                AppendRibbon(_cellSize, _width, _color);
        }

        private void CollectBoundaryEdges()
        {
            foreach (List<Vector2Int> _ends in m_boundaryEdges.Values)
                _ends.Clear();

            m_boundaryEdgeStarts.Clear();

            foreach (Vector2Int _coordinate in m_coordinates)
            {
                if (!m_coordinates.Contains(_coordinate + South))
                    AddBoundaryEdge(_coordinate, _coordinate + East);

                if (!m_coordinates.Contains(_coordinate + East))
                    AddBoundaryEdge(_coordinate + East, _coordinate + NorthEast);

                if (!m_coordinates.Contains(_coordinate + North))
                    AddBoundaryEdge(_coordinate + NorthEast, _coordinate + North);

                if (!m_coordinates.Contains(_coordinate + West))
                    AddBoundaryEdge(_coordinate + North, _coordinate);
            }
        }

        private void AddBoundaryEdge(Vector2Int _from, Vector2Int _to)
        {
            if (!m_boundaryEdges.TryGetValue(_from, out List<Vector2Int> _ends))
                m_boundaryEdges[_from] = _ends = new List<Vector2Int>();

            _ends.Add(_to);
            m_boundaryEdgeStarts.Add(_from);
        }

        private bool TryTakeLoop()
        {
            m_loop.Clear();

            while (m_boundaryEdgeStarts.Count > 0)
            {
                Vector2Int _start = m_boundaryEdgeStarts[^1];

                if (!HasBoundaryEdge(_start))
                {
                    m_boundaryEdgeStarts.RemoveAt(m_boundaryEdgeStarts.Count - 1);
                    continue;
                }

                Vector2Int _current = _start;

                while (true)
                {
                    m_loop.Add(_current);

                    if (!TryTakeBoundaryEdge(_current, out Vector2Int _next) || _next == _start)
                        break;

                    _current = _next;
                }

                return true;
            }

            return false;
        }

        private bool HasBoundaryEdge(Vector2Int _from)
        {
            return m_boundaryEdges.TryGetValue(_from, out List<Vector2Int> _ends) && _ends.Count > 0;
        }

        private bool TryTakeBoundaryEdge(Vector2Int _from, out Vector2Int _to)
        {
            if (!HasBoundaryEdge(_from))
            {
                _to = default;
                return false;
            }

            List<Vector2Int> _ends = m_boundaryEdges[_from];
            _to = _ends[^1];
            _ends.RemoveAt(_ends.Count - 1);

            return true;
        }

        private void AppendRibbon(float _cellSize, float _width, Color _color)
        {
            int _count = m_loop.Count;
            if (_count < 3)
                return;

            int _baseIndex = m_vertices.Count;

            for (int _i = 0; _i < _count; _i++)
            {
                Vector2Int _previous = m_loop[(_i - 1 + _count) % _count];
                Vector2Int _current = m_loop[_i];
                Vector2Int _next = m_loop[(_i + 1) % _count];

                Vector2 _inwardFromPrevious = GetInwardNormal(_current - _previous);
                Vector2 _inwardFromNext = GetInwardNormal(_next - _current);
                Vector2 _inward = _inwardFromPrevious == _inwardFromNext
                    ? _inwardFromPrevious
                    : _inwardFromPrevious + _inwardFromNext;

                Vector3 _outerPosition = CornerToLocalPosition(_current, _cellSize);

                m_vertices.Add(_outerPosition);
                m_colors.Add(_color);
                m_vertices.Add(_outerPosition + new Vector3(_inward.x, 0f, _inward.y) * _width);
                m_colors.Add(_color);
            }

            for (int _i = 0; _i < _count; _i++)
            {
                int _outer = _baseIndex + _i * 2;
                int _nextOuter = _baseIndex + (_i + 1) % _count * 2;

                m_triangles.Add(_outer);
                m_triangles.Add(_nextOuter);
                m_triangles.Add(_nextOuter + 1);

                m_triangles.Add(_outer);
                m_triangles.Add(_nextOuter + 1);
                m_triangles.Add(_outer + 1);
            }
        }

        private static Vector2 GetInwardNormal(Vector2Int _direction)
        {
            return new Vector2(-_direction.y, _direction.x);
        }

        private static Vector3 CornerToLocalPosition(Vector2Int _corner, float _cellSize)
        {
            return new Vector3((_corner.x - 0.5f) * _cellSize, 0f, (_corner.y - 0.5f) * _cellSize);
        }
    }
}
