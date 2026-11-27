using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    public enum CardActionTargets
    {
        EffectZone = 0,
        Self = 1
    }

    [Serializable]
    public abstract class EntityCardAction : CardAction
    {
        [SerializeField] private CardActionTargets m_targets = CardActionTargets.EffectZone;
        [SerializeField] private Team m_targetTeams = Team.Ally | Team.Enemy;

        public CardActionTargets Targets => m_targets;
        public Team TargetTeams => m_targetTeams;

        public bool CanAffect(Entity _entity)
        {
            return _entity != null && _entity.TryGetModule(out EntityTeamModule _teamModule) && _teamModule.BelongsTo(m_targetTeams);
        }

        public override void ApplyToCaster(CardActionContext _context)
        {
            if (m_targets == CardActionTargets.Self)
                ApplyTo(_context.Caster, _context);
        }

        public override bool Affects(Entity _entity)
        {
            return m_targets == CardActionTargets.EffectZone && CanAffect(_entity);
        }

        public override void ApplyToEntity(Entity _entity, CardActionContext _context)
        {
            if (Affects(_entity))
                ApplyTo(_entity, _context);
        }

        protected abstract void ApplyTo(Entity _entity, CardActionContext _context);
    }
}
