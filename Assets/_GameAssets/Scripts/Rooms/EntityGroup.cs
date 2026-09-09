using System.Collections.Generic;
using Deckbuilder.Combat;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class EntityGroup : MonoBehaviour
{
    [SerializeField] private List<Entity> _entityPrefabs = new List<Entity>();

    [Header("debug")]
    [SerializeField] private Arena _arena;

    private readonly List<Entity> _entities = new List<Entity>();

    private void Awake()
    {
        Init(_entityPrefabs);
    }

    public void Init(IReadOnlyList<Entity> _entitiesToSpawn)
    {
        foreach (Entity _entityPrefab in _entitiesToSpawn)
            Spawn(_entityPrefab);
    }

    private Entity Spawn(Entity _entityPrefab)
    {
        Entity _entity = Instantiate(_entityPrefab, transform.position, CusRandom.Rotation(RotationAxis.Y), transform);
        _entity.onFightQuery += OnEntityFightQuery;
        _entities.Add(_entity);

        return _entity;
    }
    
    
    private void OnEntityFightQuery(Entity entity)
    {
        _arena.StartCombat(_entities);
        entity.onFightQuery -= OnEntityFightQuery;
    }
}
