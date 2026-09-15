using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class Room : MonoBehaviour
{
    public Action onCompleted;

    [SerializeField] private RoomType _roomType;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private List<Door> _doors = new List<Door>();

    public RoomType RoomType => _roomType;
    public Transform SpawnPoint => _spawnPoint;
    public bool IsCompleted { get; private set; }

    public void SetupDoors(IReadOnlyCollection<DoorDirection> _openDirections)
    {
        foreach (Door _door in _doors)
        {
            _door.SetEnabled(_openDirections.Contains(_door.Direction));
            _door.SetLocked(!IsCompleted);
        }
    }

    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        foreach (Door _door in _doors)
            _door.SetLocked(false);

        this.Log($"{_roomType} room completed, doors unlocked.");

        onCompleted?.Invoke();
    }
}
