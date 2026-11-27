using System;
using System.Collections;
using System.Collections.Generic;
using Deckbuilder.ActionResources;
using Deckbuilder.Actions;
using Deckbuilder.Cards;
using Deckbuilder.Grid;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utils;

namespace Deckbuilder.Combat
{
    [RequireComponent(typeof(ArenaGrid))]
    public class CombatManager : MonoBehaviour
    {
        public Action onCombatStarted;
        public Action onCombatEnded;
        public Action<CombatPhase> onPhaseChanged;
        public Action<Entity> onActiveEntityChanged;

        [SerializeField] private ArenaGrid m_grid;
        [SerializeField, Required] private Transform m_cameraFocusPoint;
        [SerializeField, Required] private TurnCursor m_turnCursorPrefab;
        [SerializeField] private LayerMask m_cellLayerMask = ~0;
        [SerializeField, Required] private CellHighlightStyle m_placementCellStyle;
        [SerializeField, Required] private CellHighlightStyle m_placementHoverStyle;
        [SerializeField, Required] private CellHighlightStyle m_enemySpawnCellStyle;
        [SerializeField, Min(0f)] private float m_combatStartDelay = 0.75f;
        [SerializeField, Min(0f)] private float m_turnTransitionDelay = 0.35f;
        [SerializeField, Min(0f)] private float m_enemyTurnDuration = 0.5f;
        [SerializeField] private CombatDeck m_deck = new();

        public ArenaGrid Grid => m_grid;
        public CombatDeck Deck => m_deck;
        public CombatDeckView DeckView => m_combatCanvas.DeckView;
        public IReadOnlyList<Entity> Enemies => m_enemies;
        public bool IsCombatRunning { get; private set; }
        public CombatPhase Phase { get; private set; }
        public Entity ActiveEntity { get; private set; }
        public bool IsPlayerTurn => Phase == CombatPhase.PlayerTurn;
        public bool IsEndTurnQueued { get; private set; }

        private readonly List<Entity> m_enemies = new();
        private readonly List<Entity> m_enemiesTakingTurn = new();
        private readonly List<GridCell> m_placementCells = new();
        private readonly List<GridCell> m_hoveredCells = new();
        private TurnCursor m_turnCursor;
        private Entity m_player;
        private CombatCanvas m_combatCanvas;
        private Coroutine m_enemyTurnsRoutine;
        private Coroutine m_combatStartRoutine;
        private CellHighlightSource m_placementCellsHighlight;
        private CellHighlightSource m_placementHoverHighlight;
        private CellHighlightSource m_enemySpawnCellsHighlight;
        private EntityActionResourceModule m_playerResources;

        private void Reset()
        {
            m_grid = GetComponent<ArenaGrid>();
        }

        private void OnEnable()
        {
            CursorManager.Instance.SetLockedToNormal(true);
        }

        private void OnDisable()
        {
            if (CursorManager.Instance != null)
                CursorManager.Instance.SetLockedToNormal(false);
        }

        private void Start()
        {
            SceneManager.SetActiveScene(gameObject.scene);
            CameraManager.Instance.FocusOn(m_cameraFocusPoint);
            StartCombat(GameFlow.Instance.Player, GameFlow.Instance.CombatEnemyPrefabs);
        }

        private void StartCombat(Entity _player, IReadOnlyList<Entity> _enemyPrefabs)
        {
            IsCombatRunning = true;

            EntityManager.Instance.onEntityUnregistered += Forget;

            m_grid.BuildIfNeeded();
            m_turnCursor = Instantiate(m_turnCursorPrefab);
            m_turnCursor.SetCellSize(m_grid.CellSize);

            SpawnEnemies(_enemyPrefabs);

            m_combatCanvas = UIManager.Instance.GetCanvas<CombatCanvas>();
            m_combatCanvas.onEndTurnClicked += EndPlayerTurn;
            m_combatCanvas.onEndCombatClicked += EndCombat;
            m_combatCanvas.onLockPlacementClicked += LockPlacement;
            m_combatCanvas.Open();

            m_player = _player;

            BindPlayerResources();
            StartDeck();
            StartPlacement();
        }

        public void EndCombat()
        {
            if (!IsCombatRunning)
                return;

            IsCombatRunning = false;
            IsEndTurnQueued = false;
            SetPhase(CombatPhase.None);
            SetActiveEntity(null);

            if (m_enemyTurnsRoutine != null)
            {
                StopCoroutine(m_enemyTurnsRoutine);
                m_enemyTurnsRoutine = null;
            }

            if (m_combatStartRoutine != null)
            {
                StopCoroutine(m_combatStartRoutine);
                m_combatStartRoutine = null;
            }

            ReleasePlacementHighlights();
            UnbindPersistentListeners();

            m_deck.EndCombat();

            onCombatEnded?.Invoke();

            GameFlow.Instance.EndCombat();
        }

