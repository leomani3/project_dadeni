using System.Collections.Generic;
using Deckbuilder.Actions;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deckbuilder.Player
{
    public class PlayerActionController : EntityModule
    {
        [SerializeField] private LayerMask m_cellLayerMask = ~0;
        [SerializeField] private bool m_requireClickToEnterMoveMode;
        [SerializeField, Required] private CellHighlightStyle m_movementRangeStyle;
        [SerializeField, Required] private CellHighlightStyle m_movementPathStyle;
        [SerializeField, Required] private CellHighlightStyle m_targetZoneStyle;
        [SerializeField, Required] private CellHighlightStyle m_targetZoneBlockedStyle;
        [SerializeField, Required] private CellHighlightStyle m_effectZoneStyle;
        [SerializeField, Required] private CellHighlightStyle m_targetCellStyle;

        private GridCell m_hoveredCell;
        private bool m_isMoveModeActive;
        private CombatManager m_combatManager;
        private ArenaGrid m_grid;

        private CellHighlightSource m_movementRangeHighlight;
        private CellHighlightSource m_movementPathHighlight;
        private CellHighlightSource m_targetZoneHighlight;
        private CellHighlightSource m_targetZoneBlockedHighlight;
        private CellHighlightSource m_effectZoneHighlight;
        private CellHighlightSource m_targetCellHighlight;

        private readonly GridPathSteering m_pathSteering = new();
        private readonly List<GridCell> m_reachableCells = new();
        private readonly List<GridCell> m_pathCells = new();
        private readonly List<GridCell> m_zoneCells = new();
        private readonly List<GridCell> m_targetCells = new();
        private readonly List<GridCell> m_blockedTargetCells = new();
        private readonly List<GridCell> m_hoveredCells = new();
        private readonly List<RaycastResult> m_uiRaycastResults = new();

        protected override void OnInitialize()
        {
            base.OnInitialize();

            enabled = false;
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();

            enabled = true;

            ResolveCombatManager();
            CreateHighlightSources();
        }

        public override void OnTurnEnd()
        {
            base.OnTurnEnd();

            enabled = false;
            SetMoveModeActive(false);

            ClearMoveHighlights();
            ClearCardHighlights();
        }

        public override void OnCombatExit()
        {
            base.OnCombatExit();

            enabled = false;
            SetMoveModeActive(false);

            ReleaseHighlightSources();
        }

        private bool ResolveCombatManager()
        {
            m_combatManager = Owner.TryGetModule(out EntityCombatMoverModule _combatMover) ? _combatMover.CombatManager : null;
            m_grid = m_combatManager != null ? m_combatManager.Grid : null;

            return m_combatManager != null && m_grid != null;
        }

        private void CreateHighlightSources()
        {
            if (m_movementRangeHighlight != null)
                return;

            m_movementRangeHighlight = m_grid.CreateHighlightSource(m_movementRangeStyle);
            m_movementPathHighlight = m_grid.CreateHighlightSource(m_movementPathStyle);
            m_targetZoneHighlight = m_grid.CreateHighlightSource(m_targetZoneStyle);
            m_targetZoneBlockedHighlight = m_grid.CreateHighlightSource(m_targetZoneBlockedStyle);
            m_effectZoneHighlight = m_grid.CreateHighlightSource(m_effectZoneStyle);
            m_targetCellHighlight = m_grid.CreateHighlightSource(m_targetCellStyle);
        }

        private void ReleaseHighlightSources()
        {
            if (m_movementRangeHighlight == null)
                return;

            m_movementRangeHighlight.Release();
            m_movementPathHighlight.Release();
            m_targetZoneHighlight.Release();
            m_targetZoneBlockedHighlight.Release();
            m_effectZoneHighlight.Release();
            m_targetCellHighlight.Release();

            m_movementRangeHighlight = null;
            m_movementPathHighlight = null;
            m_targetZoneHighlight = null;
            m_targetZoneBlockedHighlight = null;
            m_effectZoneHighlight = null;
            m_targetCellHighlight = null;
        }

        private void Update()
        {
            if (!ResolveCombatManager())
            {
                m_hoveredCell = null;
                return;
            }

            m_hoveredCell = m_grid.GetCellUnderCursor(m_cellLayerMask);

            CombatDeckView _deckView = m_combatManager.DeckView;
            GridCell _entityCell = CardExecutor.GetEffectiveCell(Owner);

            if (_entityCell == null || m_combatManager.IsEndTurnQueued)
            {
                SetMoveModeActive(false);
                ClearMoveHighlights();
                ClearCardHighlights();
                return;
            }

            if (_deckView.SelectedCard != null)
            {
                SetMoveModeActive(false);
                UpdateCardTargeting(_deckView, _deckView.SelectedCard, _entityCell);
            }
            else if (_deckView.FocusedCard != null)
            {
                SetMoveModeActive(false);
                UpdateCardPreview(_deckView.FocusedCard, _entityCell);
            }
            else
            {
                UpdateMoveMode(_entityCell);
            }
        }

        private void UpdateMoveMode(GridCell _entityCell)
        {
            ClearCardHighlights();

            bool _leftPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (!m_requireClickToEnterMoveMode)
            {
                UpdateMovement(_entityCell, _leftPressed, true);
                return;
            }

            bool _isHoveringCharacter = m_hoveredCell == _entityCell;

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                SetMoveModeActive(false);

            if (_isHoveringCharacter && _leftPressed)
                SetMoveModeActive(!m_isMoveModeActive);

            if (!m_isMoveModeActive && !_isHoveringCharacter)
            {
                ClearMoveHighlights();
                return;
            }

            UpdateMovement(_entityCell, _leftPressed, m_isMoveModeActive);
        }

        private void UpdateMovement(GridCell _entityCell, bool _leftPressed, bool _canConfirmMove)
        {
            int _movementPoints = GetRemainingMovementPoints();
            m_grid.GetReachableCells(_entityCell, _movementPoints, m_reachableCells);
            m_movementRangeHighlight.Set(m_reachableCells);

            bool _canMoveToHoveredCell = _canConfirmMove
                && m_hoveredCell != null
                && m_reachableCells.Contains(m_hoveredCell)
                && m_pathSteering.TrySteerTo(m_grid, _entityCell.Coordinate, m_hoveredCell.Coordinate, _movementPoints);

            if (_canMoveToHoveredCell)
            {
                CollectSteeredPathCells();
                m_movementPathHighlight.Set(m_pathCells);
            }
            else
            {
                m_movementPathHighlight.Clear();
            }

            if (_canMoveToHoveredCell && _leftPressed)
            {
                EnqueueMove();
                SetMoveModeActive(false);
            }
        }

        private void SetMoveModeActive(bool _isActive)
        {
            m_isMoveModeActive = _isActive;
            m_pathSteering.Clear();
        }

        private void CollectSteeredPathCells()
        {
            m_pathCells.Clear();

            foreach (Vector2Int _coordinate in m_pathSteering.Path)
                m_pathCells.Add(m_grid.GetCell(_coordinate));
        }

        private void UpdateCardPreview(CardInstance _card, GridCell _entityCell)
        {
            ClearMoveHighlights();
            ShowLaunchZone(_card, _entityCell);
            m_effectZoneHighlight.Clear();
            m_targetCellHighlight.Clear();
        }

        private void UpdateCardTargeting(CombatDeckView _deckView, CardInstance _card, GridCell _entityCell)
        {
            ClearMoveHighlights();
            ShowLaunchZone(_card, _entityCell);

            bool _isLockedOnSelf = IsLockedOnSelf(_entityCell);
            GridCell _targetCell = _isLockedOnSelf ? _entityCell : m_hoveredCell;

            bool _canTargetCell = _targetCell != null
                && CanAfford(_card)
                && CardExecutor.CanTarget(m_combatManager, _card, Owner, _targetCell);
            _deckView.SetArrowOnValidTarget(_canTargetCell);
            _deckView.SetArrowLockedWorldPoint(_isLockedOnSelf ? GetSelfLockPoint() : null);

            if (_canTargetCell)
            {
                ZoneDefinition _effectZone = _card.EffectZone;
                m_grid.GetCellsInZone(_targetCell.Coordinate, _effectZone.Shape, _effectZone.MaxRange, _effectZone.MinRange, m_zoneCells);
                m_effectZoneHighlight.Set(m_zoneCells);

                m_hoveredCells.Clear();
                m_hoveredCells.Add(_targetCell);
                m_targetCellHighlight.Set(m_hoveredCells);
            }
            else
            {
                m_effectZoneHighlight.Clear();
                m_targetCellHighlight.Clear();
            }

            if (Mouse.current == null)
                return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                _deckView.CancelSelection();
                return;
            }

            bool _confirmPressed = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame;
            if (_isLockedOnSelf && IsPointerOverUI())
                return;

            if (_confirmPressed && _canTargetCell)
                PlayCard(_card, _targetCell);
            else if (Mouse.current.leftButton.wasPressedThisFrame && !_isLockedOnSelf)
                _deckView.CancelSelection();
        }

        private bool IsLockedOnSelf(GridCell _entityCell)
        {
            return m_targetCells.Count == 1 && m_blockedTargetCells.Count == 0 && m_targetCells[0] == _entityCell;
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData _pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
            m_uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerData, m_uiRaycastResults);

            foreach (RaycastResult _result in m_uiRaycastResults)
            {
                if (_result.module is GraphicRaycaster)
                    return true;
            }

            return false;
        }

        private Vector3 GetSelfLockPoint()
        {
            Owner.TryGetModule(out EntityAnimationModule _animationModule);
            return _animationModule.HitPoint.position;
        }

        private void ShowLaunchZone(CardInstance _card, GridCell _entityCell)
        {
            m_targetCells.Clear();
            m_blockedTargetCells.Clear();

            ZoneDefinition _targetZone = _card.TargetZone;
            m_grid.GetCellsInZone(_entityCell.Coordinate, _targetZone.Shape, _targetZone.MaxRange, _targetZone.MinRange, m_zoneCells);

            foreach (GridCell _cell in m_zoneCells)
            {
                bool _hasLineOfSight = !_card.RequiresLineOfSight
                    || m_grid.HasLineOfSight(_entityCell, _cell, out GridCell _blockingCell, Owner);

                if (_hasLineOfSight)
                    m_targetCells.Add(_cell);
                else
                    m_blockedTargetCells.Add(_cell);
            }

            m_targetZoneHighlight.Set(m_targetCells);
            m_targetZoneBlockedHighlight.Set(m_blockedTargetCells);
        }

        private void EnqueueMove()
        {
            Owner.TryGetModule(out EntityActionQueueModule _queue);
            _queue.Enqueue(new MoveAction(Owner, m_pathCells));
        }

        private void PlayCard(CardInstance _card, GridCell _targetCell)
        {
            Owner.TryGetModule(out EntityActionResourceModule _resources);

            if (!_resources.TrySpend(_card.CostResource, _card.Cost))
                return;

            Owner.TryGetModule(out EntityActionQueueModule _queue);

            m_combatManager.Deck.Play(_card);
            _queue.Enqueue(new PlayCardAction(m_combatManager, _card, Owner, _targetCell));
        }

        private bool CanAfford(CardInstance _card)
        {
            Owner.TryGetModule(out EntityActionResourceModule _resources);
            return _resources.CanAfford(_card.CostResource, _card.Cost);
        }

        private int GetRemainingMovementPoints()
        {
            Owner.TryGetModule(out EntityCombatMoverModule _combatMover);
            return _combatMover.RemainingMovementPoints;
        }

        private void ClearMoveHighlights()
        {
            m_movementRangeHighlight.Clear();
            m_movementPathHighlight.Clear();
        }

        private void ClearCardHighlights()
        {
            m_targetZoneHighlight.Clear();
            m_targetZoneBlockedHighlight.Clear();
            m_effectZoneHighlight.Clear();
            m_targetCellHighlight.Clear();
        }
    }
}
