using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class HealCardAction : EntityCardAction
    {
        [SerializeField] private float m_healAmount;

        public float HealAmount => m_healAmount;

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            if (_entity.TryGetModule(out EntityHealthModule _health))
                _health.Heal(m_healAmount);
        }
    }
}
