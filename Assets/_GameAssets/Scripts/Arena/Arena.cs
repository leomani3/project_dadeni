using System;
using System.Collections.Generic;
using Deckbuilder.Grid;
using Lean.Pool;
using UnityEngine;
using Utils;

namespace Deckbuilder.Combat
{
    [RequireComponent(typeof(ArenaGrid))]
    public class Arena : MonoBehaviour
    {
        public Action onCombatStarted;
        public Action onCombatEnded;

        [SerializeField] private ArenaGrid m_grid;
        
        public ArenaGrid Grid => m_grid;
        public IReadOnlyList<Entity> Enemies => m_enemies;
        public bool IsCombatRunning { get; private set; }
        
        private readonly List<Entity> m_combatEntities = new();
        private readonly List<Entity> m_enemies = new();
        private Entity _player;

        private void Reset()
        {
            m_grid = GetComponent<ArenaGrid>();
        }

        public void StartCombat(IReadOnlyList<Entity> _enemies)
        {
            gameObject.SetActive(true);
            if (IsCombatRunning)
                return;

            IsCombatRunning = true;

            EntityManager.Instance.onEntityUnregistered += Forget;

            m_grid.BuildIfNeeded();

            PlaceEnemies(_enemies);
            PlacePlayer();

            onCombatStarted?.Invoke();
        }

        public void EndCombat()
        {
            if (!IsCombatRunning)
                return;

            IsCombatRunning = false;

            EntityManager.Instance.onEntityUnregistered -= Forget;

            foreach (Entity _entity in m_combatEntities)
            {
                ReleaseCell(_entity);
                _entity.OnCombatExit();
            }

            m_combatEntities.Clear();
            m_enemies.Clear();
            _player = null;

            m_grid.ClearAllHighlights();

            onCombatEnded?.Invoke();
        }

        public Entity SpawnEntity(Entity _entityPrefab, GridCell _cell)
        {
            Entity _entity = LeanPool.Spawn(_entityPrefab, _cell.transform.position, _cell.transform.rotation);
            PlaceOnCell(_entity, _cell);

            return _entity;
        }

        private void PlaceEnemies(IReadOnlyList<Entity> _enemies)
        {
            List<GridCell> _spawnCells = m_grid.GetFreeSpawnCells(CellType.EnemySpawn, true);

            for (int _i = 0; _i < _enemies.Count; _i++)
            {
                if (_i >= _spawnCells.Count)
                {
                    this.LogError($"The grid has {_spawnCells.Count} free {CellType.EnemySpawn} cells for {_enemies.Count} enemies, the remaining ones are not placed.");
                    return;
                }

                PlaceOnCell(_enemies[_i], _spawnCells[_i]);
                m_enemies.Add(_enemies[_i]);
            }
        }

        private void PlacePlayer()
        {
            _player = EntityManager.Instance.Player;
            GridCell _spawnCell = m_grid.GetFreeSpawnCell(CellType.AllySpawn);
            
            PlaceOnCell(_player, _spawnCell);
        }

        private void PlaceOnCell(Entity _entity, GridCell _cell)
        {
            _entity.transform.SetPositionAndRotation(_cell.transform.position, _cell.transform.rotation);

            if (!_cell.TrySetOccupant(_entity))
                this.LogWarning($"Cell {_cell.name} is already occupied by {_cell.Occupant.name}.");

            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.SetArena(this);

            m_combatEntities.Add(_entity);
            _entity.OnCombatEnter();
        }

        private void ReleaseCell(Entity _entity)
        {
            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover) && _combatMover.CurrentCell != null && _combatMover.CurrentCell.Occupant == _entity)
                _combatMover.CurrentCell.ClearOccupant();
        }

        private void Forget(Entity _entity)
        {
            ReleaseCell(_entity);

            m_combatEntities.Remove(_entity);
            m_enemies.Remove(_entity);

            if (_player == _entity)
                _player = null;
        }
    }
}
