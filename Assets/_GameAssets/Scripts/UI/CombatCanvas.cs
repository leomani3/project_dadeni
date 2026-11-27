using System;
using System.Collections.Generic;
using Deckbuilder.ActionResources;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatCanvas : CanvasHandler
{
    public Action onEndTurnClicked;
    public Action onEndCombatClicked;
    public Action onLockPlacementClicked;

    [SerializeField] private CustomButton _endTurnButton;
    [SerializeField] private CustomButton _endCombatButton;
    [SerializeField, Required] private CustomButton _lockPlacementButton;
    [SerializeField, Required] private TurnBanner _turnBanner;
    [SerializeField, Required] private CombatDeckView _deckView;
    [SerializeField, Required] private RectTransform _resourceLabelContainer;
    [SerializeField, Required] private TMP_Text _resourceLabelPrefab;
    [SerializeField, Min(0f)] private float _resourceLabelSpacing = 48f;
    [SerializeField] private string _notEnoughResourceMessageFormat = "Not enough {0}";
    [SerializeField] private Color _notEnoughResourceColor = new Color(1f, 0.28f, 0.22f);
    [SerializeField, Min(0f)] private float _resourceShakeDuration = 0.4f;
    [SerializeField, Min(0f)] private float _resourceShakeStrength = 14f;
    [SerializeField, Min(1)] private int _resourceShakeVibrato = 30;

    private readonly Dictionary<ActionResourceConfig, TMP_Text> _resourceLabels = new Dictionary<ActionResourceConfig, TMP_Text>();
    private readonly Dictionary<TMP_Text, Sequence> _resourceShakeTweens = new Dictionary<TMP_Text, Sequence>();

    public CombatDeckView DeckView => _deckView;

    public override void Init()
    {
        base.Init();
        _endTurnButton.onClick += HandleEndTurnButtonClicked;
        _endCombatButton.onClick += HandleEndCombatButtonClicked;
        _lockPlacementButton.onClick += HandleLockPlacementButtonClicked;
        _deckView.onUnaffordableCardRejected += HandleUnaffordableCardRejected;
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        CustomButton _spaceButton = _lockPlacementButton.gameObject.activeInHierarchy ? _lockPlacementButton : _endTurnButton;

        if (_spaceButton.gameObject.activeInHierarchy)
            _spaceButton.Press();
    }

    public void ShowResources(IReadOnlyList<ActionResourceConfig> resources)
    {
        ClearResources();

        for (int i = 0; i < resources.Count; i++)
        {
            ActionResourceConfig resource = resources[i];
            TMP_Text label = Instantiate(_resourceLabelPrefab, _resourceLabelContainer);
            label.rectTransform.anchoredPosition = new Vector2(0f, (resources.Count - 1 - i) * _resourceLabelSpacing);
            label.name = resource.name + "Label";
            label.color = resource.Color;
            _resourceLabels.Add(resource, label);
            SetResource(resource, 0, 0);
        }
    }

    public void ClearResources()
    {
        foreach (Sequence tween in _resourceShakeTweens.Values)
            tween.Kill();

        foreach (TMP_Text label in _resourceLabels.Values)
            Destroy(label.gameObject);

        _resourceShakeTweens.Clear();
        _resourceLabels.Clear();
    }

    public void SetResource(ActionResourceConfig resource, int remaining, int max)
    {
        if (_resourceLabels.TryGetValue(resource, out TMP_Text label))
            label.text = $"{resource.ShortName} {remaining}/{max}";
    }

    public void ShowNotEnoughResource(ActionResourceConfig resource)
    {
        string message = string.Format(_notEnoughResourceMessageFormat, resource.ShortName);

        if (!_resourceLabels.TryGetValue(resource, out TMP_Text label))
        {
            FloatingTextManager.Instance.SpawnScreenText(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f), message, FloatingTextType.Warning);
            return;
        }

        RectTransform labelTransform = label.rectTransform;

        if (_resourceShakeTweens.TryGetValue(label, out Sequence previousTween))
            previousTween.Kill();

        Vector2 restPosition = labelTransform.anchoredPosition;
        Color restColor = resource.Color;
        label.color = _notEnoughResourceColor;

        _resourceShakeTweens[label] = DOTween.Sequence()
            .Join(labelTransform.DOShakeAnchorPos(_resourceShakeDuration, new Vector2(_resourceShakeStrength, 0f), _resourceShakeVibrato, 0f, false, true))
            .Join(label.DOColor(restColor, _resourceShakeDuration).SetEase(Ease.InQuad))
            .OnKill(() =>
            {
                labelTransform.anchoredPosition = restPosition;
                label.color = restColor;
            });

        Canvas labelCanvas = label.canvas;
        Camera labelCamera = labelCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : labelCanvas.worldCamera;
        Vector2 labelScreenPosition = RectTransformUtility.WorldToScreenPoint(labelCamera, labelTransform.position);
        FloatingTextManager.Instance.SpawnScreenText(labelScreenPosition, message, FloatingTextType.Warning);
    }

    public void SetEndTurnButtonInteractable(bool interactable)
    {
        _endTurnButton.SetInteractible(interactable);
    }

    public void SetPlacementModeActive(bool active)
    {
        _lockPlacementButton.SetActive(active);
        _endTurnButton.SetActive(!active);
    }

    public void ShowTurnBanner(CombatPhase phase)
    {
        _turnBanner.Show(phase);
    }

    private void HandleUnaffordableCardRejected(CardInstance card)
    {
        ShowNotEnoughResource(card.CostResource);
    }

    private void HandleEndTurnButtonClicked(int buttonIndex)
    {
        onEndTurnClicked?.Invoke();
    }

    private void HandleEndCombatButtonClicked(int buttonIndex)
    {
        onEndCombatClicked?.Invoke();
    }

    private void HandleLockPlacementButtonClicked(int buttonIndex)
    {
        onLockPlacementClicked?.Invoke();
    }
}
