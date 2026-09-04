using System;
using System.Collections.Generic;
using MyBox;
using UnityEngine;
using Utils;

public class EntityManager : Singleton<EntityManager>
{
    public Action<Entity> onEntityRegistered;
    public Action<Entity> onEntityUnregistered;

    [SerializeField] private Entity _player;
    [SerializeField] private List<Entity> _enemies = new List<Entity>();
    [SerializeField] private List<Entity> _entities = new List<Entity>();
    [SerializeField] private SerializableDictionary<Collider, Entity> _entitiesByCollider = new SerializableDictionary<Collider, Entity>();

    public List<Entity> AllEntities => _entities;
    public SerializableDictionary<Collider, Entity> EntitiesByCollider => _entitiesByCollider;
    public List<Entity> Enemies => _enemies;
    public Entity Player => _player;

    internal void Register(Entity entity)
    {
        if (entity == null) return;

        if (!_entities.Contains(entity))
            _entities.Add(entity);

        if (entity.Collider != null && !_entitiesByCollider.ContainsKey(entity.Collider))
            _entitiesByCollider.Add(entity.Collider, entity);

        if (entity.TryGetModule(out EntityTeamModule teamModule))
        {
            if (teamModule.Team == Team.Ally)
                _player = entity;
            else if (teamModule.Team == Team.Enemy && !_enemies.Contains(entity))
                _enemies.Add(entity);
        }

        onEntityRegistered?.Invoke(entity);
    }

    internal void Unregister(Entity entity)
    {
        if (entity == null) return;

        _entities.Remove(entity);

        if (entity.Collider != null)
            _entitiesByCollider.Remove(entity.Collider);

        if (entity.TryGetModule(out EntityTeamModule teamModule))
        {
            if (teamModule.Team == Team.Ally)
                _player = null;
            else if (teamModule.Team == Team.Enemy)
                _enemies.Remove(entity);
        }

        onEntityUnregistered?.Invoke(entity);
    }
}
