using System.Collections.Generic;
using Deckbuilder.Cards.Actions;
using Deckbuilder.Combat;
using Deckbuilder.Grid;

namespace Deckbuilder.Cards
{
    public static class CardExecutor
    {
        public static GridCell GetEffectiveCell(Entity _entity)
        {
            return _entity != null && _entity.TryGetModule(out EntityCombatMoverModule _combatMover) ? _combatMover.EffectiveCell : null;
        }

        public static bool IsWithinTargetZone(CombatManager _combatManager, CardInstance _card, Entity _caster, GridCell _targetCell)
        {
            GridCell _casterCell = GetEffectiveCell(_caster);
            if (_combatManager == null || _card == null || _targetCell == null || _casterCell == null)
                return false;

            ZoneDefinition _targetZone = _card.TargetZone;
            foreach (GridCell _cell in _combatManager.Grid.GetCellsInZone(_casterCell.Coordinate, _targetZone.Shape, _targetZone.MaxRange, _targetZone.MinRange))
            {
                if (_cell == _targetCell)
                    return true;
            }

            return false;
        }

        public static bool CanTarget(CombatManager _combatManager, CardInstance _card, Entity _caster, GridCell _targetCell)
        {
            if (!IsWithinTargetZone(_combatManager, _card, _caster, _targetCell))
                return false;

            if (_card.RequiresLineOfSight && !_combatManager.Grid.HasLineOfSight(GetEffectiveCell(_caster), _targetCell, out GridCell _blockingCell, _caster))
                return false;

            return true;
        }

        public static bool TryResolve(CombatManager _combatManager, CardInstance _card, Entity _caster, GridCell _targetCell, out CardActionContext _context)
        {
            if (!CanTarget(_combatManager, _card, _caster, _targetCell))
            {
                _context = default;
                return false;
            }

            List<Entity> _entitiesInEffectZone = new List<Entity>();
            foreach (GridCell _cell in _combatManager.Grid.GetCellsInZone(_targetCell.Coordinate, _card.EffectZone.Shape, _card.EffectZone.MaxRange, _card.EffectZone.MinRange))
            {
                if (_cell.Occupant != null)
                    _entitiesInEffectZone.Add(_cell.Occupant);
            }

            _context = new CardActionContext(_combatManager, _card, _caster, _targetCell, _entitiesInEffectZone);
            return true;
        }

        public static List<Entity> GetAffectedEntities(CardActionContext _context)
        {
            List<Entity> _affected = new List<Entity>();

            foreach (Entity _entity in _context.EntitiesInEffectZone)
            {
                foreach (CardAction _action in _context.Card.Actions)
                {
                    if (!_action.Affects(_entity))
                        continue;

                    _affected.Add(_entity);
                    break;
                }
            }

            return _affected;
        }

        public static void ApplyOnRelease(CardActionContext _context)
        {
            foreach (CardAction _action in _context.Card.Actions)
                _action.ApplyToCaster(_context);
        }

        public static void ApplyToZone(CardActionContext _context)
        {
            foreach (CardAction _action in _context.Card.Actions)
            {
                _action.ApplyToTargetCell(_context);

                foreach (Entity _entity in _context.EntitiesInEffectZone)
                    _action.ApplyToEntity(_entity, _context);
            }
        }

        public static void ApplyToEntity(CardActionContext _context, Entity _entity)
        {
            foreach (CardAction _action in _context.Card.Actions)
                _action.ApplyToEntity(_entity, _context);
        }

        public static void ApplyToTargetCell(CardActionContext _context)
        {
            foreach (CardAction _action in _context.Card.Actions)
                _action.ApplyToTargetCell(_context);
        }
    }
}
