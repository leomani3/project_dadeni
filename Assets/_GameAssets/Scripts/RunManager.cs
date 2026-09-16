using System;
using System.Collections.Generic;
using System.IO;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class RunManager : Singleton<RunManager>
{
    private const string MainSceneName = "MainScene";
    private const string CombatSceneName = "CombatScene";
    private const string MapDebugFileName = "Map.txt";

    [SerializeField] private Entity _playerPrefab;
    [SerializeField] private SerializableDictionary<RoomType, Room> _roomPrefabs = new SerializableDictionary<RoomType, Room>();
    [SerializeField] private MapGenerationSettings _mapGenerationSettings = new MapGenerationSettings();

    private readonly List<Entity> _currentEnemyGroup = new List<Entity>();
    private readonly List<GameObject> _mainSceneRootsHiddenDuringCombat = new List<GameObject>();
    private Pose _playerPoseBeforeCombat;
    private Pose _virtualCameraPoseBeforeCombat;
    private bool _isChangingRoom;

    public Entity Player { get; private set; }
    public IReadOnlyList<Entity> CurrentEnemyGroup => _currentEnemyGroup;
    public MapData CurrentMap { get; private set; }
    public Room CurrentRoom { get; private set; }
    public int CurrentRoomIndex { get; private set; }

    public async void StartRun(string _seed = "")
    {
        if (Player != null)
            Destroy(Player.gameObject);

        _currentEnemyGroup.Clear();
        _mainSceneRootsHiddenDuringCombat.Clear();
        _isChangingRoom = false;
        CurrentRoom = null;

        RunRandom.Initialize(string.IsNullOrWhiteSpace(_seed) ? GameConfig.Instance.cheatSettings.debugSeed : _seed);

        await SceneManager.LoadSceneAsync(MainSceneName);

        Player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity, transform);

        GenerateMap();
        SpawnRoom(CurrentMap.StartNodeIndices[0]);
    }

    public void CompleteCurrentRoom()
    {
        CurrentRoom.Complete();
    }

    public void EnterNextRoom(DoorDirection _direction)
    {
        if (_isChangingRoom)
            return;

        if (!CurrentMap.TryGetNextRoomIndex(CurrentRoomIndex, _direction, out int _nextRoomIndex))
        {
            this.LogError($"No room behind the {_direction} door of room {CurrentRoomIndex}.");
            return;
        }

        _isChangingRoom = true;

        FadeManager.Instance.FadeIn(() =>
        {
            SpawnRoom(_nextRoomIndex);
            _isChangingRoom = false;
        });
    }

    public void StartCombat(IReadOnlyList<Entity> _enemyPrefabs)
    {
        _currentEnemyGroup.Clear();
        _currentEnemyGroup.AddRange(_enemyPrefabs);

        _playerPoseBeforeCombat = new Pose(Player.transform.position, Player.transform.rotation);

        Transform _virtualCameraTransform = CameraManager.Instance.VirtualCamera.transform;
        _virtualCameraPoseBeforeCombat = new Pose(_virtualCameraTransform.position, _virtualCameraTransform.rotation);

        HideMainScene();

        SceneManager.LoadSceneAsync(CombatSceneName, LoadSceneMode.Additive);
    }

    public async void EndCombat()
    {
        _currentEnemyGroup.Clear();

        await SceneManager.UnloadSceneAsync(CombatSceneName);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(MainSceneName));
        ShowMainScene();

        CameraManager.Instance.VirtualCamera.transform.SetPositionAndRotation(_virtualCameraPoseBeforeCombat.position, _virtualCameraPoseBeforeCombat.rotation);

        Player.transform.SetPositionAndRotation(_playerPoseBeforeCombat.position, _playerPoseBeforeCombat.rotation);
        Player.OnCombatExit();
    }

    private void GenerateMap()
    {
        CurrentMap = MapGenerator.Generate(_mapGenerationSettings, RunRandom.GetStream(RandomChannel.Map));

        ExportMapDebugFile();
    }

    private void ExportMapDebugFile()
    {
#if UNITY_EDITOR
        string _directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
#else
        string _directory = Path.Combine(Application.persistentDataPath, "Logs");
#endif
        Directory.CreateDirectory(_directory);

        string _filePath = Path.Combine(_directory, MapDebugFileName);
        string _header = $"Seed: {RunRandom.SeedString}{Environment.NewLine}Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{Environment.NewLine}";

        File.WriteAllText(_filePath, _header + CurrentMap.ToDebugString());

        this.Log($"Map generated with seed {RunRandom.SeedString} - {_filePath}");
    }

    private void SpawnRoom(int _roomIndex)
    {
        if (CurrentRoom != null)
            Destroy(CurrentRoom.gameObject);

        CurrentRoomIndex = _roomIndex;
        CurrentRoom = Instantiate(_roomPrefabs[CurrentMap.GetRoomType(_roomIndex)]);
        CurrentRoom.SetupDoors(CurrentMap.GetNextRoomDirections(_roomIndex));

        TeleportPlayerToSpawnPoint(CurrentRoom.SpawnPoint);
    }

    private void TeleportPlayerToSpawnPoint(Transform _spawnPoint)
    {
        Player.TryGetModule(out EntityRoomMoverModule _roomMover);
        _roomMover.Teleport(_spawnPoint.position);
        Player.transform.rotation = _spawnPoint.rotation;
    }

    private void HideMainScene()
    {
        _mainSceneRootsHiddenDuringCombat.Clear();

        foreach (GameObject _root in SceneManager.GetSceneByName(MainSceneName).GetRootGameObjects())
        {
            if (!_root.activeSelf)
                continue;

            _root.SetActive(false);
            _mainSceneRootsHiddenDuringCombat.Add(_root);
        }
    }

    private void ShowMainScene()
    {
        foreach (GameObject _root in _mainSceneRootsHiddenDuringCombat)
            _root.SetActive(true);

        _mainSceneRootsHiddenDuringCombat.Clear();
    }
}
