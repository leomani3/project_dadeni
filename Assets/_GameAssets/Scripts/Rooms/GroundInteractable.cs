using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class GroundInteractable : MonoBehaviour, IInteractable
{
    public void OnPointerEnter(PointerEventData _eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Normal);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData.button != PointerEventData.InputButton.Left)
            return;

        Vector3 _destination = _eventData.pointerPressRaycast.worldPosition;

        EntityManager.Instance.Player.TryGetModule(out EntityRoomMoverModule _roomMover);
        _roomMover.MoveTo(_destination);

        ClickFeedbackManager.Instance.PlayMove(_destination);
    }
}
