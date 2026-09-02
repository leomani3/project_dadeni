using Deckbuilder.Grid;
using Lean.Pool;
using MyBox;
using UnityEngine;
using Utils;

public class CombatManager : Singleton<CombatManager>
{
    [SerializeField] private Entity m_playerPrefab;
    [SerializeField] private Vector2Int m_playerSpawnCoordinate = new(0, 0);
    [SerializeField] private Entity m_enemyPrefab;
    [SerializeField] private Vector2Int m_enemySpawnCoordinate = new(3, 0);

    public Entity Player { get; private set; }
    public Entity Enemy { get; private set; }

    private void Awake()
    {
        if (GridManager.Instance == null)
        {
            this.LogError("No GridManager in the scene, combat cannot start.");
            return;
        }

        GridManager.Instance.Init();

        Player = SpawnEntity(m_playerPrefab, GridManager.Instance.GetCell(m_playerSpawnCoordinate));
        Enemy = SpawnEntity(m_enemyPrefab, GridManager.Instance.GetCell(m_enemySpawnCoordinate));
    }

    public Entity SpawnEntity(Entity _entityPrefab, GridCell _cell)
    {
        if (_entityPrefab == null)
        {
            this.LogError("No entity prefab provided to spawn.");
            return null;
        }

        if (_cell == null)
        {
            this.LogError("No cell provided to spawn entity on.");
            return null;
        }

        Entity _entity = LeanPool.Spawn(_entityPrefab, _cell.transform.position, _cell.transform.rotation);

        if (!_cell.TrySetOccupant(_entity))
            this.LogWarning($"Spawn cell {_cell.name} is already occupied.");

        return _entity;
    }
}
