using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Cards
{
    public class PlayerDeckModule : EntityModule
    {
        [Serializable]
        private struct StartingDeckEntry
        {
            public CardConfig card;
            public bool upgraded;
            [Min(1)] public int copies;
        }

        [SerializeField] private List<StartingDeckEntry> m_startingDeck = new();

        private readonly List<CardInstance> m_cards = new();

        public IReadOnlyList<CardInstance> Cards => m_cards;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ResetToStartingDeck();
        }

        public void ResetToStartingDeck()
        {
            m_cards.Clear();

            foreach (StartingDeckEntry _entry in m_startingDeck)
            {
                for (int i = 0; i < _entry.copies; i++)
                    m_cards.Add(new CardInstance(_entry.card, _entry.upgraded));
            }
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
