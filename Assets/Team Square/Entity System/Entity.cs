using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Action<Entity> onFightQuery;

    [SerializeField] private Collider _collider;
    [SerializeField] private EntityType _entityType;

    private readonly Dictionary<Type, EntityModule> _modulesByType = new Dictionary<Type, EntityModule>();

    public EntityType EntityType => _entityType;
    public Collider Collider => _collider;
    public bool IsTakingTurn { get; private set; }

    public bool TryGetModule<T>(out T module) where T : EntityModule
    {
        if (_modulesByType.TryGetValue(typeof(T), out var storedModule))
        {
            module = (T)storedModule;
            return true;
        }

        module = null;
        return false;
    }

    public void Despawn()
    {
        if (TryGetModule(out EntityHealthModule healthModule))
        {
            healthModule.OnDeathStart -= RemoveFromEntityManager;
            healthModule.OnDeath -= Despawn;
        }

        RemoveFromEntityManager();

        foreach (var module in _modulesByType.Values.Distinct())
            module.Cleanup();

        Destroy(gameObject);
    }

    private void Reset()
    {
        _collider = GetComponent<Collider>();
    }

    private void Awake()
    {
        InitializeModules();
        RegisterInEntityManager();
    }

    private void OnDestroy()
    {
        EntityManager.Instance?.Unregister(this, notifyListeners: false);
    }

    private void InitializeModules()
    {
        var modules = GetComponents<EntityModule>();
        foreach (var module in modules)
        {
            var type = module.GetType();
            while (type != null && typeof(EntityModule).IsAssignableFrom(type))
            {
                _modulesByType.TryAdd(type, module);
                type = type.BaseType;
            }

            module.Initialize(this);
        }

        foreach (var module in _modulesByType.Values.Distinct())
            module.OnAllModuleInitialized();

        if (TryGetModule(out EntityHealthModule healthModule))
        {
            healthModule.OnDeathStart += RemoveFromEntityManager;
            healthModule.OnDeath += Despawn;
        }
    }

    private void RegisterInEntityManager()
    {
        if (_collider != null)
            _collider.enabled = true;

        EntityManager.Instance?.Register(this);
    }

    private void RemoveFromEntityManager()
    {
        if (_collider != null)
            _collider.enabled = false;

        EntityManager.Instance?.Unregister(this);
    }

    public void QueryFight()
    {
        onFightQuery?.Invoke(this);
    }

    public void OnCombatEnter()
    {
        foreach (var module in _modulesByType.Values.Distinct())
            module.OnCombatEnter();
    }

    public void OnCombatExit()
    {
        IsTakingTurn = false;

        foreach (var module in _modulesByType.Values.Distinct())
            module.OnCombatExit();
    }

    [ContextMenu("Start Turn")]
    public void OnTurnStart()
    {
        IsTakingTurn = true;

        foreach (var module in _modulesByType.Values.Distinct())
            module.OnTurnStart();
    }

    [ContextMenu("End Turn")]
    public void OnTurnEnd()
    {
        foreach (var module in _modulesByType.Values.Distinct())
            module.OnTurnEnd();

        IsTakingTurn = false;
    }
}
