using System;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public abstract class CardAction
    {
        public abstract string DescriptionFormat { get; }
        public abstract object[] DescriptionValues { get; }

        public abstract void Execute(CardActionContext _context);
    }
}
