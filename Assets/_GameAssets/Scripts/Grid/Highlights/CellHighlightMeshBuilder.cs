using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public class CellHighlightMeshBuilder
    {
        private const float TurnDotScale = 0.75f;
        private const float ArrivalDotScale = 1.4f;
        private const float DotClearanceRatio = 0.45f;
        private const float DotGlyph = 1f;
        private const float ChevronGlyph = 0f;

        private readonly List<Vector3> m_vertices = new();
        private readonly List<Vector2> m_uvs = new();
        private readonly List<Vector2> m_glyphs = new();
        private readonly List<int> m_triangles = new();

        public void Build(IReadOnlyList<GridCell> _cells, float _cellSize, CellHighlightStyle _style, Mesh _mesh)
        {
            m_vertices.Clear();
            m_uvs.Clear();
            m_glyphs.Clear();
            m_triangles.Clear();

            if (_style.Mode == CellHighlightMode.Path)
                AppendPath(_cells, _cellSize, _style);
            else
                AppendCellQuads(_cells, _cellSize, _cellSize * _style.CellScale);

            _mesh.Clear();
            _mesh.SetVertices(m_vertices);
            _mesh.SetUVs(0, m_uvs);
            _mesh.SetUVs(1, m_glyphs);
            _mesh.SetTriangles(m_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void AppendCellQuads(IReadOnlyList<GridCell> _cells, float _cellSize, float _quadSize)
        {
            foreach (GridCell _cell in _cells)
                AppendQuad(CoordinateToLocalPosition(_cell.Coordinate, _cellSize), Vector3.right, Vector3.forward, _quadSize, 0f);
        }

        private void AppendPath(IReadOnlyList<GridCell> _cells, float _cellSize, CellHighlightStyle _style)
        {
            int _stepCount = _cells.Count - 1;

            if (_stepCount < 1)
                return;

            float _size = _cellSize * _style.GlyphScale;
            float _spacing = Mathf.Max(0.05f, _style.GlyphSpacing) * _cellSize;
            float _length = _stepCount * _cellSize;
            float _turnClearance = (_size * TurnDotScale + _size) * DotClearanceRatio;
            float _arrivalClearance = (_size * ArrivalDotScale + _size) * DotClearanceRatio;

            for (float _travelled = _spacing * 0.5f; _travelled < _length; _travelled += _spacing)
            {
                if (_length - _travelled < _arrivalClearance || IsNearATurn(_cells, _cellSize, _travelled, _turnClearance))
                    continue;

                int _step = Mathf.Min((int)(_travelled / _cellSize), _stepCount - 1);

                Vector3 _from = CoordinateToLocalPosition(_cells[_step].Coordinate, _cellSize);
                Vector3 _to = CoordinateToLocalPosition(_cells[_step + 1].Coordinate, _cellSize);

                Vector3 _forward = (_to - _from).normalized;
                Vector3 _right = new Vector3(_forward.z, 0f, -_forward.x);

                AppendQuad(Vector3.Lerp(_from, _to, (_travelled - _step * _cellSize) / _cellSize), _right, _forward, _size, ChevronGlyph);
            }

            for (int _step = 1; _step < _stepCount; _step++)
            {
                if (!IsTurn(_cells, _step))
                    continue;

                AppendQuad(CoordinateToLocalPosition(_cells[_step].Coordinate, _cellSize), Vector3.right, Vector3.forward, _size * TurnDotScale, DotGlyph);
            }

            AppendQuad(CoordinateToLocalPosition(_cells[^1].Coordinate, _cellSize), Vector3.right, Vector3.forward, _size * ArrivalDotScale, DotGlyph);
        }

        private static bool IsNearATurn(IReadOnlyList<GridCell> _cells, float _cellSize, float _travelled, float _clearance)
        {
            for (int _step = 1; _step < _cells.Count - 1; _step++)
            {
                if (IsTurn(_cells, _step) && Mathf.Abs(_travelled - _step * _cellSize) < _clearance)
                    return true;
            }

            return false;
        }

        private static bool IsTurn(IReadOnlyList<GridCell> _cells, int _index)
        {
            if (_index <= 0 || _index >= _cells.Count - 1)
                return false;

            return _cells[_index].Coordinate - _cells[_index - 1].Coordinate != _cells[_index + 1].Coordinate - _cells[_index].Coordinate;
        }

        private void AppendQuad(Vector3 _center, Vector3 _right, Vector3 _forward, float _size, float _glyph)
        {
            float _half = _size * 0.5f;
            Vector3 _acrossOffset = _right * _half;
            Vector3 _alongOffset = _forward * _half;

            int _baseIndex = m_vertices.Count;

            m_vertices.Add(_center - _acrossOffset - _alongOffset);
            m_vertices.Add(_center + _acrossOffset - _alongOffset);
            m_vertices.Add(_center + _acrossOffset + _alongOffset);
            m_vertices.Add(_center - _acrossOffset + _alongOffset);

            m_uvs.Add(new Vector2(0f, 0f));
            m_uvs.Add(new Vector2(1f, 0f));
            m_uvs.Add(new Vector2(1f, 1f));
            m_uvs.Add(new Vector2(0f, 1f));

            Vector2 _glyphData = new Vector2(_glyph, 0f);
            m_glyphs.Add(_glyphData);
            m_glyphs.Add(_glyphData);
            m_glyphs.Add(_glyphData);
            m_glyphs.Add(_glyphData);

            m_triangles.Add(_baseIndex);
            m_triangles.Add(_baseIndex + 3);
            m_triangles.Add(_baseIndex + 2);

            m_triangles.Add(_baseIndex);
            m_triangles.Add(_baseIndex + 2);
            m_triangles.Add(_baseIndex + 1);
        }

        private static Vector3 CoordinateToLocalPosition(Vector2Int _coordinate, float _cellSize)
        {
            return new Vector3(_coordinate.x * _cellSize, 0f, _coordinate.y * _cellSize);
        }
    }
}
