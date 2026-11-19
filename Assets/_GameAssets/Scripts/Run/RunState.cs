using System;
using System.Collections.Generic;
using Deckbuilder.Cards;

[Serializable]
public struct SavedCard
{
    public string cardId;
    public bool upgraded;

    public SavedCard(string _cardId, bool _upgraded)
    {
        cardId = _cardId;
        upgraded = _upgraded;
    }
}

[Serializable]
public class RunState
{
    public long seed;
    public string characterId;
    public RunPhase phase;
    public int currentRoomIndex;
    public bool currentRoomCompleted;
    public List<int> visitedRoomIndices = new List<int>();
    public float currentHealth;
    public float maxHealthBonus;
    public int gold;
    public List<SavedCard> deck = new List<SavedCard>();
    public List<string> itemIds = new List<string>();
    public int roomsCleared;
    public long startedAtUtcTicks;

    public bool HasSavedHealth => currentHealth > 0f;

    public static RunState Create(CharacterConfig _character, long _seed, int _startRoomIndex)
    {
        RunState _runState = new RunState
        {
            seed = _seed,
            characterId = _character.Id,
            phase = RunPhase.InRoom,
            currentRoomIndex = _startRoomIndex,
            currentRoomCompleted = false,
            maxHealthBonus = 0f,
            gold = _character.StartingGold,
            roomsCleared = 0,
            startedAtUtcTicks = DateTime.UtcNow.Ticks
        };

        _runState.visitedRoomIndices.Add(_startRoomIndex);

        foreach (CharacterConfig.StartingDeckEntry _entry in _character.StartingDeck)
        {
            for (int _copy = 0; _copy < _entry.copies; _copy++)
                _runState.deck.Add(new SavedCard(_entry.card.Id, _entry.upgraded));
        }

        return _runState;
    }

    public List<CardInstance> BuildDeck()
    {
        List<CardInstance> _cards = new List<CardInstance>();

        foreach (SavedCard _savedCard in deck)
            _cards.Add(new CardInstance(GameAssets.Instance.GetCard(_savedCard.cardId), _savedCard.upgraded));

        return _cards;
    }

    public void CaptureDeck(IReadOnlyList<CardInstance> _cards)
    {
        deck.Clear();

        foreach (CardInstance _card in _cards)
            deck.Add(new SavedCard(_card.Config.Id, _card.IsUpgraded));
    }

    public void EnterRoom(int _roomIndex)
    {
        currentRoomIndex = _roomIndex;
        currentRoomCompleted = false;
        phase = RunPhase.InRoom;

        if (!visitedRoomIndices.Contains(_roomIndex))
            visitedRoomIndices.Add(_roomIndex);
    }

    public void CompleteRoom()
    {
        if (currentRoomCompleted)
            return;

        currentRoomCompleted = true;
        phase = RunPhase.InRoom;
        roomsCleared++;
    }
}
