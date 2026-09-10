using DG.Tweening;
using Lean.Pool;
using MyBox;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour, IPoolable
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private RectTransform _motionRoot;

    private Sequence _sequence;
    private Vector3 _worldAnchor;
    private bool _followsWorldAnchor;

    private void Reset()
    {
        _text = GetComponentInChildren<TMP_Text>();
    }

    private void LateUpdate()
    {
        if (_followsWorldAnchor)
            MoveToScreenPosition(CameraManager.Instance.MainCam.WorldToScreenPoint(_worldAnchor));
    }

    public void PlayAtWorldPosition(Vector3 worldPosition, string message, FloatingTextConfig config)
    {
        _worldAnchor = worldPosition;
        _followsWorldAnchor = true;
        MoveToScreenPosition(CameraManager.Instance.MainCam.WorldToScreenPoint(worldPosition));
        Play(message, config);
    }

    public void PlayAtScreenPosition(Vector3 screenPosition, string message, FloatingTextConfig config)
    {
        _followsWorldAnchor = false;
        MoveToScreenPosition(screenPosition);
        Play(message, config);
    }

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
        _sequence?.Kill();
        _followsWorldAnchor = false;
    }

    private void MoveToScreenPosition(Vector3 screenPosition)
    {
        transform.position = new Vector3(screenPosition.x, screenPosition.y, 0f);
    }

    private void Play(string message, FloatingTextConfig config)
    {
        _sequence?.Kill();

        _text.text = message;
        _text.font = config.font;
        _text.fontSize = config.fontSize;
        _text.color = config.color.WithAlphaSetTo(config.enableFadeIn ? 0f : config.color.a);

        _motionRoot.anchoredPosition = Vector2.zero;
        _motionRoot.localScale = config.enableScaleIn ? Vector3.zero : Vector3.one;

        float riseDuration = config.spawnDuration + config.stayDuration;
        float xOffset = config.randomXMovement ? Random.Range(config.minMaxXOffset.x, config.minMaxXOffset.y) : 0f;

        _sequence = DOTween.Sequence();
        _sequence.Insert(0f, _motionRoot.DOAnchorPos(new Vector2(xOffset, config.YOffset), riseDuration));
        _sequence.Insert(0f, _text.DOFade(config.color.a, config.spawnDuration));

        if (config.enableScaleIn)
            _sequence.Insert(0f, _motionRoot.DOScale(1f, config.spawnDuration).SetEase(config.scaleInEase));

        if (config.enableFadeOut)
            _sequence.Insert(riseDuration, _text.DOFade(0f, config.despawnDuration));

        if (config.enableScaleOut)
            _sequence.Insert(riseDuration, _motionRoot.DOScale(0f, config.despawnDuration));

        _sequence.OnComplete(() => LeanPool.Despawn(this));
    }
}
