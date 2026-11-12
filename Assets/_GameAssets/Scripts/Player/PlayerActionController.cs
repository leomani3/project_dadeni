using System.Collections.Generic;
using Deckbuilder.Actions;
using Deckbuilder.Cards;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckbuilder.Player
{
    public class PlayerActionController : EntityModule
    {
        [SerializeField] private LayerMask m_cellLayerMask = ~0;
        [SerializeField, Required] private CellHighlightStyle m_movementRangeStyle;
        [SerializeField, Required] private CellHighlightStyle m_movementPathStyle;
        [SerializeField, Required] private CellHighlightStyle m_targetZoneStyle;
        [SerializeField, Required] private CellHighlightStyle m_targetZoneBlockedStyle;
        [SerializeField, Required] private CellHighlightStyle m_effectZoneStyle;

        private GridCell m_hoveredCell;
        private CombatManager m_combatManager;
        private ArenaGrid m_grid;

        private CellHighlightSource m_movementRangeHighlight;
        private CellHighlightSource m_movementPathHighlight;
        private CellHighlightSource m_targetZoneHighlight;
        private CellHighlightSource m_targetZoneBlockedHighlight;
        private CellHighlightSource m_effectZoneHighlight;

        private readonly List<GridCell> m_reachableCells = new();
        private readonly List<GridCell> m_zoneCells = new();
        private readonly List<GridCell> m_targetCells = new();
        private readonly List<GridCell> m_blockedTargetCells = new();

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

            ClearMoveHighlights();
            ClearCardHighlights();
        }

        public override void OnCombatExit()
        {
            base.OnCombatExit();

            enabled = false;

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

            m_movementRangeHighlight = null;
            m_movementPathHighlight = null;
            m_targetZoneHighlight = null;
            m_targetZoneBlockedHighlight = null;
            m_effectZoneHighlight = null;
        }

        private void Update()
        {
            UpdateHover();

            if (!ResolveCombatManager())
                return;

            CombatDeckView _deckView = m_combatManager.DeckView;
            GridCell _entityCell = CardExecutor.GetEffectiveCell(Owner);

            if (_entityCell == null)
            {
                ClearMoveHighlights();
                ClearCardHighlights();
                return;
            }

            if (_deckView.SelectedCard != null)
                UpdateCardTargeting(_deckView, _deckView.SelectedCard, _entityCell);
            else if (_deckView.FocusedCard != null)
                UpdateCardPreview(_deckView.FocusedCard, _entityCell);
            else
                UpdateMoveMode(_entityCell);
        }

        private void UpdateHover()
        {
            m_hoveredCell = null;

            if (UIManager.Instance.IsOverUI)
                return;

            if (CameraManager.Instance == null)
                return;

            Camera _camera = CameraManager.Instance.MainCam;
            if (_camera == null || Mouse.current == null)
                return;

            Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(_ray, out RaycastHit _hit, Mathf.Infinity, m_cellLayerMask))
                m_hoveredCell = _hit.collider.GetComponentInParent<GridCell>();
        }

        private void UpdateMoveMode(GridCell _entityCell)
        {
            ClearCardHighlights();

            m_grid.GetReachableCells(_entityCell, GetRemainingMovementPoints(), m_reachableCells);
            m_movementRangeHighlight.Set(m_reachableCells);

            bool _isHoveredCellReachable = m_hoveredCell != null && m_reachableCells.Contains(m_hoveredCell);
            List<GridCell> _path = _isHoveredCellReachable ? m_grid.FindPath(_entityCell, m_hoveredCell) : null;

            if (_path != null)
                m_movementPathHighlight.Set(_path);
            else
                m_movementPathHighlight.Clear();

            if (_isHoveredCellReachable && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                EnqueueMove(m_hoveredCell);
        }

        private void UpdateCardPreview(CardInstance _card, GridCell _entityCell)
        {
            ClearMoveHighlights();
            ShowLaunchZone(_card, _entityCell);
            m_effectZoneHighlight.Clear();
        }

        private void UpdateCardTargeting(CombatDeckView _deckView, CardInstance _card, GridCell _entityCell)
        {
            ClearMoveHighlights();
            ShowLaunchZone(_card, _entityCell);

            bool _canTargetHoveredCell = m_hoveredCell != null && CardExecutor.CanTarget(m_combatManager, _card, Owner, m_hoveredCell);
            _deckView.SetArrowOnValidTarget(_canTargetHoveredCell);

            if (_canTargetHoveredCell)
            {
                ZoneDefinition _effectZone = _card.EffectZone;
                m_grid.GetCellsInZone(m_hoveredCell.Coordinate, _effectZone.Shape, _effectZone.MaxRange, _effectZone.MinRange, m_zoneCells);
                m_effectZoneHighlight.Set(m_zoneCells);
            }
            else
            {
                m_effectZoneHighlight.Clear();
            }

            if (Mouse.current == null)
                return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                _deckView.CancelSelection();
                return;
            }

            bool _confirmPressed = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame;
            if (_confirmPressed && _canTargetHoveredCell)
                PlayCard(_card, m_hoveredCell);
            else if (Mouse.current.leftButton.wasPressedThisFrame)
                _deckView.CancelSelection();
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

        private void EnqueueMove(GridCell _destination)
        {
            Owner.TryGetModule(out EntityActionQueueModule _queue);
            _queue.Enqueue(new MoveAction(Owner, _destination));
        }

        private void PlayCard(CardInstance _card, GridCell _targetCell)
        {
            Owner.TryGetModule(out EntityActionQueueModule _queue);

            m_combatManager.Deck.Play(_card);
            _queue.Enqueue(new PlayCardAction(m_combatManager, _card, Owner, _targetCell));
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
        }
    }
}
