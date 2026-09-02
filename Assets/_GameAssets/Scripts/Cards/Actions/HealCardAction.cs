using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class HealCardAction : CardAction
    {
        [SerializeField] private float m_healAmount;

        public float HealAmount => m_healAmount;
        public override Type DefinitionType => typeof(HealActionDefinition);
        protected override float Magnitude => m_healAmount;
    }
}
