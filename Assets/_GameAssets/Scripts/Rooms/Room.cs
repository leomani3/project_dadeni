using System;
using UnityEngine;

public class Room : MonoBehaviour
{                                    
    [SerializeField] private Transform _spawnPoint;
    
    private RoomType _roomType;
    public Transform SpawnPoint => _spawnPoint;
}