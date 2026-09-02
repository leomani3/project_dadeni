using System.Collections.Generic;
using Deckbuilder.Cards.Actions;
using Deckbuilder.Grid;
using UnityEngine;

namespace Deckbuilder.Cards
{
    public static class CardExecutor
    {
        public static GridCell GetEffectiveCell(Entity _entity)
        {
            return _entity != null && _entity.TryGetModule(out EntityGridModule _gridModule) ? _gridModule.EffectiveCell : null;
        }

        public static bool IsWithinTargetZone(CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            GridCell _casterCell = GetEffectiveCell(_caster);
            if (_card == null || _targetCell == null || _casterCell == null || GridManager.Instance == null)
                return false;

            ZoneDefinition _targetZone = _card.TargetZone;
            foreach (GridCell _cell in GridManager.Instance.GetCellsInZone(_casterCell.Coordinate, _targetZone.Shape, _targetZone.Size, _targetZone.MinSize))
            {
                if (_cell == _targetCell)
                    return true;
            }

            return false;
        }

        public static bool CanTarget(CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            if (!IsWithinTargetZone(_card, _caster, _targetCell))
                return false;

            if (_card.RequiresLineOfSight && !GridManager.Instance.HasLineOfSight(GetEffectiveCell(_caster), _targetCell, out GridCell _blockingCell, _caster))
                return false;

            return true;
        }

        public static bool Execute(CardConfig _card, Entity _caster, GridCell _targetCell)
        {
            if (!CanTarget(_card, _caster, _targetCell))
                return false;

            List<Entity> _affectedEntities = new List<Entity>();
            foreach (GridCell _cell in GridManager.Instance.GetCellsInZone(_targetCell.Coordinate, _card.EffectZone.Shape, _card.EffectZone.Size, _card.EffectZone.MinSize))
            {
                if (_cell.Occupant != null)
                    _affectedEntities.Add(_cell.Occupant);
            }

            foreach (CardAction _action in _card.Actions)
                ApplyAction(_action, _caster, _targetCell, _affectedEntities);

            return true;
        }

        private static void ApplyAction(CardAction _action, Entity _caster, GridCell _targetCell, List<Entity> _affectedEntities)
        {
            switch (_action)
            {
                case DealDamageCardAction _dealDamage:
                    foreach (Entity _entity in _affectedEntities)
                    {
                        if (_entity.TryGetModule(out EntityHealthModule _health))
                            _health.TakeDamage(_dealDamage.Damage, false);
                    }

                    break;

                case HealCardAction _heal:
                    foreach (Entity _entity in _affectedEntities)
                    {
                        if (_entity.TryGetModule(out EntityHealthModule _health))
                            _health.Heal(_heal.HealAmount);
                    }

                    break;

                case ApplyStatusEffectCardAction _applyStatusEffect:
                    Debug.LogWarning($"Status effect system not implemented yet, skipping {_applyStatusEffect.StatusEffect?.DisplayName}.");
                    break;

                case SummonCardAction _summon:
                    if (_targetCell.IsOccupied)
                        Debug.LogWarning("Cannot summon, target cell is already occupied.");
                    else if (CombatManager.Instance != null)
                        CombatManager.Instance.SpawnEntity(_summon.EntityPrefab, _targetCell);

                    break;
            }
        }
    }
}
