using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class SummonCardAction : CardAction
    {
        [SerializeField] private Entity m_entityPrefab;

        public Entity EntityPrefab => m_entityPrefab;

        public override void Execute(CardActionContext _context)
        {
            if (_context.TargetCell.IsOccupied)
            {
                Debug.LogWarning("Cannot summon, target cell is already occupied.");
                return;
            }

            _context.Arena.SpawnEntity(m_entityPrefab, _context.TargetCell);
        }
    }
}
