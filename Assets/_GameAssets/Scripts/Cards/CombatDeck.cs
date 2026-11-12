using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Deckbuilder.Cards
{
    [Serializable]
    public class CombatDeck
    {
        public event Action onDiscardReshuffledIntoDraw;

        [SerializeField, Min(0)] private int m_cardsDrawnPerTurn = 5;
        [SerializeField, Min(1)] private int m_maxHandSize = 10;

        private RandomStream m_random;

        public CardPile DrawPile { get; } = new CardPile("Draw");
        public CardPile Hand { get; } = new CardPile("Hand");
        public CardPile DiscardPile { get; } = new CardPile("Discard");
        public CardPile ExhaustPile { get; } = new CardPile("Exhaust");

        public bool IsHandFull => Hand.Count >= m_maxHandSize;

        public void StartCombat(IEnumerable<CardInstance> _deck, RandomStream _random)
        {
            m_random = _random;
            ClearAllPiles();

            foreach (CardInstance _card in _deck)
                DrawPile.AddToBottom(_card);

            DrawPile.Shuffle(m_random);
        }

        public void EndCombat()
        {
            ClearAllPiles();
            m_random = null;
        }

        public int DrawTurnHand()
        {
            return Draw(m_cardsDrawnPerTurn);
        }

        public int Draw(int _count)
        {
            int _drawnCount = 0;
            while (_drawnCount < _count && TryDraw(out CardInstance _card))
                _drawnCount++;

            return _drawnCount;
        }

        public bool TryDraw(out CardInstance _card)
        {
            if (DrawPile.IsEmpty)
                ReshuffleDiscardIntoDraw();

            if (!DrawPile.TryTakeTop(out _card))
                return false;

            ReceiveDrawnCard(_card);
            return true;
        }

        public bool TryDrawFirst(Predicate<CardInstance> _match, out CardInstance _card)
        {
            if (!DrawPile.TryTakeFirst(_match, out _card))
                return false;

            ReceiveDrawnCard(_card);
            return true;
        }

        public bool TryDrawFirstOfType(CardType _type, out CardInstance _card)
        {
            return TryDrawFirst(_candidate => _candidate.Type == _type, out _card);
        }

        public bool TryDrawSpecific(CardInstance _wantedCard)
        {
            return TryDrawFirst(_candidate => _candidate == _wantedCard, out CardInstance _card);
        }

        public void Play(CardInstance _card)
        {
            if (!Hand.Remove(_card))
                throw new InvalidOperationException($"{_card} cannot be played because it is not in the hand.");

            if (_card.Exhausts)
                ExhaustPile.AddToBottom(_card);
            else
                DiscardPile.AddToBottom(_card);
        }

        public void DiscardHand()
        {
            while (Hand.TryTakeTop(out CardInstance _card))
                DiscardPile.AddToBottom(_card);
        }

        public void MoveCard(CardInstance _card, CardPile _destination, int _destinationIndex)
        {
            GetPileContaining(_card).Remove(_card);
            _destination.Insert(_card, Mathf.Clamp(_destinationIndex, 0, _destination.Count));
        }

        public void Exhaust(CardInstance _card)
        {
            MoveCard(_card, ExhaustPile, ExhaustPile.Count);
        }

        public CardPile GetPileContaining(CardInstance _card)
        {
            foreach (CardPile _pile in EnumeratePiles())
            {
                if (_pile.Contains(_card))
                    return _pile;
            }

            throw new InvalidOperationException($"{_card} is in none of the combat piles.");
        }

        public void ReshuffleDiscardIntoDraw()
        {
            if (DiscardPile.IsEmpty)
                return;

            DiscardPile.MoveAllToBottomOf(DrawPile);
            DrawPile.Shuffle(m_random);
            onDiscardReshuffledIntoDraw?.Invoke();
        }

        private void ReceiveDrawnCard(CardInstance _card)
        {
            if (IsHandFull)
                DiscardPile.AddToBottom(_card);
            else
                Hand.AddToBottom(_card);
        }

        private IEnumerable<CardPile> EnumeratePiles()
        {
            yield return DrawPile;
            yield return Hand;
            yield return DiscardPile;
            yield return ExhaustPile;
        }

        private void ClearAllPiles()
        {
            foreach (CardPile _pile in EnumeratePiles())
                _pile.Clear();
        }
    }
}
