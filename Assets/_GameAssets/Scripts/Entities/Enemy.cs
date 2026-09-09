using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Enemy : Entity, IInteractable
{
    [SerializeField] private float _fightStartDistance = 2f;

    public void OnPointerEnter(PointerEventData _eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Combat);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Normal);
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData.button != PointerEventData.InputButton.Left)
            return;

        Entity _player = EntityManager.Instance.Player;

        _player.TryGetModule(out EntityRoomMoverModule _roomMover);
        _roomMover.MoveTo(transform.position, _fightStartDistance, QueryFight);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Color _gizmoColor = new Color(1f, 0.35f, 0.2f);

        UnityEditor.Handles.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.1f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, _fightStartDistance);

        UnityEditor.Handles.color = _gizmoColor;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _fightStartDistance);
    }
#endif
}
