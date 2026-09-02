using System;
using Deckbuilder.Grid;
using UnityEngine;

namespace Deckbuilder.Cards
{
    [Serializable]
    public class ZoneDefinition
    {
        [SerializeField] private GridShape m_shape;
        [SerializeField, Min(0)] private int m_minSize;
        [SerializeField, Min(1)] private int m_size = 1;

        public GridShape Shape => m_shape;
        public int Size => m_size;
        public int MinSize => Mathf.Min(m_minSize, Mathf.Max(m_size - 1, 0));
    }
}
