using System;
using System.Collections.Generic;
using Deckbuilder.Grid;
using Deckbuilder.Grid.Highlighting;
using Lean.Pool;
using UnityEngine;
using Utils;

namespace Deckbuilder.Combat
{
    [RequireComponent(typeof(ArenaGrid))]
    [RequireComponent(typeof(ArenaHighlighter))]
    public class Arena : MonoBehaviour
    {
        public Action onCombatStarted;
        public Action onCombatEnded;

        [SerializeField] private ArenaGrid m_grid;
        [SerializeField] private ArenaHighlighter m_highlighter;
        [SerializeField] private Entity m_playerPrefab;
        [SerializeField] private List<Entity> m_enemyPrefabs = new();


        private readonly List<Entity> m_spawnedEntities = new();
        private readonly List<Entity> m_enemies = new();

        public ArenaGrid Grid => m_grid;
        public ArenaHighlighter Highlighter => m_highlighter;
        public Entity Player { get; private set; }
        public IReadOnlyList<Entity> Enemies => m_enemies;
        public bool IsCombatRunning { get; private set; }

        private void Reset()
        {
            m_grid = GetComponent<ArenaGrid>();
            m_highlighter = GetComponent<ArenaHighlighter>();
        }

        public void StartCombat()
        {
            StartCombat(m_playerPrefab, m_enemyPrefabs);
        }

        public void StartCombat(Entity _playerPrefab, IReadOnlyList<Entity> _enemyPrefabs)
        {
            if (IsCombatRunning)
                return;
            
            IsCombatRunning = true;

            if (EntityManager.Instance != null)
                EntityManager.Instance.onEntityUnregistered += Forget;

            m_grid.BuildIfNeeded();

            Player = SpawnPlayer(_playerPrefab);
            SpawnEnemies(_enemyPrefabs);

            onCombatStarted?.Invoke();
        }

        public void EndCombat()
        {
            if (!IsCombatRunning)
                return;

            IsCombatRunning = false;

            if (EntityManager.Instance != null)
                EntityManager.Instance.onEntityUnregistered -= Forget;

            for (int _i = m_spawnedEntities.Count - 1; _i >= 0; _i--)
                Despawn(m_spawnedEntities[_i]);

            m_spawnedEntities.Clear();
            m_enemies.Clear();
            Player = null;

            m_highlighter.ClearAllLayers();

            onCombatEnded?.Invoke();
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

            if (_entity.TryGetModule(out EntityGridModule _gridModule))
                _gridModule.SetArena(this);

            m_spawnedEntities.Add(_entity);
            return _entity;
        }

        public void Despawn(Entity _entity)
        {
            if (_entity == null)
                return;

            Forget(_entity);
            LeanPool.Despawn(_entity);
        }

        public Entity SpawnPlayer(Entity _playerPrefab, bool _onRandomCell = false)
        {
            return SpawnOnSpawnCell(_playerPrefab, CellType.AllySpawn, _onRandomCell);
        }

        public Entity SpawnEnemy(Entity _enemyPrefab, bool _onRandomCell = true)
        {
            Entity _enemy = SpawnOnSpawnCell(_enemyPrefab, CellType.EnemySpawn, _onRandomCell);

            if (_enemy != null)
                m_enemies.Add(_enemy);

            return _enemy;
        }

        public void SpawnEnemies(IReadOnlyList<Entity> _enemyPrefabs, bool _onRandomCells = false)
        {
            if (_enemyPrefabs == null)
                return;

            m_grid.BuildIfNeeded();

            foreach (Entity _enemyPrefab in _enemyPrefabs)
                SpawnEnemy(_enemyPrefab, _onRandomCells);
        }

        private Entity SpawnOnSpawnCell(Entity _entityPrefab, CellType _spawnCellType, bool _onRandomCell)
        {
            GridCell _spawnCell = m_grid.GetFreeSpawnCell(_spawnCellType, _onRandomCell);

            if (_spawnCell == null)
            {
                this.LogError($"The grid has no free {_spawnCellType} cell, {_entityPrefab.name} cannot be spawned.");
                return null;
            }

            return SpawnEntity(_entityPrefab, _spawnCell);
        }

        private void Forget(Entity _entity)
        {
            m_spawnedEntities.Remove(_entity);
            m_enemies.Remove(_entity);

            if (Player == _entity)
                Player = null;
        }
    }
}
