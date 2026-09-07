using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid.Highlighting
{
    public class ArenaHighlighter : MonoBehaviour
    {
        private static readonly Dictionary<HighlightLayer, int> SlotByLayer = new()
        {
            { HighlightLayer.MovementRange, 0 },
            { HighlightLayer.MovementPath, 1 },
            { HighlightLayer.TargetZone, 0 },
            { HighlightLayer.EffectZone, 1 },
            { HighlightLayer.EnemyTelegraph, 2 },
        };

        private readonly Dictionary<HighlightLayer, HashSet<GridCell>> m_activeCellsByLayer = new();

        public void Highlight(GridCell _cell, HighlightLayer _layer, Color _color)
        {
            if (_cell == null)
                return;

            _cell.HighlightVisual.SetSlot(GetSlot(_layer), _color);
            GetActiveSet(_layer).Add(_cell);
        }

        public void Clear(GridCell _cell, HighlightLayer _layer)
        {
            if (_cell == null)
                return;

            _cell.HighlightVisual.SetSlot(GetSlot(_layer), null);
            GetActiveSet(_layer).Remove(_cell);
        }

        public void ClearLayer(HighlightLayer _layer)
        {
            HashSet<GridCell> _cells = GetActiveSet(_layer);
            int _slot = GetSlot(_layer);

            foreach (GridCell _cell in _cells)
            {
                if (_cell != null)
                    _cell.HighlightVisual.SetSlot(_slot, null);
            }

            _cells.Clear();
        }

        public void ClearAllLayers()
        {
            foreach (HighlightLayer _layer in m_activeCellsByLayer.Keys)
                ClearLayer(_layer);
        }

        private static int GetSlot(HighlightLayer _layer)
        {
            return SlotByLayer.TryGetValue(_layer, out int _slot) ? _slot : 0;
        }

        private HashSet<GridCell> GetActiveSet(HighlightLayer _layer)
        {
            if (!m_activeCellsByLayer.TryGetValue(_layer, out HashSet<GridCell> _set))
                m_activeCellsByLayer[_layer] = _set = new HashSet<GridCell>();

            return _set;
        }
    }
}
