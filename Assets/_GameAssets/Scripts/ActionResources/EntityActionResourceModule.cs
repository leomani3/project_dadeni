using System;
using System.Collections.Generic;
using Deckbuilder.ActionResources;
using Stats;
using UnityEngine;

public class EntityActionResourceModule : EntityModule
{
    private class ResourcePool
    {
        public int Remaining;
        public int Max;
    }

    public Action<ActionResourceConfig, int, int> OnResourceChanged;

    [SerializeField] private List<ActionResourceConfig> m_resources = new();

    private readonly Dictionary<ActionResourceConfig, ResourcePool> m_pools = new();
    private EntityStatModule m_statModule;

    public IReadOnlyList<ActionResourceConfig> Resources => m_resources;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        m_pools.Clear();
        foreach (ActionResourceConfig _resource in m_resources)
            m_pools.Add(_resource, new ResourcePool());
    }

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out m_statModule);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        OnResourceChanged = null;
    }

    public override void OnCombatEnter()
    {
        base.OnCombatEnter();

        foreach (ActionResourceConfig _resource in m_resources)
        {
            int _max = ComputeMax(_resource);
            Set(_resource, _resource.StartsCombatFull ? _max : 0, _max);
        }
    }

    public override void OnCombatExit()
    {
        base.OnCombatExit();

        foreach (ActionResourceConfig _resource in m_resources)
            Set(_resource, 0, 0);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        foreach (ActionResourceConfig _resource in m_resources)
        {
            int _max = ComputeMax(_resource);
            Set(_resource, _resource.RefillsOnTurnStart ? _max : GetRemaining(_resource), _max);
        }
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();

        foreach (ActionResourceConfig _resource in m_resources)
        {
            if (_resource.GainOnTurnEnd > 0)
                Gain(_resource, _resource.GainOnTurnEnd);
        }
    }

    public bool Has(ActionResourceConfig _resource)
    {
        return m_pools.ContainsKey(_resource);
    }

    public int GetRemaining(ActionResourceConfig _resource)
    {
        return GetPool(_resource).Remaining;
    }

    public int GetMax(ActionResourceConfig _resource)
    {
        return GetPool(_resource).Max;
    }

    public bool CanAfford(ActionResourceConfig _resource, int _amount)
    {
        return _amount <= 0 || (Has(_resource) && _amount <= GetRemaining(_resource));
    }

    public bool TrySpend(ActionResourceConfig _resource, int _amount)
    {
        if (!CanAfford(_resource, _amount))
            return false;

        if (_amount > 0)
            Set(_resource, GetRemaining(_resource) - _amount, GetMax(_resource));

        return true;
    }

    public void Gain(ActionResourceConfig _resource, int _amount)
    {
        ResourcePool _pool = GetPool(_resource);
        Set(_resource, Mathf.Clamp(_pool.Remaining + _amount, 0, _pool.Max), _pool.Max);
    }

    private int ComputeMax(ActionResourceConfig _resource)
    {
        return Mathf.RoundToInt(m_statModule.GetValue(_resource.MaxStat));
    }

    private ResourcePool GetPool(ActionResourceConfig _resource)
    {
        if (!m_pools.TryGetValue(_resource, out ResourcePool _pool))
            throw new InvalidOperationException($"{Owner.name} has no {_resource.name} action resource.");

        return _pool;
    }

    private void Set(ActionResourceConfig _resource, int _remaining, int _max)
    {
        ResourcePool _pool = GetPool(_resource);
        _pool.Remaining = _remaining;
        _pool.Max = _max;
        OnResourceChanged?.Invoke(_resource, _remaining, _max);
    }
}
