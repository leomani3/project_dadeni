using System.Collections;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EntitySpawnModule : EntityModule
{
    [SerializeField] private ParticleSystem _spawnVFXPrefab;
    [SerializeField] private string _spawnAnimationTrigger = "Spawn";
    [SerializeField] private float _preSpawnDelay = 0.5f;
    [SerializeField] private float _spawnDuration = 1f;
    [SerializeField] private float _vfxShrinkDuration = 0.3f;

    private ParticleSystem _spawnVFXInstance;
    private Coroutine _spawnCoroutine;

    public ParticleSystem SpawnVFXPrefab => _spawnVFXPrefab;
    public float PreSpawnDelay => _preSpawnDelay;

    public override void OnAllModuleInitialized()
    {
        if (Owner.TryGetModule(out EntityHealthModule healthModule))
            healthModule.OnDeathStart += EndSpawnEarly;

        Owner.SetSpawning(true);

        if (Owner.Animator != null && !string.IsNullOrEmpty(_spawnAnimationTrigger))
            Owner.Animator.SetTrigger(_spawnAnimationTrigger);

        _spawnCoroutine = Owner.StartCoroutine(WaitForSpawnDuration());
    }

    public override void Cleanup()
    {
        StopSpawnCoroutine();
        UnsubscribeFromHealthModule();
        Owner.SetSpawning(false);

        if (_spawnVFXInstance != null)
        {
            _spawnVFXInstance.transform.DOKill();
            LeanPool.Despawn(_spawnVFXInstance);
            _spawnVFXInstance = null;
        }
    }

    public void SetSpawnVFXInstance(ParticleSystem instance)
    {
        _spawnVFXInstance = instance;
    }

    public void HandleSpawnEnd()
    {
        StopSpawnCoroutine();
        UnsubscribeFromHealthModule();
        Owner.SetSpawning(false);
        ShrinkAndDespawnVFX();
    }

    private IEnumerator WaitForSpawnDuration()
    {
        yield return new WaitForSeconds(_spawnDuration);
        _spawnCoroutine = null;
        HandleSpawnEnd();
    }

    private void EndSpawnEarly()
    {
        StopSpawnCoroutine();
        Owner.SetSpawning(false);
        ShrinkAndDespawnVFX();
    }

    private void StopSpawnCoroutine()
    {
        if (_spawnCoroutine == null) return;

        Owner.StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = null;
    }

    private void UnsubscribeFromHealthModule()
    {
        if (Owner.TryGetModule(out EntityHealthModule healthModule))
            healthModule.OnDeathStart -= EndSpawnEarly;
    }

    private void ShrinkAndDespawnVFX()
    {
        if (_spawnVFXInstance == null) return;

        _spawnVFXInstance.transform
            .DOScale(Vector3.zero, _vfxShrinkDuration)
            .OnComplete(() =>
            {
                LeanPool.Despawn(_spawnVFXInstance);
                _spawnVFXInstance = null;
            });
    }
}
