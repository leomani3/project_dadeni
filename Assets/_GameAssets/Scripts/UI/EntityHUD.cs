using System.Collections.Generic;
using Deckbuilder.StatusEffects;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EntityHUD : MonoBehaviour, IPoolable
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GenericGauge _healthBar;
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private RectTransform _statusEffectContainer;
    [SerializeField] private Image _statusEffectIconPrefab;
    [SerializeField] private float _fadeInDuration = 0.2f;

    private readonly Dictionary<StatusEffectConfig, Image> _statusEffectIcons = new Dictionary<StatusEffectConfig, Image>();
    private Transform _target;
    private Tween _fadeTween;

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
    }

    public void Setup(Transform target, string displayName, int level, float currentHealth, float maxHealth)
    {
        _target = target;
        _nameLabel.text = displayName;
        SetLevel(level);
        _healthBar.Setup(currentHealth, maxHealth);
        FollowTarget();
    }

    public void SetHealth(float currentHealth, float maxHealth, bool showFeedback)
    {
        _healthBar.SetValue(currentHealth, maxHealth, instant: false, showChunks: showFeedback);
    }

    public void SetLevel(int level)
    {
        _levelLabel.text = $"Lv {level}";
    }

    public void AddStatusEffect(StatusEffectConfig statusEffect)
    {
        if (_statusEffectIcons.ContainsKey(statusEffect)) return;

        Image icon = LeanPool.Spawn(_statusEffectIconPrefab, _statusEffectContainer);
        icon.sprite = statusEffect.Icon;
        _statusEffectIcons.Add(statusEffect, icon);
    }

    public void RemoveStatusEffect(StatusEffectConfig statusEffect)
    {
        if (_statusEffectIcons.Remove(statusEffect, out Image icon))
            LeanPool.Despawn(icon);
    }

    public void OnSpawn()
    {
        _fadeTween?.Kill();
        _canvasGroup.alpha = 0f;
        _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration);
    }

    public void OnDespawn()
    {
        _fadeTween?.Kill();
        _healthBar.StopFeedback();

        foreach (Image icon in _statusEffectIcons.Values)
            if (icon.gameObject.activeSelf)
                LeanPool.Despawn(icon);

        _statusEffectIcons.Clear();
        _target = null;
    }

    private void FollowTarget()
    {
        transform.position = CameraManager.Instance.MainCam.WorldToScreenPoint(_target.position);
    }
}
