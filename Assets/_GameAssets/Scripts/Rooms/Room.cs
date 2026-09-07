using UnityEngine;

public abstract class Room : MonoBehaviour
{
    private RoomType _roomType;

    public abstract void Enter();
    public abstract void Exit();
}