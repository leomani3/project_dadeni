using System.Collections.Generic;

namespace Deckbuilder.Cards
{
    public class PlayerDeckModule : EntityModule
    {
        private readonly List<CardInstance> m_cards = new();

        public IReadOnlyList<CardInstance> Cards => m_cards;

        public void Load(IEnumerable<CardInstance> _cards)
        {
            m_cards.Clear();
            m_cards.AddRange(_cards);
        }

        public void AddCard(CardInstance _card)
        {
            m_cards.Add(_card);
        }

        public bool RemoveCard(CardInstance _card)
        {
            return m_cards.Remove(_card);
        }
    }
}
