using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class HubPoi : MonoBehaviour, IInteractable
{
    public Action<HubPoi> onReached;

    [SerializeField] private HubPoiType _type;
    [SerializeField, Required] private Transform _interactionPoint;
    [SerializeField, Min(0f)] private float _stopDistance = 0.5f;

    public HubPoiType Type => _type;
    public Transform InteractionPoint => _interactionPoint;

    private void Reset()
    {
        _interactionPoint = transform;
    }

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (!GameFlow.Instance.IsPlayerControlEnabled)
            return;

        CursorManager.Instance.ApplyCursor(CursorType.Hand);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Normal);
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!GameFlow.Instance.IsPlayerControlEnabled)
            return;

        GameFlow.Instance.Player.TryGetModule(out EntityRoomMoverModule _roomMover);

        if (!_roomMover.MoveTo(_interactionPoint.position, _stopDistance, HandleInteractionPointReached))
            return;

        ClickFeedbackManager.Instance.PlayMove(_roomMover.Destination);
    }

    private void HandleInteractionPointReached()
    {
        onReached?.Invoke(this);
    }
}
