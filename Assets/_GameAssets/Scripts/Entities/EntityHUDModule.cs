using System.Collections.Generic;
using Deckbuilder.StatusEffects;
using UnityEngine;

[RequireComponent(typeof(EntityHealthModule))]
[RequireComponent(typeof(StatusEffectModule))]
public class EntityHUDModule : EntityModule
{
    [SerializeField] private EntityHUD _hudPrefab;
    [SerializeField] private Transform _hudAnchor;
    [SerializeField, Min(0f)] private float _floatingTextSpacing = 40f;
    [SerializeField, Min(0f)] private float _floatingTextInterval = 0.05f;

    private struct PendingFloatingText
    {
        public string Message;
        public FloatingTextType Type;
        public Color? Color;
    }

    private EntityHealthModule _healthModule;
    private StatusEffectModule _statusEffectModule;
    private EntityHUD _hud;
    private int _lastBlock;
    private float _nextFloatingTextTime;
    private readonly Queue<PendingFloatingText> _pendingFloatingTexts = new Queue<PendingFloatingText>();
    private readonly List<float> _floatingTextSlotReleaseTimes = new List<float>();

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();

        if (!GameFlow.Instance.IsInGameplay)
            return;

        Owner.TryGetModule(out _healthModule);
        _healthModule.OnHealthChanged += HandleHealthChanged;
        _healthModule.OnBlockChanged += HandleBlockChanged;
        _healthModule.OnDeathStart += HandleDeathStart;

        Owner.TryGetModule(out _statusEffectModule);
        _statusEffectModule.OnStatusEffectChanged += HandleStatusEffectChanged;
        _statusEffectModule.OnStatusEffectRemoved += HandleStatusEffectRemoved;
        _statusEffectModule.OnStatusEffectApplied += HandleStatusEffectApplied;

        SpawnHUD();
    }

    public override void Cleanup()
    {
        base.Cleanup();

        _pendingFloatingTexts.Clear();
        _floatingTextSlotReleaseTimes.Clear();

        if (_healthModule == null)
            return;

        UnsubscribeFromModules();
        DespawnHUD();
    }

    private void Update()
    {
        if (_pendingFloatingTexts.Count == 0 || Time.time < _nextFloatingTextTime)
            return;

        SpawnFloatingText(_pendingFloatingTexts.Dequeue());
        _nextFloatingTextTime = Time.time + _floatingTextInterval;
    }

    private void OnEnable()
    {
        if (_hud != null)
            _hud.SetHidden(false);
    }

    private void OnDisable()
    {
        if (_hud != null)
            _hud.SetHidden(true);
    }

    private void OnDestroy()
    {
        DespawnHUD();
    }

    private void SpawnHUD()
    {
        Transform canvasTransform = UIManager.Instance.GetCanvas<RunCanvas>().transform;
        _hud = Instantiate(_hudPrefab, canvasTransform);
        _hud.Setup(_hudAnchor, _healthModule.MaxHealth, _healthModule.MaxHealth, _healthModule.Block);
        _lastBlock = _healthModule.Block;
    }

    private void DespawnHUD()
    {
        if (_hud == null) return;

        Destroy(_hud.gameObject);
        _hud = null;
    }

    private void UnsubscribeFromModules()
    {
        _healthModule.OnHealthChanged -= HandleHealthChanged;
        _healthModule.OnBlockChanged -= HandleBlockChanged;
        _healthModule.OnDeathStart -= HandleDeathStart;
        _statusEffectModule.OnStatusEffectChanged -= HandleStatusEffectChanged;
        _statusEffectModule.OnStatusEffectRemoved -= HandleStatusEffectRemoved;
        _statusEffectModule.OnStatusEffectApplied -= HandleStatusEffectApplied;
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

        if (block > _lastBlock)
            QueueFloatingText($"+{block - _lastBlock}", FloatingTextType.Block, GameConfig.Instance.uiSettings.shieldColor);

        _lastBlock = block;
    }

    private void HandleStatusEffectChanged(StatusEffect statusEffect)
    {
        _hud.SetStatusEffect(statusEffect);
    }

    private void HandleStatusEffectRemoved(StatusEffect statusEffect)
    {
        _hud.RemoveStatusEffect(statusEffect);
    }

    private void HandleStatusEffectApplied(StatusEffectConfig config, int stacks)
    {
        if (stacks > 0)
            QueueFloatingText(config.DisplayName, FloatingTextType.StatusEffect, null);
    }

    private void HandleDeathStart()
    {
        UnsubscribeFromModules();
        DespawnHUD();
    }

    private void SpawnHealthChangeText(float delta, bool isCrit)
    {
        if (delta < 0f)
            QueueFloatingText(Mathf.Abs(delta).ToString("N0"), isCrit ? FloatingTextType.CriticalDamage : FloatingTextType.Damage, null);
        else if (delta > 0f)
            QueueFloatingText($"+{delta:N0}", FloatingTextType.Heal, null);
    }

    private void QueueFloatingText(string message, FloatingTextType type, Color? color)
    {
        _pendingFloatingTexts.Enqueue(new PendingFloatingText { Message = message, Type = type, Color = color });
    }

    private void SpawnFloatingText(PendingFloatingText text)
    {
        int slot = _floatingTextSlotReleaseTimes.FindIndex(releaseTime => releaseTime <= Time.time);

        if (slot < 0)
        {
            slot = _floatingTextSlotReleaseTimes.Count;
            _floatingTextSlotReleaseTimes.Add(0f);
        }

        float lifetime = FloatingTextManager.Instance.SpawnWorldText(_hudAnchor.position, text.Message, text.Type, text.Color, slot * _floatingTextSpacing);
        _floatingTextSlotReleaseTimes[slot] = Time.time + lifetime;
    }
}
