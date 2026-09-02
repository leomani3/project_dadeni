using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class SummonCardAction : CardAction
    {
        [SerializeField] private Entity m_entityPrefab;

        public Entity EntityPrefab => m_entityPrefab;

        public override Type DefinitionType => typeof(SummonActionDefinition);

        protected override float Magnitude => 1f;
    }
}
