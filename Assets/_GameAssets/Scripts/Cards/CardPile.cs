using System;
using System.Collections.Generic;
using Utils;

namespace Deckbuilder.Cards
{
    public class CardPile
    {
        public event Action<CardPile, CardInstance> onCardAdded;
        public event Action<CardPile, CardInstance> onCardRemoved;
        public event Action<CardPile> onOrderChanged;

        private readonly List<CardInstance> m_cards = new();

        public string Name { get; }
        public IReadOnlyList<CardInstance> Cards => m_cards;
        public int Count => m_cards.Count;
        public bool IsEmpty => m_cards.Count == 0;

        public CardPile(string _name)
        {
            Name = _name;
        }

        public bool Contains(CardInstance _card)
        {
            return m_cards.Contains(_card);
        }

        public int IndexOf(CardInstance _card)
        {
            return m_cards.IndexOf(_card);
        }

        public void AddToTop(CardInstance _card)
        {
            Insert(_card, 0);
        }

        public void AddToBottom(CardInstance _card)
        {
            Insert(_card, m_cards.Count);
        }

        public void Insert(CardInstance _card, int _index)
        {
            if (m_cards.Contains(_card))
                throw new InvalidOperationException($"{_card} is already in the {Name} pile.");

            m_cards.Insert(_index, _card);
            onCardAdded?.Invoke(this, _card);
        }

        public bool Remove(CardInstance _card)
        {
            if (!m_cards.Remove(_card))
                return false;

            onCardRemoved?.Invoke(this, _card);
            return true;
        }

        public CardInstance RemoveAt(int _index)
        {
            CardInstance _card = m_cards[_index];
            m_cards.RemoveAt(_index);
            onCardRemoved?.Invoke(this, _card);

            return _card;
        }

        public bool TryPeekTop(out CardInstance _card)
        {
            _card = IsEmpty ? null : m_cards[0];
            return _card != null;
        }

        public bool TryFindFirst(Predicate<CardInstance> _match, out CardInstance _card)
        {
            int _index = m_cards.FindIndex(_match);
            _card = _index >= 0 ? m_cards[_index] : null;

            return _card != null;
        }

        public List<CardInstance> FindAll(Predicate<CardInstance> _match)
        {
            return m_cards.FindAll(_match);
        }

        public bool TryTakeTop(out CardInstance _card)
        {
            _card = IsEmpty ? null : RemoveAt(0);
            return _card != null;
        }

        public bool TryTakeBottom(out CardInstance _card)
        {
            _card = IsEmpty ? null : RemoveAt(m_cards.Count - 1);
            return _card != null;
        }

        public bool TryTakeFirst(Predicate<CardInstance> _match, out CardInstance _card)
        {
            int _index = m_cards.FindIndex(_match);
            _card = _index >= 0 ? RemoveAt(_index) : null;

            return _card != null;
        }

        public void Shuffle(RandomStream _random)
        {
            _random.Shuffle(m_cards);
            onOrderChanged?.Invoke(this);
        }

        public void MoveAllToBottomOf(CardPile _destination)
        {
            while (TryTakeTop(out CardInstance _card))
                _destination.AddToBottom(_card);
        }

        public void Clear()
        {
            while (!IsEmpty)
                RemoveAt(m_cards.Count - 1);
        }
    }
}
