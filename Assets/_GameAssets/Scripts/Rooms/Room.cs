using System;
using UnityEngine;

public class Room : MonoBehaviour
{                                    
    [SerializeField] private GameObject _entryDoor;
    [SerializeField] private Transform _spawnPoint;

    public Transform SpawnPoint => _spawnPoint;
    
    private RoomType _roomType;
    
    private void Awake()
    {
        _entryDoor.SetActive(true);
    }

    private void Open()
    {
        _entryDoor.SetActive(false);
    }

    public virtual void OnEnter()
    {
        _entryDoor.SetActive(true);
    }

    public virtual void Exit()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        OnEnter();
    }
}