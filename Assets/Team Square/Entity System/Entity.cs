using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using Utils;

public class Entity : MonoBehaviour, IPoolable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _collider;
    [SerializeField] private Transform _modelTransform;
    [SerializeField] private AnimationCurve _knockUpHeightCurve;
    [SerializeField] private EntityType _entityType;
    [SerializeField] private string _staggerAnimationState = "Staggered";
    [SerializeField] private string _staggerVariationAnimatorParameter = "StaggerVariation";

    private readonly Dictionary<Type, EntityModule> _modulesByType = new Dictionary<Type, EntityModule>();
    private bool _modulesInitialized;
    private bool _isStaggered;
    private float _staggerEndTime;
    private Coroutine _staggerCoroutine;
    private Coroutine _knockUpCoroutine;
    private bool _isSpawning;

    public EntityType EntityType => _entityType;
    public Animator Animator => _animator;
    public Collider Collider => _collider;
    public bool IsStaggered => _isStaggered;
    public bool IsSpawning => _isSpawning;

    internal void SetSpawning(bool value) => _isSpawning = value;

    public void CacheReferences()
    {
        _collider = GetComponent<Collider>();
        _animator = GetComponentInChildren<Animator>();
    }

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

    public void Stagger(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f) return;
        if (TryGetModule(out EntityHealthModule healthModule) && healthModule.IsDead) return;

        _isStaggered = true;

        if (_staggerCoroutine != null)
        {
            _staggerEndTime += duration;
        }
        else
        {
            _staggerEndTime = Time.time + duration;
            _staggerCoroutine = StartCoroutine(WaitForStaggerEnd());
        }

        PlayStaggerAnimation();
    }

    public void Stun(float duration)
    {
        this.LogWarning($"Stun is not implemented yet and currently falls back to Stagger for {duration} seconds.");
        Stagger(duration);
    }

    public void KnockUp(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f || _modelTransform == null) return;

        Stagger(duration);

        if (_knockUpCoroutine != null)
            StopCoroutine(_knockUpCoroutine);

        _knockUpCoroutine = StartCoroutine(RaiseModelWhileStaggered(duration));
    }

    private void Awake()
    {
        InitializeModules();
        RegisterInEntityManager();
    }

    private void OnEnable()
    {
        ResetStaggerState();
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

    private void PlayStaggerAnimation()
    {
        if (_animator == null) return;

        if (!string.IsNullOrEmpty(_staggerVariationAnimatorParameter))
            _animator.SetFloat(_staggerVariationAnimatorParameter, UnityEngine.Random.Range(0f, 1f));

        if (!string.IsNullOrEmpty(_staggerAnimationState))
            _animator.Play(_staggerAnimationState);
    }

    private void ResetStaggerState()
    {
        _isStaggered = false;
        _staggerEndTime = 0f;

        if (_staggerCoroutine != null)
        {
            StopCoroutine(_staggerCoroutine);
            _staggerCoroutine = null;
        }

        if (_knockUpCoroutine != null)
        {
            StopCoroutine(_knockUpCoroutine);
            _knockUpCoroutine = null;
        }

        if (_modelTransform != null)
            _modelTransform.localPosition = Vector3.zero;
    }

    private IEnumerator WaitForStaggerEnd()
    {
        while (Time.time < _staggerEndTime)
            yield return null;

        _isStaggered = false;
        _staggerCoroutine = null;
    }

    private IEnumerator RaiseModelWhileStaggered(float duration)
    {
        float knockUpStartTime = Time.time;

        while (_isStaggered)
        {
            float elapsed = Time.time - knockUpStartTime;
            float height = _knockUpHeightCurve.Evaluate(elapsed / duration);
            _modelTransform.localPosition = Vector3.up * height;
            yield return new WaitForEndOfFrame();
        }

        _modelTransform.localPosition = Vector3.zero;
        _knockUpCoroutine = null;
    }
}
