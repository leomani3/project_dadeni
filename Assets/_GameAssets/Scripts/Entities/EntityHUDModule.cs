using Deckbuilder.StatusEffects;
using Lean.Pool;
using UnityEngine;

[RequireComponent(typeof(EntityHealthModule))]
[RequireComponent(typeof(StatusEffectModule))]
public class EntityHUDModule : EntityModule
{
    [SerializeField] private EntityHUD _hudPrefab;
    [SerializeField] private Transform _hudAnchor;
    [SerializeField] private string _displayName;
    [SerializeField, Min(1)] private int _level = 1;

    private EntityHealthModule _healthModule;
    private StatusEffectModule _statusEffectModule;
    private EntityHUD _hud;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out _healthModule);
        _healthModule.OnHealthChanged += HandleHealthChanged;
        _healthModule.OnBlockChanged += HandleBlockChanged;
        _healthModule.OnDeathStart += HandleDeathStart;

        Owner.TryGetModule(out _statusEffectModule);
        _statusEffectModule.OnStatusEffectChanged += HandleStatusEffectChanged;
        _statusEffectModule.OnStatusEffectRemoved += HandleStatusEffectRemoved;

        SpawnHUD();
    }

    public override void Cleanup()
    {
        base.Cleanup();
        UnsubscribeFromModules();
        DespawnHUD();
    }

    private void OnDestroy()
    {
        if (_hud != null)
            Destroy(_hud.gameObject);
    }

    private void SpawnHUD()
    {
        Transform canvasTransform = UIManager.Instance.GetCanvas<RunCanvas>().transform;
        _hud = LeanPool.Spawn(_hudPrefab, canvasTransform);
        _hud.Setup(_hudAnchor, _displayName, _level, _healthModule.MaxHealth, _healthModule.MaxHealth, _healthModule.Block);
    }

    private void DespawnHUD()
    {
        if (_hud == null) return;

        if (_hud.gameObject.activeSelf)
            LeanPool.Despawn(_hud);

        _hud = null;
    }

    private void UnsubscribeFromModules()
    {
        _healthModule.OnHealthChanged -= HandleHealthChanged;
        _healthModule.OnBlockChanged -= HandleBlockChanged;
        _healthModule.OnDeathStart -= HandleDeathStart;
        _statusEffectModule.OnStatusEffectChanged -= HandleStatusEffectChanged;
        _statusEffectModule.OnStatusEffectRemoved -= HandleStatusEffectRemoved;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth, float delta, bool isCrit, bool suppressFeedback)
    {
        _hud.SetHealth(currentHealth, maxHealth, !suppressFeedback);

        if (!suppressFeedback)
            SpawnHealthChangeText(delta, isCrit);
    }

    private void HandleBlockChanged(int block)
    {
        _hud.SetBlock(block);
    }

    private void HandleStatusEffectChanged(StatusEffect statusEffect)
    {
        _hud.SetStatusEffect(statusEffect);
    }

    private void HandleStatusEffectRemoved(StatusEffect statusEffect)
    {
        _hud.RemoveStatusEffect(statusEffect);
    }

    private void HandleDeathStart()
    {
        UnsubscribeFromModules();
        DespawnHUD();
    }

    private void SpawnHealthChangeText(float delta, bool isCrit)
    {
        if (delta < 0f)
            FloatingTextManager.Instance.SpawnWorldText(_hudAnchor.position, Mathf.Abs(delta).ToString("N0"), isCrit ? FloatingTextType.CriticalDamage : FloatingTextType.Damage);
        else if (delta > 0f)
            FloatingTextManager.Instance.SpawnWorldText(_hudAnchor.position, $"+{delta:N0}", FloatingTextType.Heal);
    }
}
