using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class CombatCanvas : CanvasHandler
{
    public Action onEndTurnClicked;
    public Action onEndCombatClicked;

    [SerializeField] private CustomButton _endTurnButton;
    [SerializeField] private CustomButton _endCombatButton;
    [SerializeField, Required] private CombatDeckView _deckView;

    public CombatDeckView DeckView => _deckView;

    public override void Init()
    {
        base.Init();
        _endTurnButton.onClick += HandleEndTurnButtonClicked;
        _endCombatButton.onClick += HandleEndCombatButtonClicked;
    }

    public void SetEndTurnButtonInteractable(bool interactable)
    {
        _endTurnButton.SetInteractible(interactable);
    }

    private void HandleEndTurnButtonClicked(int buttonIndex)
    {
        onEndTurnClicked?.Invoke();
    }

    private void HandleEndCombatButtonClicked(int buttonIndex)
    {
        onEndCombatClicked?.Invoke();
    }
}
