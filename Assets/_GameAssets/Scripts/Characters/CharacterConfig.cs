using System;
using System.Collections.Generic;
using Deckbuilder.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Deckbuilder/Character Config")]
public class CharacterConfig : ScriptableObject
{
    [Serializable]
    public struct StartingDeckEntry
    {
        public CardConfig card;
        public bool upgraded;
        [Min(1)] public int copies;
    }

    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(2, 5)] private string _description;
    [SerializeField] private Sprite _portrait;
    [SerializeField, Required] private Entity _prefab;
    [SerializeField, Min(0)] private int _startingGold = 99;
    [SerializeField] private List<StartingDeckEntry> _startingDeck = new List<StartingDeckEntry>();
    [SerializeField] private bool _unlockedByDefault = true;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Portrait => _portrait;
    public Entity Prefab => _prefab;
    public int StartingGold => _startingGold;
    public IReadOnlyList<StartingDeckEntry> StartingDeck => _startingDeck;
    public bool UnlockedByDefault => _unlockedByDefault;
}
