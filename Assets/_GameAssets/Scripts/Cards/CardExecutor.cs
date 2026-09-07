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
            return _entity != null && _entity.TryGetModule(out EntityGridModule _gridModule) ? _gridModule.EffectiveCell : null;
        }

        public static bool IsWithinTargetZone(Arena _arena, CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            GridCell _casterCell = GetEffectiveCell(_caster);
            if (_arena == null || _card == null || _targetCell == null || _casterCell == null)
                return false;

            ZoneDefinition _targetZone = _card.TargetZone;
            foreach (GridCell _cell in _arena.Grid.GetCellsInZone(_casterCell.Coordinate, _targetZone.Shape, _targetZone.MaxRange, _targetZone.MinRange))
            {
                if (_cell == _targetCell)
                    return true;
            }

            return false;
        }

        public static bool CanTarget(Arena _arena, CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            if (!IsWithinTargetZone(_arena, _card, _caster, _targetCell))
                return false;

            if (_card.RequiresLineOfSight && !_arena.Grid.HasLineOfSight(GetEffectiveCell(_caster), _targetCell, out GridCell _blockingCell, _caster))
                return false;

            return true;
        }

        public static bool Execute(Arena _arena, CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            if (!CanTarget(_arena, _card, _caster, _targetCell))
                return false;

            List<Entity> _entitiesInEffectZone = new List<Entity>();
            foreach (GridCell _cell in _arena.Grid.GetCellsInZone(_targetCell.Coordinate, _card.EffectZone.Shape, _card.EffectZone.MaxRange, _card.EffectZone.MinRange))
            {
                if (_cell.Occupant != null)
                    _entitiesInEffectZone.Add(_cell.Occupant);
            }

            CardActionContext _context = new CardActionContext(_arena, _card, _caster, _targetCell, _entitiesInEffectZone);
            foreach (CardAction _action in _card.Actions)
                _action.Execute(_context);

            return true;
        }
    }
}
