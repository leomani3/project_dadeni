using System;
using System.Collections;
using DG.Tweening;
using Lean.Pool;
using Stats;
using UnityEngine;
using Utils;

public class EntityHealthModule : EntityModule
{
    public Action<float, float, float, bool, bool> OnHealthChanged;
    public Action<float, float> OnDamageTaken;
    public Action<float, float> OnHealed;
    public Action OnDeathStart;
    public Action OnDeath;

    [SerializeField] private ParticleSystem _deathVFXPrefab;
    [SerializeField] private Vector3 _damagePunchScale = new Vector3(0.3f, -0.2f, 0f);
    [SerializeField, Min(0f)] private float _damagePunchDuration = 0.35f;
    [SerializeField, Min(1)] private int _damagePunchVibrato = 6;
    [SerializeField, Range(0f, 1f)] private float _damagePunchElasticity = 0.5f;
    [SerializeField] private SoundKeys _damageSound = SoundKeys._None;
    [SerializeField] private SoundKeys _deathSound = SoundKeys._None;
    [SerializeField] private string _deathAnimationState = "Death";
    [SerializeField, Min(0f)] private float _despawnDelayAfterDeath = 5f;
    [SerializeField] private float _maxHealthFallbackWithoutStatModule = 100f;

    private float _currentHealth;
    protected bool _isDead;
    private Tween _damagePunchTween;
    protected EntityStatModule _statModule;

    public bool IsDead => _isDead;
    public float CurrentHealth => _currentHealth;

    public float MaxHealth
    {
        get
        {
            if (Owner.TryGetModule(out EntityStatModule statModule))
                return statModule.GetValue(StatType.MaxHealth);

            this.LogWarning($"No EntityStatModule attached, falling back to {_maxHealthFallbackWithoutStatModule} MaxHealth.");
            return _maxHealthFallbackWithoutStatModule;
        }
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        _isDead = false;
    }

    public override void OnAllModuleInitialized()
    {
        base.OnAllModuleInitialized();
        Owner.TryGetModule(out _statModule);
        _currentHealth = MaxHealth;
    }

    public override void Cleanup()
    {
        OnHealthChanged = null;
        OnDamageTaken = null;
        OnHealed = null;
        OnDeathStart = null;
        OnDeath = null;
    }

    public void RefillToMaxHealth()
    {
        _currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, 0f, false, true);
    }

    public void TakeDamage(float amount, bool isCrit, bool suppressFeedback = false)
    {
        if (_isDead || amount <= 0f) return;

        float healthBeforeDamage = _currentHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        float healthDelta = _currentHealth - healthBeforeDamage;

        if (!suppressFeedback)
            PlayDamageFeedback();

        OnDamageTaken?.Invoke(amount, _currentHealth);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, healthDelta, isCrit, suppressFeedback);

        if (_currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        float healthBeforeHeal = _currentHealth;
        _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        float healthDelta = _currentHealth - healthBeforeHeal;

        if (healthDelta <= 0f) return;

        OnHealed?.Invoke(healthDelta, _currentHealth);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, healthDelta, false, false);
    }

    protected virtual void PlayDamageFeedback()
    {
        PlayDamagePunchScale();

        if (Owner.TryGetModule(out EntitySheenModule sheenModule))
            sheenModule.PlayWhiteSheen();

        if (_damageSound != SoundKeys._None)
            SoundManager.Instance?.PlaySound(_damageSound);
    }

    protected virtual void Die()
    {
        _isDead = true;

        _damagePunchTween?.Kill(complete: true);

        if (_deathVFXPrefab != null)
            LeanPool.Spawn(_deathVFXPrefab, transform.position, Quaternion.identity);

        if (_deathSound != SoundKeys._None)
            SoundManager.Instance?.PlaySound(_deathSound);

        OnDeathStart?.Invoke();
        StartCoroutine(PlayDeathAnimationThenNotify());
    }

    private void PlayDamagePunchScale()
    {
        _damagePunchTween?.Kill(complete: true);
        Owner.transform.localScale = Vector3.one;

        _damagePunchTween = Owner.transform
            .DOPunchScale(_damagePunchScale, _damagePunchDuration, _damagePunchVibrato, _damagePunchElasticity)
            .SetUpdate(UpdateType.Normal)
            .SetLink(Owner.gameObject);
    }

    private IEnumerator PlayDeathAnimationThenNotify()
    {
        if (Owner.Animator != null && !string.IsNullOrEmpty(_deathAnimationState))
            Owner.Animator.Play(_deathAnimationState);

        yield return new WaitForSeconds(_despawnDelayAfterDeath);

        OnDeath?.Invoke();
    }
}
