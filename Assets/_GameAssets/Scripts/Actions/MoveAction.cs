using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Grid;

namespace Deckbuilder.Actions
{
    public class MoveAction : IEntityAction
    {
        private readonly Entity m_entity;
        private readonly GridCell m_destination;
        private readonly List<GridCell> m_path;

        public MoveAction(Entity _entity, GridCell _destination)
        {
            m_entity = _entity;
            m_destination = _destination;
        }

        public MoveAction(Entity _entity, IReadOnlyList<GridCell> _path)
        {
            m_entity = _entity;
            m_path = new List<GridCell>(_path);
            m_destination = m_path.Count > 0 ? m_path[^1] : null;
        }

        public IEnumerator Execute()
        {
            if (!m_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                yield break;

            bool _started = m_path != null
                ? _combatMover.MoveAlong(m_path)
                : _combatMover.MoveTo(m_destination);

            if (!_started)
                yield break;

            while (_combatMover.IsMoving)
                yield return null;
        }
    }
}
