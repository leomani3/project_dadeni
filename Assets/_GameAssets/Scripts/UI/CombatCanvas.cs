using System;
using Deckbuilder.Combat;
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

    public CombatDeckView DeckView => _deckView;

    public override void Init()
    {
        base.Init();
        _endTurnButton.onClick += HandleEndTurnButtonClicked;
        _endCombatButton.onClick += HandleEndCombatButtonClicked;
        _lockPlacementButton.onClick += HandleLockPlacementButtonClicked;
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
