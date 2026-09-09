using System.Collections.Generic;
using Deckbuilder.Actions;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckbuilder.Player
{
    public class PlayerActionController : EntityModule
    {
        private enum Mode
        {
            Move,
            CardPreview
        }

        private static readonly List<GridCell> NoCells = new();

        [SerializeField] private CardConfig m_testCard;
        [SerializeField] private Key m_moveModeKey = Key.Digit1;
        [SerializeField] private Key m_selectCardKey = Key.Digit2;

        [SerializeField] private LayerMask m_cellLayerMask = ~0;

        private Mode m_mode = Mode.Move;
        private GridCell m_hoveredCell;
        private Arena m_arena;
        private ArenaGrid m_grid;

        private readonly List<GridCell> m_targetCells = new();
        private readonly List<GridCell> m_blockedTargetCells = new();

        protected override void OnInitialize()
        {
            base.OnInitialize();

            enabled = false;
        }

        public override void OnCombatEnter()
        {
            base.OnCombatEnter();

            enabled = true;
        }

        public override void OnCombatExit()
        {
            base.OnCombatExit();

            m_mode = Mode.Move;
            enabled = false;
        }

        private bool ResolveArena()
        {
            m_arena = Owner.TryGetModule(out EntityCombatMoverModule _combatMover) ? _combatMover.Arena : null;
            m_grid = m_arena != null ? m_arena.Grid : null;

            return m_arena != null && m_grid != null;
        }

        private void Update()
        {
            UpdateModeSwitch();
            UpdateHover();

            if (!ResolveArena())
                return;

            if (m_mode == Mode.Move)
                UpdateMoveMode(Owner);
            else
                UpdateCardPreviewMode(Owner);
        }

        private void UpdateModeSwitch()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current[m_moveModeKey].wasPressedThisFrame)
                m_mode = Mode.Move;
            else if (m_mode == Mode.Move && m_testCard != null && Keyboard.current[m_selectCardKey].wasPressedThisFrame)
                m_mode = Mode.CardPreview;
        }

        private void UpdateHover()
        {
            m_hoveredCell = null;

            if (CameraManager.Instance == null)
                return;

            Camera _camera = CameraManager.Instance.MainCam;
            if (_camera == null || Mouse.current == null)
                return;

            Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(_ray, out RaycastHit _hit, Mathf.Infinity, m_cellLayerMask))
                m_hoveredCell = _hit.collider.GetComponentInParent<GridCell>();
        }

        private void UpdateMoveMode(Entity _entity)
        {
            ClearCardHighlights();

            GridCell _entityCell = CardExecutor.GetEffectiveCell(_entity);
            if (_entityCell == null)
            {
                ClearMoveHighlights();
                return;
            }

            List<GridCell> _reachableCells = m_grid.GetReachableCells(_entityCell, GetMovementPoints(_entity));
            m_grid.SetHighlightLayer(HighlightLayer.MovementRange, _reachableCells);

            bool _isHoveredCellReachable = m_hoveredCell != null && _reachableCells.Contains(m_hoveredCell);
            List<GridCell> _path = _isHoveredCellReachable ? m_grid.FindPath(_entityCell, m_hoveredCell) : null;

            m_grid.SetHighlightLayer(HighlightLayer.MovementPath, _path ?? NoCells);

            if (_isHoveredCellReachable && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                EnqueueMove(_entity, m_hoveredCell);
        }

        private void UpdateCardPreviewMode(Entity _entity)
        {
            ClearMoveHighlights();

            GridCell _entityCell = CardExecutor.GetEffectiveCell(_entity);
            if (_entityCell == null || m_testCard == null)
            {
                ClearCardHighlights();
                m_mode = Mode.Move;
                return;
            }

            CollectTargetCells(_entity, _entityCell);
            m_grid.SetHighlightLayer(HighlightLayer.TargetZone, m_targetCells);
            m_grid.SetHighlightLayer(HighlightLayer.TargetZoneBlocked, m_blockedTargetCells);

            bool _canTargetHoveredCell = m_hoveredCell != null && CardExecutor.CanTarget(m_arena, m_testCard, _entity, m_hoveredCell);

            IEnumerable<GridCell> _effectCells = NoCells;
            if (_canTargetHoveredCell)
            {
                ZoneDefinition _effectZone = m_testCard.EffectZone;
                _effectCells = m_grid.GetCellsInZone(m_hoveredCell.Coordinate, _effectZone.Shape, _effectZone.MaxRange, _effectZone.MinRange);
            }

            m_grid.SetHighlightLayer(HighlightLayer.EffectZone, _effectCells);

            if (_canTargetHoveredCell && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                EnqueueCard(_entity, m_hoveredCell);
                m_mode = Mode.Move;
            }
        }

        private void CollectTargetCells(Entity _entity, GridCell _entityCell)
        {
            m_targetCells.Clear();
            m_blockedTargetCells.Clear();

            ZoneDefinition _targetZone = m_testCard.TargetZone;

            foreach (GridCell _cell in m_grid.GetCellsInZone(_entityCell.Coordinate, _targetZone.Shape, _targetZone.MaxRange, _targetZone.MinRange))
            {
                bool _hasLineOfSight = !m_testCard.RequiresLineOfSight
                    || m_grid.HasLineOfSight(_entityCell, _cell, out GridCell _blockingCell, _entity);

                if (_hasLineOfSight)
                    m_targetCells.Add(_cell);
                else
                    m_blockedTargetCells.Add(_cell);
            }
        }

        private void EnqueueMove(Entity _entity, GridCell _destination)
        {
            if (_entity.TryGetModule(out EntityActionQueueModule _queue))
                _queue.Enqueue(new MoveAction(_entity, _destination));
        }

        private void EnqueueCard(Entity _entity, GridCell _targetCell)
        {
            if (_entity.TryGetModule(out EntityActionQueueModule _queue))
                _queue.Enqueue(new PlayCardAction(m_arena, m_testCard, _entity, _targetCell));
        }

        private int GetMovementPoints(Entity _entity)
        {
            if (_entity.TryGetModule(out EntityStatModule _statModule))
                return Mathf.RoundToInt(_statModule.GetValue(StatType.MovementPoints));

            return 3;
        }

        private void ClearMoveHighlights()
        {
            m_grid.ClearHighlightLayer(HighlightLayer.MovementRange);
            m_grid.ClearHighlightLayer(HighlightLayer.MovementPath);
        }

        private void ClearCardHighlights()
        {
            m_grid.ClearHighlightLayer(HighlightLayer.TargetZone);
            m_grid.ClearHighlightLayer(HighlightLayer.TargetZoneBlocked);
            m_grid.ClearHighlightLayer(HighlightLayer.EffectZone);
        }
    }
}
