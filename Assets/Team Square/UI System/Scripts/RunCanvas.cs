using Sirenix.OdinInspector;
using UnityEngine;

public class RunCanvas : CanvasHandler
{
    [SerializeField, Required] private CustomButton _completeRoomButton;

    public override void Init()
    {
        base.Init();

        _completeRoomButton.onClick += HandleCompleteRoomButtonClicked;

        GameFlow.Instance.onStateChanged += HandleGameFlowStateChanged;
        ApplyGameFlowState(GameFlow.Instance.State);
    }

    private void OnDestroy()
    {
        if (GameFlow.Instance != null)
            GameFlow.Instance.onStateChanged -= HandleGameFlowStateChanged;
    }

    private void HandleGameFlowStateChanged(GameFlowState _state)
    {
        ApplyGameFlowState(_state);
    }

    private void ApplyGameFlowState(GameFlowState _state)
    {
        if (_state == GameFlowState.Run)
            Open();
        else
            Close();
    }

    private void HandleCompleteRoomButtonClicked(int _buttonIndex)
    {
        RunManager.Instance.CompleteCurrentRoom();
    }
}
