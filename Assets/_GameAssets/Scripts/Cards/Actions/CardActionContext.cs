using System.Collections.Generic;
using Deckbuilder.Grid;

namespace Deckbuilder.Cards.Actions
{
    public readonly struct CardActionContext
    {
        public readonly CardConfig Card;
        public readonly Entity Caster;
        public readonly GridCell TargetCell;
        public readonly IReadOnlyList<Entity> EntitiesInEffectZone;

        public CardActionContext(CardConfig _card, Entity _caster, GridCell _targetCell, IReadOnlyList<Entity> _entitiesInEffectZone)
        {
            Card = _card;
            Caster = _caster;
            TargetCell = _targetCell;
            EntitiesInEffectZone = _entitiesInEffectZone;
        }
    }
}
