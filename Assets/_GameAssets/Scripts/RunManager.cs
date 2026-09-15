using System.Collections.Generic;
using System.Linq;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : Singleton<RunManager>
{
    [SerializeField] private Entity _playerPrefab;
    [SerializeField] private List<Room> _rooms;

    private Room _currentRoom;
    private readonly List<Entity> _currentEnemyGroup = new List<Entity>();
    private Pose _playerPoseBeforeCombat;

    public Entity Player { get; private set; }
    public IReadOnlyList<Entity> CurrentEnemyGroup => _currentEnemyGroup;

    public async void StartRun()
    {
        if (Player != null)
            Destroy(Player.gameObject);

        _currentEnemyGroup.Clear();

        await SceneManager.LoadSceneAsync("MainScene");

        _rooms = FindObjectsByType<Room>(FindObjectsSortMode.None).ToList();

        Player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity, transform);
        TeleportPlayerInRoom(_rooms[0]);
    }

    public void StartCombat(IReadOnlyList<Entity> _enemyPrefabs)
    {
        _currentEnemyGroup.Clear();
        _currentEnemyGroup.AddRange(_enemyPrefabs);

        _playerPoseBeforeCombat = new Pose(Player.transform.position, Player.transform.rotation);

        SceneManager.LoadSceneAsync("CombatScene");
    }

    public async void EndCombat()
    {
        _currentEnemyGroup.Clear();

        await SceneManager.LoadSceneAsync("MainScene");

        Player.transform.SetPositionAndRotation(_playerPoseBeforeCombat.position, _playerPoseBeforeCombat.rotation);
        Player.OnCombatExit();
    }
    
    public void TeleportPlayerInRoom(Room room)
    {
        if (Player != null)
        {
            Player.TryGetModule(out EntityRoomMoverModule _roomMover);
            _roomMover.Teleport(room.SpawnPoint.position);
            SetCurrentRoom(room);
        }
    }

    public void SetCurrentRoom(Room room)
    {
        _currentRoom = room;
    }
}
