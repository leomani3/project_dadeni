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

        public override string DescriptionFormat => "Apply {0} {1}";
        public override object[] DescriptionValues => new object[] { m_stacks, m_statusEffect.DisplayName };

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            _entity.TryGetModule(out StatusEffectModule _statusEffects);
            _statusEffects.ApplyStatusEffect(m_statusEffect, m_stacks);
        }
    }
}
