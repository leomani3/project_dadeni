using System;
using System.Collections;
using Deckbuilder.StatusEffects;
using DG.Tweening;
using Sirenix.OdinInspector;
using Stats;
using UnityEngine;
using Utils;

public class EntityHealthModule : EntityModule
{
    public Action<float, float, float, bool, bool> OnHealthChanged;
    public Action<float, float> OnDamageTaken;
    public Action<float, float> OnHealed;
    public Action<int> OnBlockChanged;
    public Action OnDeathStart;
    public Action OnDeath;

    [SerializeField, Required] private ParticleSystem _deathVFXPrefab;
    [SerializeField, Min(0f)] private float _deathVFXLifetime = 3f;
    [SerializeField, Min(0f)] private float _deathVanishDelay = 0.2f;
    [SerializeField, Min(0f)] private float _deathVanishDuration = 0.35f;
    [SerializeField, Min(0f)] private float _deathVanishSinkDepth = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _deathVanishEndScale = 0.5f;
    [SerializeField] private Ease _deathVanishEase = Ease.InCubic;
    [SerializeField] private Vector3 _damagePunchScale = new Vector3(0.3f, -0.2f, 0f);
    [SerializeField, Min(0f)] private float _damagePunchDuration = 0.15f;
    [SerializeField, Min(1)] private int _damagePunchVibrato = 6;
    [SerializeField, Range(0f, 1f)] private float _damagePunchElasticity = 0.2f;
    [SerializeField] private SoundKeys _damageSound = SoundKeys._None;
    [SerializeField] private SoundKeys _deathSound = SoundKeys._None;
    [SerializeField] private float _maxHealthFallbackWithoutStatModule = 100f;

    private float _currentHealth;
    private int _block;
    protected bool _isDead;
    private Tween _damagePunchTween;
    protected EntityStatModule _statModule;
    protected StatusEffectModule _statusEffectModule;
    private EntityAnimationModule _animationModule;

    public bool IsDead => _isDead;
    public float CurrentHealth => _currentHealth;
    public int Block => _block;

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
        Owner.TryGetModule(out _statusEffectModule);

        if (!Owner.TryGetModule(out _animationModule))
            throw new MissingComponentException($"{Owner.name} has an EntityHealthModule but no EntityAnimationModule to play its death.");
        _currentHealth = MaxHealth;
    }

    public override void Cleanup()
    {
        OnHealthChanged = null;
        OnDamageTaken = null;
        OnHealed = null;
        OnBlockChanged = null;
        OnDeathStart = null;
        OnDeath = null;
        _block = 0;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (!GameConfig.Instance.gameSettings.retainBlockBetweenTurns)
            SetBlock(0);
    }

    public override void OnCombatExit()
    {
        base.OnCombatExit();
        SetBlock(0);
    }

    public void SetHealth(float _health)
    {
        _currentHealth = Mathf.Clamp(_health, 0f, MaxHealth);
        _isDead = _currentHealth <= 0f;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, 0f, false, true);
    }

    public void RefillToMaxHealth()
    {
        _currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, 0f, false, true);
    }

    public void TakeDamage(float amount, bool isCrit, bool ignoreVulnerable = false, bool ignoreBlock = false, bool suppressFeedback = false)
    {
        if (_isDead || amount <= 0f) return;

        if (!ignoreVulnerable && _statusEffectModule.TryGetStatusEffect(StatusEffectType.Vulnerable, out StatusEffect vulnerable))
            amount *= 1f + vulnerable.Config.Potency / 100f;

        int damage = Mathf.FloorToInt(amount);

        if (!ignoreBlock)
        {
            int blockedDamage = Mathf.Min(_block, damage);
            SetBlock(_block - blockedDamage);
            damage -= blockedDamage;
        }

        if (damage <= 0) return;

        float healthBeforeDamage = _currentHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        float healthDelta = _currentHealth - healthBeforeDamage;

        if (!suppressFeedback)
            PlayDamageFeedback();

        OnDamageTaken?.Invoke(damage, _currentHealth);
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth, healthDelta, isCrit, suppressFeedback);

        if (_currentHealth <= 0f)
            Die();
    }

    public void GainBlock(float amount)
    {
        if (_isDead || amount <= 0f) return;

        if (_statusEffectModule.TryGetStatusEffect(StatusEffectType.Dexterity, out StatusEffect dexterity))
            amount += dexterity.Stacks * dexterity.Config.Potency;

        if (_statusEffectModule.TryGetStatusEffect(StatusEffectType.Frail, out StatusEffect frail))
            amount *= 1f - frail.Config.Potency / 100f;

        int gainedBlock = Mathf.FloorToInt(amount);
        if (gainedBlock <= 0) return;

        SetBlock(_block + gainedBlock);
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

    private void SetBlock(int block)
    {
        if (block == _block) return;

        _block = block;
        OnBlockChanged?.Invoke(_block);
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
        yield return _animationModule.PlayDeath();
        yield return PlayDeathVanish();

        OnDeath?.Invoke();
    }

    private IEnumerator PlayDeathVanish()
    {
        Transform model = _animationModule.Animator.transform;
        Vector3 bodyPosition = _animationModule.Animator.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 bodyGroundPoint = new Vector3(bodyPosition.x, Owner.transform.position.y, bodyPosition.z);
        Vector3 modelStartPosition = model.position;
        Vector3 modelStartScale = model.localScale;

        Sequence vanish = DOTween.Sequence()
            .AppendInterval(_deathVanishDelay)
            .AppendCallback(() => SpawnDeathVFX(bodyGroundPoint))
            .Append(DOVirtual.Float(0f, 1f, _deathVanishDuration, progress =>
            {
                float scale = Mathf.LerpUnclamped(1f, _deathVanishEndScale, progress);
                model.localScale = modelStartScale * scale;
                model.position = bodyGroundPoint + (modelStartPosition - bodyGroundPoint) * scale + Vector3.down * (_deathVanishSinkDepth * progress);
            }).SetEase(_deathVanishEase))
            .SetLink(Owner.gameObject);

        yield return vanish.WaitForCompletion();
    }

    private void SpawnDeathVFX(Vector3 position)
    {
        Destroy(Instantiate(_deathVFXPrefab, position, _deathVFXPrefab.transform.rotation).gameObject, _deathVFXLifetime);
    }
}
