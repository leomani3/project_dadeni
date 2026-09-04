using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class DealDamageCardAction : EntityCardAction
    {
        [SerializeField] private float m_damage;

        public float Damage => m_damage;

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            if (_entity.TryGetModule(out EntityHealthModule _health))
                _health.TakeDamage(m_damage, false);
        }
    }
}
