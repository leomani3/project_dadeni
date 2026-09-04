using System;
using Deckbuilder.StatusEffects;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class ApplyStatusEffectCardAction : EntityCardAction
    {
        [SerializeField] private StatusEffectConfig m_statusEffect;
        [SerializeField] private int m_stacks = 1;

        public StatusEffectConfig StatusEffect => m_statusEffect;
        public int Stacks => m_stacks;

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            Debug.LogWarning($"Status effect system not implemented yet, skipping {m_statusEffect?.DisplayName} on {_entity.name}.");
        }
    }
}
