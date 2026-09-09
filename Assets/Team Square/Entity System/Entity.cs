using System;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;

public class Entity : MonoBehaviour, IPoolable
{
    public Action<Entity> onFightQuery;
    
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _collider;
    [SerializeField] private EntityType _entityType;

    private readonly Dictionary<Type, EntityModule> _modulesByType = new Dictionary<Type, EntityModule>();
    private bool _modulesInitialized;

    public EntityType EntityType => _entityType;
    public Animator Animator => _animator;
    public Collider Collider => _collider;

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

    public void OnSpawn()
    {
        InitializeModules();
        RegisterInEntityManager();
    }

    public void OnDespawn()
    {
        foreach (var module in _modulesByType.Values.Distinct())
            module.Cleanup();

        _modulesInitialized = false;
    }

    public void Despawn()
    {
        if (TryGetModule(out EntityHealthModule healthModule))
        {
            healthModule.OnDeathStart -= RemoveFromEntityManager;
            healthModule.OnDeath -= Despawn;
        }

        LeanPool.Despawn(this);
    }

    private void Reset()
    {
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider>();
    }

    private void Awake()
    {
        InitializeModules();
        RegisterInEntityManager();
    }

    private void InitializeModules()
    {
        if (_modulesInitialized) return;

        _modulesInitialized = true;

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
        foreach (var module in _modulesByType.Values.Distinct())
            module.OnCombatExit();
    }
}