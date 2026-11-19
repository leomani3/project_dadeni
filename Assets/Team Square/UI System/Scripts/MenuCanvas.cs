using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

public class MenuCanvas : CanvasHandler
{
    [SerializeField, Required] private CustomButton _continueButton;
    [SerializeField, Required] private CustomButton _newGameButton;

    public override void Init()
    {
        base.Init();

        _continueButton.onClick += HandleContinueClicked;
        _newGameButton.onClick += HandleNewGameClicked;

        GameFlow.Instance.onStateChanged += HandleGameFlowStateChanged;
    }

    private void OnDestroy()
    {
        if (GameFlow.Instance != null)
            GameFlow.Instance.onStateChanged -= HandleGameFlowStateChanged;
    }

    public override void Open()
    {
        base.Open();

        _continueButton.gameObject.SetActive(!GameData.Instance.firstGameLaunch);
    }

    private void HandleGameFlowStateChanged(GameFlowState _state)
    {
        if (_state != GameFlowState.Hub)
            Close();
    }

    private void HandleContinueClicked(int _buttonIndex)
    {
        HubManager.Instance.EnterFreeRoam();
    }

    private void HandleNewGameClicked(int _buttonIndex)
    {
        this.LogWarning("New game requested, the meta save is being wiped.");

        SaveSystem.DeleteEverything();

        HubManager.Instance.EnterFreeRoam();
    }
}
