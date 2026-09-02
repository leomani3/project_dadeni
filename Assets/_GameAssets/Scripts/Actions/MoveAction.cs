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
            if (!m_entity.TryGetModule(out EntityGridModule _gridModule))
                yield break;

            if (!_gridModule.MoveTo(m_destination))
                yield break;

            while (_gridModule.IsMoving)
                yield return null;
        }
    }
}