        private void OnDestroy()
        {
            UnbindPersistentListeners();
        }

        private void UnbindPersistentListeners()
        {
            if (EntityManager.Instance != null)
                EntityManager.Instance.onEntityUnregistered -= Forget;

            UnbindPlayerResources();

            if (m_combatCanvas == null)
                return;

            m_combatCanvas.onEndTurnClicked -= EndPlayerTurn;
            m_combatCanvas.onEndCombatClicked -= EndCombat;
            m_combatCanvas.onLockPlacementClicked -= LockPlacement;
            m_combatCanvas.DeckView.Unbind();
            m_combatCanvas.ClearResources();
            m_combatCanvas.SetPlacementModeActive(false);
            m_combatCanvas.Close();

            m_combatCanvas = null;
        }

        public void EndPlayerTurn()
        {
            if (!IsPlayerTurn || IsEndTurnQueued)
                return;

            IsEndTurnQueued = true;
            m_combatCanvas.DeckView.SetInteractable(false);
            m_combatCanvas.SetEndTurnButtonInteractable(false);

            m_player.TryGetModule(out EntityActionQueueModule _playerActionQueue);
            _playerActionQueue.Enqueue(new EndTurnAction(this));
        }

        public IEnumerator PassPlayerTurn()
        {
            while (m_combatCanvas.DeckView.IsTransferring)
                yield return null;

            IsEndTurnQueued = false;
            SetPhase(CombatPhase.EnemyTurn);
            m_enemyTurnsRoutine = StartCoroutine(PlayEnemyTurns());
        }

        public Entity SpawnEntity(Entity _entityPrefab, GridCell _cell)
        {
            Entity _entity = Instantiate(_entityPrefab, _cell.transform.position, _cell.transform.rotation);
            PlaceOnCell(_entity, _cell);

            return _entity;
        }

        private void StartPlacement()
        {
            m_placementCells.Clear();
            m_placementCells.AddRange(m_grid.GetFreeSpawnCells(CellType.AllySpawn));

            if (m_placementCells.Count == 0)
            {
                this.LogError($"The grid has no free {CellType.AllySpawn} cell, the player cannot be placed.");
                return;
            }

            m_placementCellsHighlight = m_grid.CreateHighlightSource(m_placementCellStyle);
            m_placementHoverHighlight = m_grid.CreateHighlightSource(m_placementHoverStyle);
            m_enemySpawnCellsHighlight = m_grid.CreateHighlightSource(m_enemySpawnCellStyle);
            m_placementCellsHighlight.Set(m_placementCells);
            m_enemySpawnCellsHighlight.Set(m_grid.GetEnemySpawnCells());

            SetPhase(CombatPhase.Placement);
            m_combatCanvas.SetPlacementModeActive(true);
            m_combatCanvas.ShowTurnBanner(CombatPhase.Placement);

            PlaceOnCell(m_player, m_placementCells[0]);
            FaceEnemiesTowardsPlayer();
            SetActiveEntity(m_player);
        }

