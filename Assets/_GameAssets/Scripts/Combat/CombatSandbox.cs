using System;
using System.Collections.Generic;
using Deckbuilder.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/CombatSandbox")]
public class CombatSandbox : ScriptableObject
{
    private static CombatSandbox _instance;
    public static CombatSandbox Instance => _instance ?? Load();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticInstance()
    {
        _instance = null;
    }

    private static CombatSandbox Load()
    {
        _instance = Resources.Load<CombatSandbox>(nameof(CombatSandbox));

        if (_instance == null)
            throw new InvalidOperationException($"No {nameof(CombatSandbox)} asset was found in a Resources folder.");

        return _instance;
    }

    [Serializable]
    public struct EnemyEntry
    {
        public Entity prefab;
        [Min(1)] public int count;
    }

    [SerializeField] private bool _bootStraightIntoCombat = true;
    [SerializeField, Required] private CharacterConfig _character;
    [SerializeField] private string _seed = "COMBAT";
    [SerializeField] private List<EnemyEntry> _enemies = new List<EnemyEntry>();

    public bool BootStraightIntoCombat => _bootStraightIntoCombat;
    public CharacterConfig Character => _character;
    public string Seed => _seed;
    public IReadOnlyList<EnemyEntry> Enemies => _enemies;

    public List<CardInstance> BuildDeck()
    {
        List<CardInstance> _cards = new List<CardInstance>();

        foreach (CharacterConfig.StartingDeckEntry _entry in _character.StartingDeck)
        {
            for (int _copy = 0; _copy < _entry.copies; _copy++)
                _cards.Add(new CardInstance(_entry.card, _entry.upgraded));
        }

        return _cards;
    }

    public List<Entity> BuildEnemyPrefabs()
    {
        List<Entity> _prefabs = new List<Entity>();

        foreach (EnemyEntry _entry in _enemies)
        {
            for (int _copy = 0; _copy < _entry.count; _copy++)
                _prefabs.Add(_entry.prefab);
        }

        return _prefabs;
    }

    public string ToDebugString()
    {
        List<string> _deckParts = new List<string>();

        foreach (CharacterConfig.StartingDeckEntry _entry in _character.StartingDeck)
            _deckParts.Add($"{_entry.card.Title}{(_entry.upgraded ? "+" : string.Empty)} x{_entry.copies}");

        List<string> _enemyParts = new List<string>();

        foreach (EnemyEntry _entry in _enemies)
            _enemyParts.Add($"{_entry.prefab.name} x{_entry.count}");

        return $"character={_character.Id} seed={_seed} deck=[{string.Join(", ", _deckParts)}] enemies=[{string.Join(", ", _enemyParts)}]";
    }

    [Button]
    private void SetEnemiesToSinglePrefab(Entity _prefab, int _count)
    {
        _enemies.Clear();
        _enemies.Add(new EnemyEntry { prefab = _prefab, count = Mathf.Max(1, _count) });

        MarkDirty();
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
    }
}
