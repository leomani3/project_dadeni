using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Deckbuilder.Cards;
using MyBox;
using Stats;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class RunManager : Singleton<RunManager>
{
    private const string MainSceneName = "MainScene";
    private const string CombatSceneName = "CombatScene";
    private const string MapDebugFileName = "Map.txt";
    private const string RunMaxHealthModifierId = "RunMaxHealth";

    public Action onRunLoaded;
    public Action<RunOutcome> onRunEnded;

    [SerializeField] private SerializableDictionary<RoomType, Room> _roomPrefabs = new SerializableDictionary<RoomType, Room>();
    [SerializeField] private MapGenerationSettings _mapGenerationSettings = new MapGenerationSettings();

    private readonly List<GameObject> _mainSceneRootsHiddenDuringCombat = new List<GameObject>();
    private Pose _playerPoseBeforeCombat;
    private bool _isChangingRoom;

    public Entity Player => GameFlow.Instance.Player;
    public RunState CurrentRun { get; private set; }
    public MapData CurrentMap { get; private set; }
    public Room CurrentRoom { get; private set; }
    public int CurrentRoomIndex => CurrentRun.currentRoomIndex;
    public bool IsRunning => CurrentRun != null;

    public async void StartNewRun(CharacterConfig _character, string _seedInput = "")
    {
        RunRandom.Initialize(string.IsNullOrWhiteSpace(_seedInput) ? GameConfig.Instance.cheatSettings.debugSeed : _seedInput);

        GenerateMap();

        RunState _runState = RunState.Create(_character, RunRandom.Seed, CurrentMap.StartNodeIndices[0]);

        GameData.Instance.RegisterRunStarted(_character.Id);
        SaveSystem.SaveMeta();

        await LoadRun(_runState);
    }

    public async void ResumeRun(RunState _runState)
    {
        RunRandom.Initialize(_runState.seed);

        GenerateMap();

        await LoadRun(_runState);
    }

    public void EndRun(RunOutcome _outcome)
    {
        if (!IsRunning)
            return;

        string _characterId = CurrentRun.characterId;

        CurrentRun = null;
        CurrentMap = null;

        if (CurrentRoom != null)
        {
            Destroy(CurrentRoom.gameObject);
            CurrentRoom = null;
        }

        _isChangingRoom = false;

        SaveSystem.DeleteRun();

        GameData.Instance.RegisterRunFinished(_characterId, _outcome);
        SaveSystem.SaveMeta();

        this.Log($"Run ended with outcome {_outcome}.");

        onRunEnded?.Invoke(_outcome);
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
            CurrentRun.EnterRoom(_nextRoomIndex);
            CaptureRunState();
            SaveSystem.SaveRun(CurrentRun);

            SpawnRoom(_nextRoomIndex);
            TeleportPlayerToSpawnPoint(CurrentRoom.SpawnPoint);
            _isChangingRoom = false;
        });
    }

    public void StartCombat(IReadOnlyList<Entity> _enemyPrefabs)
    {
        CurrentRun.phase = RunPhase.InCombat;
        CaptureRunState();
        SaveSystem.SaveRun(CurrentRun);

        GameFlow.Instance.SetCombatEnemies(_enemyPrefabs);
        CurrentRoom.ClearEntityGroups();

        _playerPoseBeforeCombat = new Pose(Player.transform.position, Player.transform.rotation);

        HideMainScene();

        SceneManager.LoadSceneAsync(CombatSceneName, LoadSceneMode.Additive);
    }

    public async void ReturnFromCombat()
    {
        await SceneManager.UnloadSceneAsync(CombatSceneName);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(MainSceneName));
        ShowMainScene();

        Player.transform.SetPositionAndRotation(_playerPoseBeforeCombat.position, _playerPoseBeforeCombat.rotation);
        Player.OnCombatExit();

        CameraManager.Instance.FollowTarget(Player.transform);

        Player.TryGetModule(out EntityHealthModule _healthModule);

        if (_healthModule.IsDead)
        {
            EndRun(RunOutcome.Death);
            return;
        }

        CurrentRun.phase = RunPhase.InRoom;
        CaptureRunState();
        SaveSystem.SaveRun(CurrentRun);
    }

    public void CaptureRunState()
    {
        Player.TryGetModule(out EntityHealthModule _healthModule);
        Player.TryGetModule(out PlayerDeckModule _deckModule);

        CurrentRun.currentHealth = _healthModule.CurrentHealth;
        CurrentRun.CaptureDeck(_deckModule.Cards);
    }

    private async Task LoadRun(RunState _runState)
    {
        CurrentRun = _runState;
        _mainSceneRootsHiddenDuringCombat.Clear();
        _isChangingRoom = false;
        CurrentRoom = null;

        if (_runState.phase == RunPhase.InCombat)
        {
            _runState.phase = RunPhase.InRoom;
            this.Log("The run was saved during a fight, resuming at the start of that fight.");
        }

        await SceneManager.LoadSceneAsync(MainSceneName);

        SpawnRoom(_runState.currentRoomIndex);

        GameFlow.Instance.SpawnPlayer(CurrentRoom.SpawnPoint.position, CurrentRoom.SpawnPoint.rotation);
        ApplyRunStateToPlayer();

        CameraManager.Instance.FollowTarget(Player.transform);

        if (_runState.currentRoomCompleted)
            CurrentRoom.Complete();

        SaveSystem.SaveRun(CurrentRun);

        onRunLoaded?.Invoke();
    }

    private void ApplyRunStateToPlayer()
    {
        Player.TryGetModule(out EntityStatModule _statModule);
        Player.TryGetModule(out EntityHealthModule _healthModule);
        Player.TryGetModule(out PlayerDeckModule _deckModule);

        _statModule.AddModifier(new StatModifier(Player.EntityType, StatType.MaxHealth, CurrentRun.maxHealthBonus, ModifierType.Flat, RunMaxHealthModifierId));

        if (CurrentRun.HasSavedHealth)
            _healthModule.SetHealth(CurrentRun.currentHealth);
        else
            _healthModule.RefillToMaxHealth();

        _deckModule.Load(CurrentRun.BuildDeck());
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
        {
            CurrentRoom.onCompleted -= HandleCurrentRoomCompleted;
            Destroy(CurrentRoom.gameObject);
        }

        CurrentRoom = Instantiate(_roomPrefabs[CurrentMap.GetRoomType(_roomIndex)]);
        CurrentRoom.onCompleted += HandleCurrentRoomCompleted;
        CurrentRoom.SetupDoors(CurrentMap.GetNextRoomDirections(_roomIndex));
    }

    private void HandleCurrentRoomCompleted()
    {
        CurrentRun.CompleteRoom();
        CaptureRunState();
        SaveSystem.SaveRun(CurrentRun);

        if (CurrentMap.GetRoomType(CurrentRoomIndex) == RoomType.Boss)
            EndRun(RunOutcome.Victory);
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