        private void Update()
        {
            if (Phase != CombatPhase.Placement)
                return;

            GridCell _hoveredCell = m_grid.GetCellUnderCursor(m_cellLayerMask);
            bool _canPlaceOnHoveredCell = _hoveredCell != null && !_hoveredCell.IsOccupied && m_placementCells.Contains(_hoveredCell);

            m_hoveredCells.Clear();

            if (_canPlaceOnHoveredCell)
                m_hoveredCells.Add(_hoveredCell);

            m_placementHoverHighlight.Set(m_hoveredCells);

            if (_canPlaceOnHoveredCell && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                MovePlacementTo(_hoveredCell);
        }

        private void MovePlacementTo(GridCell _cell)
        {
            ReleaseCell(m_player);

            m_player.transform.SetPositionAndRotation(_cell.transform.position, _cell.transform.rotation);

            if (!_cell.TrySetOccupant(m_player))
                this.LogWarning($"Cell {_cell.name} is already occupied by {_cell.Occupant.name}.");

            FaceEnemiesTowardsPlayer();
        }

        private void LockPlacement()
        {
            if (Phase != CombatPhase.Placement)
                return;

            ReleasePlacementHighlights();

            m_combatCanvas.SetPlacementModeActive(false);

            onCombatStarted?.Invoke();

            m_combatStartRoutine = StartCoroutine(StartFirstTurn());
        }

        private IEnumerator StartFirstTurn()
        {
            SetPhase(CombatPhase.None);

            yield return new WaitForSeconds(m_combatStartDelay);

            m_combatStartRoutine = null;
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            SetPhase(CombatPhase.PlayerTurn);
            SetActiveEntity(m_player);

            m_combatCanvas.ShowTurnBanner(CombatPhase.PlayerTurn);
            m_deck.DrawTurnHand();
            m_combatCanvas.DeckView.SetInteractable(true);
            m_player.OnTurnStart();
            m_combatCanvas.SetEndTurnButtonInteractable(true);
        }

        private IEnumerator PlayEnemyTurns()
        {
            m_player.OnTurnEnd();
            m_deck.DiscardHand();

            m_combatCanvas.ShowTurnBanner(CombatPhase.EnemyTurn);

            yield return new WaitForSeconds(m_turnTransitionDelay);

            m_enemiesTakingTurn.Clear();
            m_enemiesTakingTurn.AddRange(m_enemies);

            foreach (Entity _enemy in m_enemiesTakingTurn)
            {
                if (m_enemies.Contains(_enemy))
                    yield return PlayEnemyTurn(_enemy);
            }

            yield return new WaitForSeconds(m_turnTransitionDelay);

            m_enemyTurnsRoutine = null;
            StartPlayerTurn();
        }

        private IEnumerator PlayEnemyTurn(Entity _enemy)
        {
            SetActiveEntity(_enemy);
            FaceTowardsPlayer(_enemy);
            _enemy.OnTurnStart();

            if (m_enemies.Contains(_enemy))
                yield return new WaitForSeconds(m_enemyTurnDuration);

            _enemy.OnTurnEnd();
        }

        private void SetPhase(CombatPhase _phase)
        {
            if (Phase == _phase)
                return;

            Phase = _phase;
            onPhaseChanged?.Invoke(_phase);
        }

        private void SetActiveEntity(Entity _entity)
        {
            if (ActiveEntity == _entity)
                return;

            ActiveEntity = _entity;
            m_turnCursor.SetTarget(_entity);
            onActiveEntityChanged?.Invoke(_entity);
        }

        private void ReleasePlacementHighlights()
        {
            if (m_placementCellsHighlight == null)
                return;

            m_placementCellsHighlight.Release();
            m_placementHoverHighlight.Release();
            m_enemySpawnCellsHighlight.Release();

            m_placementCellsHighlight = null;
            m_placementHoverHighlight = null;
            m_enemySpawnCellsHighlight = null;
        }

        private void BindPlayerResources()
        {
            m_player.TryGetModule(out m_playerResources);

            m_combatCanvas.ShowResources(m_playerResources.Resources);
            m_combatCanvas.DeckView.SetAffordabilityCheck(CanPlayerAfford);
            m_playerResources.OnResourceChanged += HandlePlayerResourceChanged;

            foreach (ActionResourceConfig _resource in m_playerResources.Resources)
                HandlePlayerResourceChanged(_resource, m_playerResources.GetRemaining(_resource), m_playerResources.GetMax(_resource));
        }

        private void UnbindPlayerResources()
        {
            if (m_playerResources != null)
                m_playerResources.OnResourceChanged -= HandlePlayerResourceChanged;

            m_playerResources = null;
        }

        private bool CanPlayerAfford(CardInstance _card)
        {
            return m_playerResources.CanAfford(_card.CostResource, _card.Cost);
        }

        private void HandlePlayerResourceChanged(ActionResourceConfig _resource, int _remaining, int _max)
        {
            m_combatCanvas.SetResource(_resource, _remaining, _max);
            m_combatCanvas.DeckView.RefreshPlayableHighlights();
        }

        private void StartDeck()
        {
            m_player.TryGetModule(out PlayerDeckModule _playerDeck);

            m_deck.StartCombat(_playerDeck.Cards, RunRandom.GetStream(RandomChannel.Combat));
            m_combatCanvas.DeckView.Bind(m_deck);
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

        private void PlaceOnCell(Entity _entity, GridCell _cell)
        {
            _entity.OnCombatEnter();
            _entity.transform.SetPositionAndRotation(_cell.transform.position, _cell.transform.rotation);

            if (!_cell.TrySetOccupant(_entity))
                this.LogWarning($"Cell {_cell.name} is already occupied by {_cell.Occupant.name}.");

            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.SetCombatManager(this);
        }

        private void FaceEnemiesTowardsPlayer()
        {
            foreach (Entity _enemy in m_enemies)
                FaceTowardsPlayer(_enemy);
        }

        private void FaceTowardsPlayer(Entity _entity)
        {
            if (_entity.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.FaceTowards(m_player.transform.position);
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

            if (ActiveEntity == _entity)
                SetActiveEntity(null);

            if (m_player == _entity)
                m_player = null;
        }
    }
}
