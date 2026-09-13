using System.Collections.Generic;
using Deckbuilder.StatusEffects;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class EntityHUD : MonoBehaviour, IPoolable
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GenericGauge _healthBar;
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private TMP_Text _levelLabel;
    [SerializeField] private TMP_Text _blockLabel;
    [SerializeField] private RectTransform _statusEffectContainer;
    [SerializeField] private StatusEffectDisplay _statusEffectDisplayPrefab;
    [SerializeField] private float _fadeInDuration = 0.2f;

    private readonly Dictionary<StatusEffectType, StatusEffectDisplay> _statusEffectDisplays = new Dictionary<StatusEffectType, StatusEffectDisplay>();
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

    public void Setup(Transform target, string displayName, int level, float currentHealth, float maxHealth, int block)
    {
        _target = target;
        _nameLabel.text = displayName;
        SetLevel(level);
        SetBlock(block);
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

    public void SetBlock(int block)
    {
        _blockLabel.gameObject.SetActive(block > 0);
        _blockLabel.text = block.ToString();
    }

    public void SetStatusEffect(StatusEffect statusEffect)
    {
        StatusEffectType type = statusEffect.Config.Type;

        if (!_statusEffectDisplays.TryGetValue(type, out StatusEffectDisplay display))
        {
            display = LeanPool.Spawn(_statusEffectDisplayPrefab, _statusEffectContainer);
            _statusEffectDisplays.Add(type, display);
        }

        display.Refresh(statusEffect);
    }

    public void RemoveStatusEffect(StatusEffect statusEffect)
    {
        if (_statusEffectDisplays.Remove(statusEffect.Config.Type, out StatusEffectDisplay display))
            LeanPool.Despawn(display);
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

        foreach (StatusEffectDisplay display in _statusEffectDisplays.Values)
            if (display.gameObject.activeSelf)
                LeanPool.Despawn(display);

        _statusEffectDisplays.Clear();
        _target = null;
    }

    private void FollowTarget()
    {
        transform.position = CameraManager.Instance.MainCam.WorldToScreenPoint(_target.position);
    }
}
