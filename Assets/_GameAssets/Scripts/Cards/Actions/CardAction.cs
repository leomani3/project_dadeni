using System;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public abstract class CardAction
    {
        public abstract string DescriptionFormat { get; }
        public abstract object[] DescriptionValues { get; }

        public virtual void ApplyToCaster(CardActionContext _context)
        {
        }

        public virtual void ApplyToTargetCell(CardActionContext _context)
        {
        }

        public virtual bool Affects(Entity _entity)
        {
            return false;
        }

        public virtual void ApplyToEntity(Entity _entity, CardActionContext _context)
        {
        }
    }
}
