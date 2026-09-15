using System;
using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Actions;
using Deckbuilder.Grid;
using Lean.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Deckbuilder.Combat
{
    [RequireComponent(typeof(ArenaGrid))]
    public class CombatManager : MonoBehaviour
    {
        public Action onCombatStarted;
        public Action onCombatEnded;

        [SerializeField] private ArenaGrid m_grid;
        [SerializeField, Min(0f)] private float m_enemyTurnDuration = 0.5f;

        public ArenaGrid Grid => m_grid;
        public IReadOnlyList<Entity> Enemies => m_enemies;
        public bool IsCombatRunning { get; private set; }
        public bool IsPlayerTurn { get; private set; }

        private readonly List<Entity> m_enemies = new();
        private readonly List<Entity> m_enemiesTakingTurn = new();
        private Entity m_player;
        private CombatCanvas m_combatCanvas;
        private Coroutine m_enemyTurnsRoutine;

        private void Reset()
        {
            m_grid = GetComponent<ArenaGrid>();
        }

        private void Start()
        {
            SceneManager.SetActiveScene(gameObject.scene);
            StartCombat(RunManager.Instance.Player, RunManager.Instance.CurrentEnemyGroup);
        }

        private void StartCombat(Entity _player, IReadOnlyList<Entity> _enemyPrefabs)
        {
            IsCombatRunning = true;

            EntityManager.Instance.onEntityUnregistered += Forget;

            m_grid.BuildIfNeeded();

            SpawnEnemies(_enemyPrefabs);
            PlacePlayer(_player);

            m_combatCanvas = UIManager.Instance.GetCanvas<CombatCanvas>();
            m_combatCanvas.onEndTurnClicked += EndPlayerTurn;
            m_combatCanvas.onEndCombatClicked += EndCombat;
            m_combatCanvas.Open();

            onCombatStarted?.Invoke();

            StartPlayerTurn();
        }

        public void EndCombat()
        {
            if (!IsCombatRunning)
                return;

            IsCombatRunning = false;
            IsPlayerTurn = false;

            if (m_enemyTurnsRoutine != null)
            {
                StopCoroutine(m_enemyTurnsRoutine);
                m_enemyTurnsRoutine = null;
            }

            EntityManager.Instance.onEntityUnregistered -= Forget;

            m_combatCanvas.onEndTurnClicked -= EndPlayerTurn;
            m_combatCanvas.onEndCombatClicked -= EndCombat;
            m_combatCanvas.Close();
            m_combatCanvas = null;

            onCombatEnded?.Invoke();

            RunManager.Instance.EndCombat();
        }

        public void EndPlayerTurn()
        {
            if (!IsPlayerTurn)
                return;

            IsPlayerTurn = false;
            m_combatCanvas.SetEndTurnButtonInteractable(false);
            m_enemyTurnsRoutine = StartCoroutine(PlayEnemyTurns());
        }

        public Entity SpawnEntity(Entity _entityPrefab, GridCell _cell)
        {
            Entity _entity = LeanPool.Spawn(_entityPrefab, _cell.transform.position, _cell.transform.rotation);
            PlaceOnCell(_entity, _cell);

            return _entity;
        }

        private void StartPlayerTurn()
        {
            IsPlayerTurn = true;
            m_player.OnTurnStart();
            m_combatCanvas.SetEndTurnButtonInteractable(true);
        }

        private IEnumerator PlayEnemyTurns()
        {
            m_player.TryGetModule(out EntityActionQueueModule _playerActionQueue);

            while (_playerActionQueue.IsProcessing)
                yield return null;

            m_player.OnTurnEnd();

            m_enemiesTakingTurn.Clear();
            m_enemiesTakingTurn.AddRange(m_enemies);

            foreach (Entity _enemy in m_enemiesTakingTurn)
            {
                if (m_enemies.Contains(_enemy))
                    yield return PlayEnemyTurn(_enemy);
            }

            m_enemyTurnsRoutine = null;
            StartPlayerTurn();
        }

        private IEnumerator PlayEnemyTurn(Entity _enemy)
        {
            _enemy.OnTurnStart();

            if (m_enemies.Contains(_enemy))
                yield return new WaitForSeconds(m_enemyTurnDuration);

            _enemy.OnTurnEnd();
        }

        private void SpawnEnemies(IReadOnlyList<Entity> _enemyPrefabs)
        {
            List<GridCell> _spawnCells = m_grid.GetFreeSpawnCells(CellType.EnemySpawn, true);

            for (int _i = 0; _i < _enemyPrefabs.Count; _i++)
            {
                if (_i >= _spawnCells.Count)
                {
                    this.LogError($"The grid has {_spawnCells.Count} free {CellType.EnemySpawn} cells for {_enemyPrefabs.Count} enemies, the remaining ones are not spawned.");
                    return;
                }

                m_enemies.Add(SpawnEntity(_enemyPrefabs[_i], _spawnCells[_i]));
            }
        }

        private void PlacePlayer(Entity _player)
        {
            m_player = _player;
            GridCell _spawnCell = m_grid.GetFreeSpawnCell(CellType.AllySpawn);

            PlaceOnCell(m_player, _spawnCell);
        }

        private void PlaceOnCell(Entity _entity, GridCell _cell)
        {
            _entity.OnCombatEnter();
            _entity.transform.SetPositionAndRotation(_cell.transform.position, _cell.transform.rotation);

            if (!_cell.TrySetOccupant(_entity))
                this.LogWarning($"Cell {_cell.name} is already occupied by {_cell.Occupant.name}.");

            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.SetCombatManager(this);
        }

        private void ReleaseCell(Entity _entity)
        {
            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover) && _combatMover.CurrentCell != null && _combatMover.CurrentCell.Occupant == _entity)
                _combatMover.CurrentCell.ClearOccupant();
        }

        private void Forget(Entity _entity)
        {
            ReleaseCell(_entity);

            m_enemies.Remove(_entity);

            if (m_player == _entity)
                m_player = null;
        }
    }
}
