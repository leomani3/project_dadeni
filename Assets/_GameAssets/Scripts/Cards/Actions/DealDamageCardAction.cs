using System;
using Deckbuilder.StatusEffects;
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
                _health.TakeDamage(GetDamageDealtBy(_context.Caster), false);
        }

        private float GetDamageDealtBy(Entity _caster)
        {
            _caster.TryGetModule(out StatusEffectModule _casterStatusEffects);

            float _damage = m_damage;

            if (_casterStatusEffects.TryGetStatusEffect(StatusEffectType.Strength, out StatusEffect _strength))
                _damage += _strength.Stacks * _strength.Config.Potency;

            if (_casterStatusEffects.TryGetStatusEffect(StatusEffectType.Weak, out StatusEffect _weak))
                _damage *= 1f - _weak.Config.Potency / 100f;

            return _damage;
        }
    }
}
