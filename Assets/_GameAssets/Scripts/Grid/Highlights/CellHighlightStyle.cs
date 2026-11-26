using UnityEngine;

namespace Deckbuilder.Grid
{
    public enum CellHighlightBlend
    {
        Additive,
        Alpha
    }

    public enum CellHighlightMode
    {
        Cells,
        Path,
        Reticle
    }

    [CreateAssetMenu(fileName = "NewCellHighlightStyle", menuName = "Deckbuilder/Cell Highlight Style")]
    public class CellHighlightStyle : ScriptableObject
    {
        [SerializeField] private CellHighlightMode m_mode = CellHighlightMode.Cells;
        [SerializeField] private Color m_fillColor = new(0.1f, 0.75f, 0.25f, 0.55f);
        [SerializeField] private Color m_centerColor = new(0.55f, 1f, 0.6f, 0.8f);
        [SerializeField] private Color m_borderColor = new(0.75f, 1f, 0.8f, 0.9f);
        [SerializeField, Range(0f, 0.5f)] private float m_borderWidth = 0.07f;
        [SerializeField, Range(0.1f, 8f)] private float m_gradientPower = 1.4f;
        [SerializeField, Range(0.1f, 1f)] private float m_cellScale = 0.98f;
        [SerializeField, Range(0.01f, 0.5f)] private float m_glyphThickness = 0.12f;
        [SerializeField, Range(0.05f, 1f)] private float m_glyphRadius = 0.55f;
        [SerializeField, Range(0.05f, 1f)] private float m_glyphScale = 0.45f;
        [SerializeField, Range(0.15f, 1.5f)] private float m_glyphSpacing = 0.4f;
        [SerializeField, Min(0f)] private float m_highlightCoefficient = 1f;
        [SerializeField] private float m_heightOffset = 0.01f;
        [SerializeField] private int m_order;
        [SerializeField] private CellHighlightBlend m_blend = CellHighlightBlend.Alpha;

        public CellHighlightMode Mode => m_mode;
        public Color FillColor => m_fillColor;
        public Color CenterColor => m_centerColor;
        public Color BorderColor => m_borderColor;
        public float BorderWidth => m_borderWidth;
        public float GradientPower => m_gradientPower;
        public float CellScale => m_cellScale;
        public float GlyphThickness => m_glyphThickness;
        public float GlyphRadius => m_glyphRadius;
        public float GlyphScale => m_glyphScale;
        public float GlyphSpacing => m_glyphSpacing;
        public float HighlightCoefficient => m_highlightCoefficient;
        public float HeightOffset => m_heightOffset;
        public int Order => m_order;
        public CellHighlightBlend Blend => m_blend;
    }
}
