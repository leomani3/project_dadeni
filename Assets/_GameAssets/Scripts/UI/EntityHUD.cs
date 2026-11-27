using System.Collections.Generic;
using Deckbuilder.StatusEffects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EntityHUD : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GenericGauge _healthBar;
    [SerializeField] private Graphic _healthBarFill;
    [SerializeField] private Image _blockIcon;
    [SerializeField] private TMP_Text _blockLabel;
    [SerializeField] private RectTransform _statusEffectContainer;
    [SerializeField] private StatusEffectDisplay _statusEffectDisplayPrefab;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _blockColorDuration = 0.2f;
    [SerializeField] private float _blockGainPunch = 0.35f;
    [SerializeField] private float _blockGainPunchDuration = 0.3f;

    private readonly Dictionary<StatusEffectType, StatusEffectDisplay> _statusEffectDisplays = new Dictionary<StatusEffectType, StatusEffectDisplay>();
    private Transform _target;
    private Tween _fadeTween;
    private Tween _blockColorTween;
    private Tween _blockPunchTween;
    private Color _healthBarFillColor;
    private int _block;

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        _healthBarFillColor = _healthBarFill.color;
        _blockIcon.color = GameConfig.Instance.uiSettings.shieldColor;
        _canvasGroup.alpha = 0f;
        _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration);
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        _blockColorTween?.Kill();
        _blockPunchTween?.Kill();
    }

    public void Setup(Transform target, float currentHealth, float maxHealth, int block)
    {
        _target = target;
        ApplyBlock(block, animate: false);
        _healthBar.Setup(currentHealth, maxHealth);
        FollowTarget();
    }

    public void SetHealth(float currentHealth, float maxHealth, bool showFeedback)
    {
        _healthBar.SetValue(currentHealth, maxHealth, instant: false, showChunks: showFeedback);
    }

    public void SetBlock(int block)
    {
        ApplyBlock(block, animate: true);
    }

    public void SetStatusEffect(StatusEffect statusEffect)
    {
        StatusEffectType type = statusEffect.Config.Type;

        if (!_statusEffectDisplays.TryGetValue(type, out StatusEffectDisplay display))
        {
            display = Instantiate(_statusEffectDisplayPrefab, _statusEffectContainer);
            _statusEffectDisplays.Add(type, display);
        }

        display.Refresh(statusEffect);
    }

    public void RemoveStatusEffect(StatusEffect statusEffect)
    {
        if (_statusEffectDisplays.Remove(statusEffect.Config.Type, out StatusEffectDisplay display))
            Destroy(display.gameObject);
    }

    public void SetHidden(bool _isHidden)
    {
        _fadeTween?.Kill();

        if (_isHidden)
            _canvasGroup.alpha = 0f;
        else
            _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration);
    }

    private void ApplyBlock(int block, bool animate)
    {
        bool hasBlock = block > 0;
        bool gainedBlock = block > _block;
        _block = block;

        _blockIcon.gameObject.SetActive(hasBlock);
        _blockLabel.text = block.ToString();

        Color fillColor = hasBlock ? GameConfig.Instance.uiSettings.shieldColor : _healthBarFillColor;
        _blockColorTween?.Kill();

        if (animate)
            _blockColorTween = _healthBarFill.DOColor(fillColor, _blockColorDuration);
        else
            _healthBarFill.color = fillColor;

        if (animate && gainedBlock)
            PunchBlockIcon();
    }

    private void PunchBlockIcon()
    {
        _blockPunchTween?.Kill();
        _blockIcon.rectTransform.localScale = Vector3.one;
        _blockPunchTween = _blockIcon.rectTransform.DOPunchScale(Vector3.one * _blockGainPunch, _blockGainPunchDuration, 6, 0.5f);
    }

    private void FollowTarget()
    {
        transform.position = CameraManager.Instance.MainCam.WorldToScreenPoint(_target.position);
    }
}
