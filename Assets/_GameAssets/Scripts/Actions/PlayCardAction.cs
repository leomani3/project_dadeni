using System.Collections;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using UnityEngine;
using Utils;

namespace Deckbuilder.Actions
{
    public class PlayCardAction : IEntityAction
    {
        private readonly Arena m_arena;
        private readonly CardConfig m_card;
        private readonly Entity m_caster;
        private readonly GridCell m_targetCell;

        public PlayCardAction(Arena _arena, CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            m_arena = _arena;
            m_card = _card;
            m_caster = _caster;
            m_targetCell = _targetCell;
        }

        public IEnumerator Execute()
        {
            if (m_caster.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.FaceTowards(m_targetCell.transform.position);

            if (m_card.CastAnimation != null && m_caster.TryGetModule(out EntityAnimationModule _animationModule))
                yield return _animationModule.PlayCast(m_card.CastAnimation, ApplyCard);
            else
                ApplyCard();
        }

        private void ApplyCard()
        {
            if (!CardExecutor.Execute(m_arena, m_card, m_caster, m_targetCell))
                m_caster.LogWarning($"Failed to play '{m_card.Title}' on {m_targetCell.name}.");
        }
    }
}
