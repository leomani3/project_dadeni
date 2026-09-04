using System;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public abstract class EntityCardAction : CardAction
    {
        [SerializeField] private Team m_targetTeams = Team.Ally | Team.Enemy;

        public Team TargetTeams => m_targetTeams;

        public bool CanAffect(Entity _entity)
        {
            return _entity != null && _entity.TryGetModule(out EntityTeamModule _teamModule) && _teamModule.BelongsTo(m_targetTeams);
        }

        public override void Execute(CardActionContext _context)
        {
            foreach (Entity _entity in _context.EntitiesInEffectZone)
            {
                if (CanAffect(_entity))
                    ApplyTo(_entity, _context);
            }
        }

        protected abstract void ApplyTo(Entity _entity, CardActionContext _context);
    }
}
