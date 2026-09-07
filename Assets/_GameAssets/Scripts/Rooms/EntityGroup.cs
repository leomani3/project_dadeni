using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

public class EntityGroup : MonoBehaviour
{
    [SerializeField] private List<Entity> _entityPrefabs = new List<Entity>();

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
        SubscribeToEntityCallbacks(_entity);
        return _entity;
    }

    private void SubscribeToEntityCallbacks(Entity entity)
    {
        entity.onHoverEnter += OnEntityHoverEnter;
        entity.onHoverExit += OnEntityHoverExit;
        entity.onFightQuery += OnEntityFightQuery;
    }
    
    private void UnsubscribeToEntityCallbacks(Entity entity)
    {
        entity.onHoverEnter -= OnEntityHoverEnter;
        entity.onHoverExit -= OnEntityHoverExit;
        entity.onFightQuery -= OnEntityFightQuery;
    }

    private void OnEntityHoverEnter(Entity entity)
    {
        //todo : display some ui
    }

    private void OnEntityHoverExit(Entity entity)
    {
        //todo : hide some ui
    }

    private void OnEntityFightQuery(Entity entity)
    {
        //Todo : start fight
        
        UnsubscribeToEntityCallbacks(entity);
    }
}
