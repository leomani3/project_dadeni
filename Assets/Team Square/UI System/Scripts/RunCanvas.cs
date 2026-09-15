using UnityEngine;

public class RunCanvas : CanvasHandler
{
    [SerializeField] private CustomButton _completeRoomButton;

    public override void Init()
    {
        base.Init();
        _completeRoomButton.onClick += HandleCompleteRoomButtonClicked;
    }

    private void HandleCompleteRoomButtonClicked(int _buttonIndex)
    {
        RunManager.Instance.CompleteCurrentRoom();
    }
}
