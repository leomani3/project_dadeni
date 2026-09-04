using System;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public abstract class CardAction
    {
        public abstract void Execute(CardActionContext _context);
    }
}
