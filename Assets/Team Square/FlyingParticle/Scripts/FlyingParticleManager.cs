using System;
using Lean.Pool;
using MyBox;
using UnityEngine;

public class FlyingParticleManager : Singleton<FlyingParticleManager>
{
    [SerializeField] private UIFlyingParticle _flyingParticlePrefab;
    [SerializeField] private Transform _particleParent;

    public UIFlyingParticle Spawn(Vector3 startScreenPos, Transform target, Sprite sprite, float duration, Action callback, double burstCount = 1)
    {
        Transform parent = _particleParent != null ? _particleParent : transform;

        UIFlyingParticle spawnedParticle = LeanPool.Spawn(_flyingParticlePrefab, startScreenPos, Quaternion.identity, parent);
        spawnedParticle.Initialize(target, sprite, duration, callback, burstCount);

        return spawnedParticle;
    }

    public void DestroyParticle(UIFlyingParticle particle)
    {
        if (particle == null) return;

        LeanPool.Despawn(particle);
    }
}
