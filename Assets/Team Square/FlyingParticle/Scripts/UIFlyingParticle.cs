using System;
using System.Threading.Tasks;
using AssetKits.ParticleImage;
using Lean.Pool;
using UnityEngine;

public class UIFlyingParticle : MonoBehaviour
{
    [SerializeField] private ParticleImage particleImage;
    [SerializeField] private float logBase = 10f;
    [SerializeField] private float logMultiplier = 5f;
    [SerializeField] private float burstInterval = .1f;

    private Action _onFirstParticleArrivedCallback;

    public async void Initialize(Transform target, Sprite sprite, float duration, Action callback = null, double burstCount = 1)
    {
        particleImage.Stop();
        particleImage.particles.Clear();

        particleImage.attractorTarget = target;
        particleImage.lifetime = duration;

        int scaledBurstCount = ApplyLogScaling(burstCount);
        particleImage.SetBurst(0, 0, 1);

        _onFirstParticleArrivedCallback = callback;

        if (particleImage != null && sprite != null)
            particleImage.sprite = sprite;

        for (int i = 0; i < scaledBurstCount; i++)
        {
            particleImage.Play();
            await Task.Delay((int)(burstInterval * 1000));
        }
    }

    public void OnFirstParticleArrived()
    {
        _onFirstParticleArrivedCallback?.Invoke();
    }

    public void OnParticleArrived()
    {
        SoundManager.Instance.PlaySound(SoundKeys.ui_currency_gain);
    }

    public void OnLastParticleArrived()
    {
        particleImage.Stop();
        LeanPool.Despawn(this);
    }

    private int ApplyLogScaling(double originalCount)
    {
        if (originalCount <= 0)
            return 0;

        float scaledValue = Mathf.Log((float)originalCount + 7.5f, logBase) * logMultiplier - 23.4f;
        return Mathf.Max(1, Mathf.RoundToInt(scaledValue));
    }
}
