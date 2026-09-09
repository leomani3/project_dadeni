using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Enemy : Entity, IInteractable
{
    [SerializeField] private float _fightStartDistance = 1.5f;

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
        _roomMover.MoveTo(GetFightStartPosition(_player.transform.position), QueryFight);
    }

    private Vector3 GetFightStartPosition(Vector3 _playerPosition)
    {
        Vector3 _fromEnemyToPlayer = _playerPosition - transform.position;
        _fromEnemyToPlayer.y = 0f;

        return transform.position + _fromEnemyToPlayer.normalized * _fightStartDistance;
    }
}
