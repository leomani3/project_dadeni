using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deckbuilder.Grid
{
    public class CellHighlightSource
    {
        private static readonly int HighlightCoefId = Shader.PropertyToID("_HighlightCoef");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        private readonly ArenaGrid m_grid;
        private readonly GameObject m_gameObject;
        private readonly Transform m_transform;
        private readonly MeshRenderer m_meshRenderer;
        private readonly Material m_material;
        private readonly Mesh m_mesh;

        private List<GridCell> m_cells = new();
        private List<GridCell> m_pendingCells = new();
        private HashSet<GridCell> m_cellSet = new();
        private HashSet<GridCell> m_pendingCellSet = new();

        private CellHighlightStyle m_style;
        private bool m_isVisible = true;

        public CellHighlightStyle Style => m_style;
        public IReadOnlyList<GridCell> Cells => m_cells;
        public bool IsVisible => m_isVisible;
        public bool IsReleased { get; private set; }

        internal bool IsDirty { get; private set; }

        internal CellHighlightSource(ArenaGrid _grid, Material _material, CellHighlightStyle _style)
        {
            m_grid = _grid;
            m_style = _style;

            m_gameObject = new GameObject($"CellHighlight_{_style.name}");
            m_transform = m_gameObject.transform;
            m_transform.SetParent(_grid.CellsRoot, false);

            m_mesh = new Mesh { name = m_gameObject.name };
            m_mesh.MarkDynamic();

            MeshFilter _meshFilter = m_gameObject.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = m_mesh;

            m_material = new Material(_material) { name = m_gameObject.name };

            m_meshRenderer = m_gameObject.AddComponent<MeshRenderer>();
            m_meshRenderer.sharedMaterial = m_material;
            m_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            m_meshRenderer.receiveShadows = false;
            m_meshRenderer.enabled = false;

            IsDirty = true;
        }

        public void Set(IEnumerable<GridCell> _cells)
        {
            m_pendingCells.Clear();
            m_pendingCellSet.Clear();

            foreach (GridCell _cell in _cells)
            {
                if (_cell != null && m_pendingCellSet.Add(_cell))
                    m_pendingCells.Add(_cell);
            }

            if (m_pendingCellSet.SetEquals(m_cellSet))
                return;

            (m_cells, m_pendingCells) = (m_pendingCells, m_cells);
            (m_cellSet, m_pendingCellSet) = (m_pendingCellSet, m_cellSet);

            IsDirty = true;
        }

        public void Clear()
        {
            if (m_cells.Count == 0)
                return;

            m_cells.Clear();
            m_cellSet.Clear();

            IsDirty = true;
        }

        public void SetStyle(CellHighlightStyle _style)
        {
            if (m_style == _style)
                return;

            m_style = _style;
            IsDirty = true;
        }

        public void SetVisible(bool _isVisible)
        {
            if (m_isVisible == _isVisible)
                return;

            m_isVisible = _isVisible;
            IsDirty = true;
        }

        public void Release()
        {
            if (IsReleased)
                return;

            IsReleased = true;

            Object.Destroy(m_gameObject);
            Object.Destroy(m_mesh);
            Object.Destroy(m_material);
        }

        internal void Rebuild(CellZoneMeshBuilder _meshBuilder)
        {
            IsDirty = false;

            bool _isDrawn = m_isVisible && m_cells.Count > 0;
            m_meshRenderer.enabled = _isDrawn;

            if (!_isDrawn)
            {
                m_mesh.Clear();
                return;
            }

            m_transform.localPosition = new Vector3(0f, m_style.HeightOffset, 0f);
            m_meshRenderer.sortingOrder = m_style.Order;

            m_material.SetFloat(HighlightCoefId, m_style.HighlightCoefficient);
            m_material.SetFloat(SrcBlendId, (float)BlendMode.One);
            m_material.SetFloat(DstBlendId, (float)(m_style.Blend == CellHighlightBlend.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));

            _meshBuilder.Build(m_cells, m_grid.CellSize, m_style, m_mesh);
        }
    }
}
