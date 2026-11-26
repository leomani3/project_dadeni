using System;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

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
    [SerializeField, Required] private TMP_Text _actionPointsLabel;
    [SerializeField, Required] private TMP_Text _movementPointsLabel;
    [SerializeField] private string _notEnoughActionPointsMessage = "Not enough AP";
    [SerializeField] private Color _notEnoughActionPointsColor = new Color(1f, 0.28f, 0.22f);
    [SerializeField, Min(0f)] private float _actionPointsShakeDuration = 0.4f;
    [SerializeField, Min(0f)] private float _actionPointsShakeStrength = 14f;
    [SerializeField, Min(1)] private int _actionPointsShakeVibrato = 30;

    private Vector2 _actionPointsLabelRestPosition;
    private Color _actionPointsLabelRestColor;
    private Sequence _actionPointsShakeTween;

    public CombatDeckView DeckView => _deckView;

    public override void Init()
    {
        base.Init();
        _endTurnButton.onClick += HandleEndTurnButtonClicked;
        _endCombatButton.onClick += HandleEndCombatButtonClicked;
        _lockPlacementButton.onClick += HandleLockPlacementButtonClicked;
        _deckView.onUnaffordableCardRejected += HandleUnaffordableCardRejected;
    }

    public void ShowNotEnoughActionPoints()
    {
        RectTransform _labelTransform = _actionPointsLabel.rectTransform;

        _actionPointsShakeTween?.Kill();
        _actionPointsLabelRestPosition = _labelTransform.anchoredPosition;
        _actionPointsLabelRestColor = _actionPointsLabel.color;
        _actionPointsLabel.color = _notEnoughActionPointsColor;

        _actionPointsShakeTween = DOTween.Sequence()
            .Join(_labelTransform.DOShakeAnchorPos(_actionPointsShakeDuration, new Vector2(_actionPointsShakeStrength, 0f), _actionPointsShakeVibrato, 0f, false, true))
            .Join(_actionPointsLabel.DOColor(_actionPointsLabelRestColor, _actionPointsShakeDuration).SetEase(Ease.InQuad))
            .OnKill(() =>
            {
                _labelTransform.anchoredPosition = _actionPointsLabelRestPosition;
                _actionPointsLabel.color = _actionPointsLabelRestColor;
            });

        Canvas _labelCanvas = _actionPointsLabel.canvas;
        Camera _labelCamera = _labelCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _labelCanvas.worldCamera;
        Vector2 _labelScreenPosition = RectTransformUtility.WorldToScreenPoint(_labelCamera, _labelTransform.position);
        FloatingTextManager.Instance.SpawnScreenText(_labelScreenPosition, _notEnoughActionPointsMessage, FloatingTextType.Warning);
    }

    public void SetEndTurnButtonInteractable(bool interactable)
    {
        _endTurnButton.SetInteractible(interactable);
    }

    public void SetActionPoints(int remaining, int max)
    {
        _actionPointsLabel.text = $"AP {remaining}/{max}";
    }

    public void SetMovementPoints(int remaining, int max)
    {
        _movementPointsLabel.text = $"MP {remaining}/{max}";
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
        ShowNotEnoughActionPoints();
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
