using System;
using System.Threading.Tasks;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class GameFlow : Singleton<GameFlow>
{
    private const string HubSceneName = "MenuScene";

    public Action<GameFlowState> onStateChanged;
    public Action<CharacterConfig> onSelectedCharacterChanged;

    public GameFlowState State { get; private set; }
    public CharacterConfig SelectedCharacter { get; private set; }
    public Entity Player { get; private set; }
    public bool IsPlayerControlEnabled { get; private set; }

    private void Start()
    {
        RunManager.Instance.onRunEnded += HandleRunEnded;
    }

    private void OnDestroy()
    {
        if (RunManager.Instance != null)
            RunManager.Instance.onRunEnded -= HandleRunEnded;
    }

    public async void Boot()
    {
        if (SaveSystem.HasRunSave)
        {
            RunState _runState = SaveSystem.LoadRun();

            SelectCharacter(GameAssets.Instance.GetCharacter(_runState.characterId));
            SetState(GameFlowState.Run);

            this.Log($"Run save found, resuming room {_runState.currentRoomIndex} on seed {RunRandom.ToSeedString(_runState.seed)}.");

            RunManager.Instance.ResumeRun(_runState);
            return;
        }

        await GoToHub(!GameConfig.Instance.cheatSettings.noMenu);
    }

    public async Task GoToHub(bool _showMenu)
    {
        SetState(GameFlowState.Hub);

        await SceneManager.LoadSceneAsync(HubSceneName);

        SelectCharacter(ResolveSelectedCharacter());
        SpawnPlayer();

        HubManager.Instance.EnterHub(_showMenu);
    }

    public void SelectCharacter(CharacterConfig _character)
    {
        if (SelectedCharacter == _character)
            return;

        SelectedCharacter = _character;

        GameData.Instance.SetLastSelectedCharacter(_character.Id);

        onSelectedCharacterChanged?.Invoke(_character);
    }

    public void SelectCharacterInHub(CharacterConfig _character)
    {
        if (State != GameFlowState.Hub || SelectedCharacter == _character)
            return;

        SelectCharacter(_character);
        SpawnPlayer();

        HubManager.Instance.PlacePlayerAtSpawnPoint();
    }

    public void StartNewRun(string _seedInput = "")
    {
        if (State == GameFlowState.Run)
        {
            this.LogWarning("A run is already in progress, it has to be finished before starting another one.");
            return;
        }

        SetState(GameFlowState.Run);

        RunManager.Instance.StartNewRun(SelectedCharacter, _seedInput);
    }

    public void SetPlayerControl(bool _enabled)
    {
        IsPlayerControlEnabled = _enabled;
    }

    public void SpawnPlayer()
    {
        SpawnPlayer(Vector3.zero, Quaternion.identity);
    }

    public void SpawnPlayer(Vector3 _position, Quaternion _rotation)
    {
        DespawnPlayer();

        Player = Instantiate(SelectedCharacter.Prefab, _position, _rotation, transform);
        Player.name = SelectedCharacter.DisplayName;
    }

    public void DespawnPlayer()
    {
        if (Player == null)
            return;

        Destroy(Player.gameObject);
        Player = null;
    }

    private CharacterConfig ResolveSelectedCharacter()
    {
        GameData _gameData = GameData.Instance;

        if (!string.IsNullOrEmpty(_gameData.lastSelectedCharacterId) && _gameData.IsCharacterUnlocked(_gameData.lastSelectedCharacterId))
            return GameAssets.Instance.GetCharacter(_gameData.lastSelectedCharacterId);

        foreach (CharacterConfig _characterConfig in GameAssets.Instance.characterConfigs)
        {
            if (_gameData.IsCharacterUnlocked(_characterConfig.Id))
                return _characterConfig;
        }

        throw new InvalidOperationException("No unlocked CharacterConfig is listed in GameAssets, the hub has no character to show.");
    }

    private async void HandleRunEnded(RunOutcome _outcome)
    {
        await GoToHub(false);
    }

    private void SetState(GameFlowState _state)
    {
        if (State == _state)
            return;

        State = _state;
        IsPlayerControlEnabled = _state == GameFlowState.Run;

        this.Log($"Game flow state is now {_state}.");

        onStateChanged?.Invoke(_state);
    }
}
