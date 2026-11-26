using System;
using Stats;
using UnityEngine;

public class EntityActionPointModule : EntityModule
{
    public Action<int, int> OnActionPointsChanged;

    public int RemainingActionPoints { get; private set; }
    public int MaxActionPoints { get; private set; }

    private EntityStatModule m_statModule;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out m_statModule);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        OnActionPointsChanged = null;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        Refill();
    }

    public override void OnCombatExit()
    {
        base.OnCombatExit();

        SetPoints(0, 0);
    }

    public bool CanAfford(int _cost)
    {
        return _cost <= RemainingActionPoints;
    }

    public bool TrySpend(int _cost)
    {
        if (!CanAfford(_cost))
            return false;

        SetPoints(RemainingActionPoints - _cost, MaxActionPoints);
        return true;
    }

    public void Refill()
    {
        int _max = Mathf.RoundToInt(m_statModule.GetValue(StatType.ActionPoints));
        SetPoints(_max, _max);
    }

    private void SetPoints(int _remaining, int _max)
    {
        RemainingActionPoints = _remaining;
        MaxActionPoints = _max;
        OnActionPointsChanged?.Invoke(RemainingActionPoints, MaxActionPoints);
    }
}
