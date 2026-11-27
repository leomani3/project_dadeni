using System.Collections;
using Deckbuilder.Combat;

namespace Deckbuilder.Actions
{
    public class EndTurnAction : IEntityAction
    {
        private readonly CombatManager m_combatManager;

        public EndTurnAction(CombatManager _combatManager)
        {
            m_combatManager = _combatManager;
        }

        public IEnumerator Execute()
        {
            return m_combatManager.PassPlayerTurn();
        }
    }
}
