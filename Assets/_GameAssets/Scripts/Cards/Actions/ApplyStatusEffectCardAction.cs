using System;
using Deckbuilder.StatusEffects;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class ApplyStatusEffectCardAction : CardAction
    {
        [SerializeField] private StatusEffectConfig m_statusEffect;
        [SerializeField] private int m_stacks = 1;

        public StatusEffectConfig StatusEffect => m_statusEffect;
        public int Stacks => m_stacks;
        public override Type DefinitionType => typeof(ApplyStatusEffectActionDefinition);
        protected override float Magnitude => m_stacks;
    }
}
