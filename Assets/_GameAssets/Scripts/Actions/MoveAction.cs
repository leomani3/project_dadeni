using System.Collections;
using Deckbuilder.Grid;

namespace Deckbuilder.Actions
{
    public class MoveAction : IEntityAction
    {
        private readonly Entity m_entity;
        private readonly GridCell m_destination;

        public MoveAction(Entity _entity, GridCell _destination)
        {
            m_entity = _entity;
            m_destination = _destination;
        }

        public IEnumerator Execute()
        {
            if (!m_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                yield break;

            if (!_combatMover.MoveTo(m_destination))
                yield break;

            while (_combatMover.IsMoving)
                yield return null;
        }
    }
}
