using Lean.Pool;
using UnityEngine;

[RequireComponent(typeof(EntityHealthModule))]
public class EntityHUDModule : EntityModule
{
    [SerializeField] private EntityHUD _hudPrefab;
    [SerializeField] private Transform _hudAnchor;
    [SerializeField] private string _displayName;
    [SerializeField, Min(1)] private int _level = 1;

    private EntityHealthModule _healthModule;
    private EntityHUD _hud;

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        Owner.TryGetModule(out _healthModule);
        _healthModule.OnHealthChanged += HandleHealthChanged;
        _healthModule.OnDeathStart += HandleDeathStart;

        SpawnHUD();
    }

    public override void Cleanup()
    {
        base.Cleanup();
        UnsubscribeFromModules();
        DespawnHUD();
    }

    private void SpawnHUD()
    {
        Transform canvasTransform = UIManager.Instance.GetCanvas<GameCanvas>().transform;
        _hud = LeanPool.Spawn(_hudPrefab, canvasTransform);
        _hud.Setup(_hudAnchor, _displayName, _level, _healthModule.MaxHealth, _healthModule.MaxHealth);
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
        _healthModule.OnDeathStart -= HandleDeathStart;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth, float delta, bool isCrit, bool suppressFeedback)
    {
        _hud.SetHealth(currentHealth, maxHealth, !suppressFeedback);

        if (!suppressFeedback)
            SpawnHealthChangeText(delta, isCrit);
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
