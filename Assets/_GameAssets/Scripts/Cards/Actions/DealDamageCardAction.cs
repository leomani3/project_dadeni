using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class DealDamageCardAction : CardAction
    {
        [SerializeField] private float m_damage;

        public float Damage => m_damage;
        public override Type DefinitionType => typeof(DealDamageActionDefinition);
        protected override float Magnitude => m_damage;
    }
}
