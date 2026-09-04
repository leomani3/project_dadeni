using System;
using Deckbuilder.Grid;
using UnityEngine;

namespace Deckbuilder.Cards
{
    [Serializable]
    public class ZoneDefinition
    {
        [SerializeField] private GridShape m_shape;
        [SerializeField] private Vector2Int m_range = new(0, 1);

        public GridShape Shape => m_shape;
        public int MaxRange => Mathf.Max(m_range.y, 1);
        public int MinRange => Mathf.Clamp(m_range.x, 0, Mathf.Max(MaxRange - 1, 0));
    }
}
