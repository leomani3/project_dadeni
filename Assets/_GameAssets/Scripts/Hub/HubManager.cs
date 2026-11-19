using System;
using System.Collections.Generic;
using System.Linq;
using MyBox;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

public class HubManager : Singleton<HubManager>
{
    public Action<HubPoiType> onPoiReached;

    [SerializeField, Required] private Transform _playerSpawnPoint;
    [SerializeField, Required] private Transform _menuFocusPoint;
    [SerializeField] private List<HubPoi> _pois = new List<HubPoi>();

    public bool IsMenuOpen { get; private set; }
    public bool IsFreeRoam { get; private set; }

    private void Reset()
    {
        _pois = GetComponentsInChildren<HubPoi>(true).ToList();
    }

    private void OnDisable()
    {
        foreach (HubPoi _poi in _pois)
            _poi.onReached -= HandlePoiReached;
    }

    public void EnterHub(bool _showMenu)
    {
        foreach (HubPoi _poi in _pois)
        {
            _poi.onReached -= HandlePoiReached;
            _poi.onReached += HandlePoiReached;
        }

        PlacePlayerAtSpawnPoint();

        if (_showMenu)
            OpenMenu();
        else
            EnterFreeRoam();
    }

    public void PlacePlayerAtSpawnPoint()
    {
        GameFlow.Instance.Player.TryGetModule(out EntityRoomMoverModule _roomMover);
        _roomMover.Teleport(_playerSpawnPoint.position);
        GameFlow.Instance.Player.transform.rotation = _playerSpawnPoint.rotation;
    }

    public void OpenMenu()
    {
        IsMenuOpen = true;
        IsFreeRoam = false;

        GameFlow.Instance.SetPlayerControl(false);

        CameraManager.Instance.FocusOn(_menuFocusPoint);

        UIManager.Instance.GetCanvas<MenuCanvas>().Open();
    }

    public void EnterFreeRoam()
    {
        IsMenuOpen = false;
        IsFreeRoam = true;

        UIManager.Instance.GetCanvas<MenuCanvas>().Close();

        CameraManager.Instance.FollowTarget(GameFlow.Instance.Player.transform);

        GameFlow.Instance.SetPlayerControl(true);
    }

    private void HandlePoiReached(HubPoi _poi)
    {
        this.Log($"Reached the {_poi.Type} point of interest.");

        onPoiReached?.Invoke(_poi.Type);

        if (_poi.Type == HubPoiType.RunStart)
            GameFlow.Instance.StartNewRun();
    }
}
