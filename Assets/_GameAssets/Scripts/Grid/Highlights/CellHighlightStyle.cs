using UnityEngine;

namespace Deckbuilder.Grid
{
    public enum CellHighlightBlend
    {
        Additive,
        Alpha
    }

    [CreateAssetMenu(fileName = "NewCellHighlightStyle", menuName = "Deckbuilder/Cell Highlight Style")]
    public class CellHighlightStyle : ScriptableObject
    {
        [SerializeField] private Color m_fillColor = new(0f, 1f, 0.2f, 0.18f);
        [SerializeField] private Color m_outlineColor = new(0f, 1f, 0.2f, 0.85f);
        [SerializeField, Range(0f, 0.5f)] private float m_outlineWidth = 0.06f;
        [SerializeField] private bool m_fillInterior = true;
        [SerializeField, Min(0f)] private float m_highlightCoefficient = 1f;
        [SerializeField] private float m_heightOffset = 0.01f;
        [SerializeField] private int m_order;
        [SerializeField] private CellHighlightBlend m_blend = CellHighlightBlend.Additive;

        public Color FillColor => m_fillColor;
        public Color OutlineColor => m_outlineColor;
        public float OutlineWidth => m_outlineWidth;
        public bool FillInterior => m_fillInterior;
        public float HighlightCoefficient => m_highlightCoefficient;
        public float HeightOffset => m_heightOffset;
        public int Order => m_order;
        public CellHighlightBlend Blend => m_blend;
    }
}
