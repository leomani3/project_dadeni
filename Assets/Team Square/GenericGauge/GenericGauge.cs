using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GenericGauge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private RectTransform _fillAreaRect;
    [SerializeField] private Image _flashOverlay;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Text Label")]
    [SerializeField] private bool _showText = false;
    [SerializeField] private TMP_Text _label;

    [Header("Visibility")]
    [SerializeField] private bool _hideWhenFull = false;
    [SerializeField] private float _hideDelay = 1f;
    [SerializeField] private float _hideFadeDuration = 0.3f;

    [Header("Timings")]
    [SerializeField] private float _chunkPopDuration = 0.15f;
    [SerializeField] private float _chunkExitDelay = 0.25f;
    [SerializeField] private float _chunkExitDuration = 0.4f;
    [SerializeField] private float _barShakeDuration = 0.15f;
    [SerializeField] private float _smoothFillDuration = 0.3f;
    [SerializeField] private float _chunkYScale = 2f;
    [SerializeField] private Color _chunkColor = Color.white;

    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.1f;
    [SerializeField] private Color _flashColor = Color.white;

    [Header("Shake Settings")]
    [SerializeField] private float _chunkShakeIntensity = 8f;
    [SerializeField] private float _barShakeIntensity = 6f;

    [Header("Pooling")]
    [SerializeField] private Image _damageChunkPrefab;
    [SerializeField] private int _chunkPoolSize = 4;

    private readonly List<Image> _chunkPool = new List<Image>();
    private readonly List<Image> _activeChunks = new List<Image>();

    private Tween _fillTween;
    private Tween _barShakeTween;
    private Tween _flashTween;
    private Tween _fadeTween;
    private Tween _hideDelayTween;
    private float _currentNormalized;

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        _currentNormalized = _fillImage.fillAmount;
        InitializeChunkPool();
        _label.gameObject.SetActive(_showText);
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void Setup(float currentValue, float maxValue)
    {
        SetValue(currentValue, maxValue, instant: true, showChunks: false);
    }

    public void SetValue(float currentValue, float maxValue)
    {
        SetValue(currentValue, maxValue, instant: false, showChunks: true);
    }

    public void SetValue(float currentValue, float maxValue, bool instant, bool showChunks)
    {
        maxValue = Mathf.Max(maxValue, 0.0001f);
        float targetNormalized = Mathf.Clamp01(currentValue / maxValue);

        if (targetNormalized >= 1f && _hideWhenFull)
            HideGauge(instant);

        if (Mathf.Approximately(targetNormalized, _currentNormalized))
            return;

        bool isDamage = targetNormalized < _currentNormalized;
        float previousNormalized = _currentNormalized;

        if (_hideWhenFull && isDamage)
            ShowGauge();

        UpdateFill(targetNormalized, instant);
        UpdateLabel(currentValue);

        if (showChunks)
        {
            SpawnChunk(previousNormalized, targetNormalized, isDamage);
            Flash();
            ShakeBar();
        }

        if (_hideWhenFull && Mathf.Approximately(targetNormalized, 1f))
            HideGauge();
    }

    public void HideGauge(bool instant = false, TweenCallback onComplete = null)
    {
        _hideDelayTween?.Kill();
        _fadeTween?.Kill();

        if (instant)
        {
            _canvasGroup.alpha = 0f;
            onComplete?.Invoke();
            return;
        }

        _hideDelayTween = DOVirtual.DelayedCall(_hideDelay, () =>
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(0f, _hideFadeDuration);

            if (onComplete != null)
                _fadeTween.onComplete += onComplete;
        });
    }

    public void StopFeedback()
    {
        KillTweens();
        _flashOverlay.gameObject.SetActive(false);

        foreach (Image chunk in _activeChunks)
        {
            DOTween.Kill(chunk.rectTransform);
            chunk.gameObject.SetActive(false);
        }

        _activeChunks.Clear();
    }

    private void InitializeChunkPool()
    {
        for (int i = 0; i < _chunkPoolSize; i++)
            _chunkPool.Add(CreateChunk());
    }

    private Image CreateChunk()
    {
        Image chunk = Instantiate(_damageChunkPrefab, _fillAreaRect);
        chunk.gameObject.SetActive(false);
        chunk.raycastTarget = false;
        return chunk;
    }

    private Image GetChunkFromPool()
    {
        foreach (Image chunk in _chunkPool)
            if (!chunk.gameObject.activeSelf)
                return chunk;

        if (_activeChunks.Count > 0)
        {
            Image reusedChunk = _activeChunks[0];
            reusedChunk.gameObject.SetActive(false);
            _activeChunks.RemoveAt(0);
            return reusedChunk;
        }

        Image extraChunk = CreateChunk();
        _chunkPool.Add(extraChunk);
        return extraChunk;
    }

    private void SpawnChunk(float from, float to, bool isDamage)
    {
        float left = Mathf.Min(from, to);
        float right = Mathf.Max(from, to);

        if (right - left <= 0.0001f)
            return;

        Image chunk = GetChunkFromPool();
        SetupChunk(chunk, left, right, isDamage);
        chunk.gameObject.SetActive(true);
        _activeChunks.Add(chunk);

        AnimateChunk(chunk);
    }

    private void SetupChunk(Image chunk, float left, float right, bool isDamage)
    {
        RectTransform chunkRect = chunk.rectTransform;

        chunkRect.anchorMin = new Vector2(left, 0f);
        chunkRect.anchorMax = new Vector2(right, 1f);
        chunkRect.offsetMin = Vector2.zero;
        chunkRect.offsetMax = Vector2.zero;
        chunkRect.anchoredPosition = Vector2.zero;
        chunkRect.localScale = Vector3.one;
        chunkRect.pivot = isDamage ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);

        chunk.color = _chunkColor;
    }

    private void AnimateChunk(Image chunk)
    {
        RectTransform chunkRect = chunk.rectTransform;
        DOTween.Kill(chunkRect);

        chunkRect.DOScaleY(_chunkYScale, _chunkPopDuration).SetEase(Ease.OutBack);
        chunkRect.DOShakeAnchorPos(_chunkPopDuration, _chunkShakeIntensity, 25, 90, false, true);
        DOVirtual.DelayedCall(_chunkExitDelay, () => PlayChunkExit(chunk));
    }

    private void PlayChunkExit(Image chunk)
    {
        Sequence exitSequence = DOTween.Sequence();
        exitSequence.Join(chunk.DOFade(0f, _chunkExitDuration));
        exitSequence.SetEase(Ease.InCubic);
        exitSequence.OnComplete(() => DeactivateChunk(chunk));
    }

    private void DeactivateChunk(Image chunk)
    {
        chunk.gameObject.SetActive(false);
        _activeChunks.Remove(chunk);
    }

    private void UpdateFill(float targetFill, bool instant)
    {
        _fillTween?.Kill();
        _currentNormalized = targetFill;

        if (instant)
        {
            _fillImage.fillAmount = targetFill;
            return;
        }

        _fillTween = _fillImage.DOFillAmount(targetFill, _smoothFillDuration).SetEase(Ease.OutCubic);
    }

    private void UpdateLabel(float currentValue)
    {
        if (!_showText) return;
        _label.text = currentValue.ToString("N0");
    }

    private void ShowGauge()
    {
        _hideDelayTween?.Kill();
        _fadeTween?.Kill();
        _fadeTween = _canvasGroup.DOFade(1f, _hideFadeDuration);
    }

    private void Flash()
    {
        _flashTween?.Kill();

        _flashOverlay.gameObject.SetActive(true);
        _flashOverlay.color = _flashColor;

        _flashTween = _flashOverlay.DOFade(0f, _flashDuration);
        _flashTween.onComplete += () => _flashOverlay.gameObject.SetActive(false);
    }

    private void ShakeBar()
    {
        _barShakeTween?.Complete();
        _barShakeTween = transform.DOShakeRotation(_barShakeDuration, Vector3.forward * _barShakeIntensity, 5);
    }

    private void KillTweens()
    {
        _fillTween?.Kill();
        _barShakeTween?.Kill(true);
        _flashTween?.Kill();
        _fadeTween?.Kill();
        _hideDelayTween?.Kill();
    }
}
