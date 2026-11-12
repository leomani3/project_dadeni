using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class GainBlockCardAction : EntityCardAction
    {
        [SerializeField] private float m_block;

        public float Block => m_block;

        public override string DescriptionFormat => "Gain {0} block";
        public override object[] DescriptionValues => new object[] { m_block };

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            if (_entity.TryGetModule(out EntityHealthModule _health))
                _health.GainBlock(m_block);
        }
    }
}
