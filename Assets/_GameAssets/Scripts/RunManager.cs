using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : Singleton<RunManager>
{
    private const string MAIN_SCENE = "MainScene";
    private const string COMBAT_SCENE = "CombatScene";

    [SerializeField] private Entity _playerPrefab;

    private readonly List<Entity> _currentEnemyGroup = new List<Entity>();
    private Pose _playerPoseBeforeCombat;

    public Entity Player { get; private set; }
    public IReadOnlyList<Entity> CurrentEnemyGroup => _currentEnemyGroup;

    public async void StartRun()
    {
        if (Player != null)
            Destroy(Player.gameObject);

        _currentEnemyGroup.Clear();

        await SceneManager.LoadSceneAsync(MAIN_SCENE);

        Player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity, transform);
    }

    public void StartCombat(IReadOnlyList<Entity> _enemyPrefabs)
    {
        _currentEnemyGroup.Clear();
        _currentEnemyGroup.AddRange(_enemyPrefabs);

        _playerPoseBeforeCombat = new Pose(Player.transform.position, Player.transform.rotation);

        SceneManager.LoadSceneAsync(COMBAT_SCENE);
    }

    public async void EndCombat()
    {
        _currentEnemyGroup.Clear();

        await SceneManager.LoadSceneAsync(MAIN_SCENE);

        Player.transform.SetPositionAndRotation(_playerPoseBeforeCombat.position, _playerPoseBeforeCombat.rotation);
        Player.OnCombatExit();
    }
}
