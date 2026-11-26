using System;
using System.Collections.Generic;
using System.Globalization;
using Deckbuilder.Cards;
using Lean.Pool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatDeckView : MonoBehaviour
{
    private class HandArrival
    {
        public float StartTime;
        public bool HasStarted;
        public Vector2 StartPosition;
        public float StartAngle;
        public float StartScale;
        public Vector2 SmoothedSlotPosition;
        public float SmoothedSlotAngle;
        public float SmoothedSlotScale;
    }

    private static readonly Key[] HotkeyDigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0 };
    private static readonly Key[] HotkeyNumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9, Key.Numpad0 };

    public event Action onTransfersSettled;
    public event Action<CardInstance> onUnaffordableCardRejected;

    [SerializeField, Required] private Canvas _canvas;
    [SerializeField, Required] private Card _cardPrefab;
    [SerializeField, Required] private RectTransform _cardContainer;
    [SerializeField, Required] private RectTransform _flightLayer;
    [SerializeField, Required] private CardTrail _trailPrefab;
    [SerializeField, Required] private CardTargetingArrow _targetingArrow;
    [SerializeField] private bool _showHotkeyLabels;

    [SerializeField, Required] private RectTransform _drawPileAnchor;
    [SerializeField, Required] private TMP_Text _drawPileCountLabel;
    [SerializeField, Required] private RectTransform _discardPileAnchor;
    [SerializeField, Required] private TMP_Text _discardPileCountLabel;
    [SerializeField, Required] private RectTransform _exhaustPileAnchor;
    [SerializeField, Required] private TMP_Text _exhaustPileCountLabel;

    [SerializeField, Min(0f)] private float _cardSpacing = 150f;
    [SerializeField, Min(0f)] private float _maxHandWidth = 900f;
    [SerializeField] private float _maxFanAngle = 10f;
    [SerializeField] private float _fanHeight = 30f;

    [SerializeField] private float _hoverHeight = 160f;
    [SerializeField, Min(0f)] private float _hoverScale = 1.3f;
    [SerializeField] private float _hoverNeighbourPush = 70f;

    [SerializeField, Range(0f, 1f)] private float _selectionScreenHeight = 0.3f;
    [SerializeField, Min(0f)] private float _clickMaxDistance = 20f;
    [SerializeField, Min(0f)] private float _draggedScale = 1.15f;
    [SerializeField] private Vector2 _selectedPosition = new Vector2(0f, 150f);
    [SerializeField, Min(0f)] private float _selectedScale = 1.1f;

    [SerializeField, Min(0f)] private float _layoutDuration = 0.2f;
    [SerializeField, Min(0f)] private float _flightDuration = 0.375f;
    [SerializeField, Min(0f)] private float _pileCardScale = 0.1f;
    [SerializeField, Min(0f)] private float _pileFlightDuration = 0.55f;
    [SerializeField, Min(0f)] private float _arcHeightRatio = 0.7f;
    [SerializeField, Min(0f)] private float _handTransferInterval = 0.085f;
    [SerializeField, Min(0f)] private float _pileTransferInterval = 0.0275f;

    private readonly Dictionary<CardInstance, Card> _handViews = new Dictionary<CardInstance, Card>();
    private readonly Dictionary<CardInstance, Card> _detachedViews = new Dictionary<CardInstance, Card>();
    private readonly Dictionary<CardInstance, CardPile> _pendingSourcePiles = new Dictionary<CardInstance, CardPile>();
    private readonly Dictionary<Card, HandArrival> _arrivingViews = new Dictionary<Card, HandArrival>();
    private readonly List<Card> _landedViews = new List<Card>();
    private readonly List<Card> _flyingViews = new List<Card>();
    private readonly List<Card> _orderedHandViews = new List<Card>();

    private CombatDeck _deck;
    private Card _hoveredView;
    private Card _draggedView;
    private Vector2 _pressScreenPosition;
    private bool _isDragButtonHeld;
    private CardInstance _selectedCard;
    private bool _isInteractable;
    private bool _isArrowOnValidTarget;
    private bool _wasTransferring;
    private float _nextTransferStartTime;
    private int _availableActionPoints;

    public CardInstance HoveredCard => _hoveredView != null ? _hoveredView.Instance : null;
    public CardInstance DraggedCard => _draggedView != null ? _draggedView.Instance : null;
    public CardInstance SelectedCard => _selectedCard;
    public CardInstance FocusedCard => _selectedCard ?? DraggedCard ?? HoveredCard;
    public bool IsTransferring => _flyingViews.Count > 0 || _arrivingViews.Count > 0;

    private Camera EventCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

    public void Bind(CombatDeck _combatDeck)
    {
        Unbind();

        _deck = _combatDeck;
        foreach (CardPile _pile in EnumeratePiles())
        {
            _pile.onCardAdded += HandleCardAdded;
            _pile.onCardRemoved += HandleCardRemoved;
        }

        foreach (CardInstance _card in _deck.Hand.Cards)
            _handViews.Add(_card, SpawnCardView(_card, Vector2.zero, 1f, true));

        _nextTransferStartTime = Time.time;
        RefreshPileCounts();
        RefreshHandLayout();
    }

    public void Unbind()
    {
        if (_deck != null)
        {
            foreach (CardPile _pile in EnumeratePiles())
            {
                _pile.onCardAdded -= HandleCardAdded;
                _pile.onCardRemoved -= HandleCardRemoved;
            }

            _deck = null;
        }

        foreach (Card _view in _handViews.Values)
            DespawnCardView(_view);
        foreach (Card _view in _detachedViews.Values)
            DespawnCardView(_view);
        foreach (Card _view in _flyingViews)
            DespawnCardView(_view);

        _handViews.Clear();
        _detachedViews.Clear();
        _flyingViews.Clear();
        _arrivingViews.Clear();
        _pendingSourcePiles.Clear();
        _orderedHandViews.Clear();

        _hoveredView = null;
        _draggedView = null;
        _selectedCard = null;
        _isInteractable = false;
        _wasTransferring = false;
        _targetingArrow.Hide();
    }

    public void SetInteractable(bool _interactable)
    {
        _isInteractable = _interactable;

        if (!_isInteractable)
        {
            _hoveredView = null;
            _draggedView = null;
            _selectedCard = null;
        }

        RefreshHandLayout();
    }

    public void SetAvailableActionPoints(int _actionPoints)
    {
        _availableActionPoints = _actionPoints;
        RefreshPlayableHighlights();
    }

    public void CancelSelection()
    {
        _selectedCard = null;
        _draggedView = null;
        RefreshHandLayout();
    }

    public void SetArrowOnValidTarget(bool _isOnValidTarget)
    {
        _isArrowOnValidTarget = _isOnValidTarget;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Update()
    {
        if (_deck == null)
            return;

        UpdateArrivals();

        if (!_isInteractable)
            return;

        UpdateHotkeys();
        UpdateDrag();
    }

    private void LateUpdate()
    {
        DespawnUnclaimedDetachedViews();
        _pendingSourcePiles.Clear();
        UpdateTargetingArrow();
        NotifyTransfersSettled();
    }

    private void UpdateHotkeys()
    {
        if (Keyboard.current == null)
            return;

        for (int i = 0; i < HotkeyDigitKeys.Length; i++)
        {
            if (Keyboard.current[HotkeyDigitKeys[i]].wasPressedThisFrame || Keyboard.current[HotkeyNumpadKeys[i]].wasPressedThisFrame)
            {
                ToggleSelectionAtHandIndex(i);
                return;
            }
        }
    }

    private void ToggleSelectionAtHandIndex(int _handIndex)
    {
        if (_handIndex >= _deck.Hand.Count)
            return;

        CardInstance _card = _deck.Hand.Cards[_handIndex];
        if (!_handViews.TryGetValue(_card, out Card _view) || _arrivingViews.ContainsKey(_view))
            return;

        if (_selectedCard == _card)
        {
            CancelSelection();
            return;
        }

        if (!CanAfford(_card))
        {
            RejectUnaffordableCard(_card);
            return;
        }

        _draggedView = null;
        _hoveredView = null;
        _selectedCard = _card;
        RefreshHandLayout();
    }

    private void UpdateDrag()
    {
        if (_draggedView == null || Mouse.current == null)
            return;

        Vector2 _pointerScreenPosition = Mouse.current.position.ReadValue();

        if (_isDragButtonHeld && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (Vector2.Distance(_pointerScreenPosition, _pressScreenPosition) <= _clickMaxDistance)
                _isDragButtonHeld = false;
            else
            {
                CancelSelection();
                return;
            }
        }
        else if (!_isDragButtonHeld && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            CancelSelection();
            return;
        }

        _draggedView.SnapTo(GetPointerLocalPosition(), 0f, _draggedScale);

        if (_pointerScreenPosition.y > Screen.height * _selectionScreenHeight)
            SelectDraggedCard();
    }

    private void SelectDraggedCard()
    {
        if (!CanAfford(_draggedView.Instance))
        {
            RejectUnaffordableCard(_draggedView.Instance);
            return;
        }

        _selectedCard = _draggedView.Instance;
        _draggedView = null;
        _hoveredView = null;
        RefreshHandLayout();
    }

    private bool CanAfford(CardInstance _card)
    {
        return _card.Cost <= _availableActionPoints;
    }

    private void RejectUnaffordableCard(CardInstance _card)
    {
        CancelSelection();
        onUnaffordableCardRejected?.Invoke(_card);
    }

    private void NotifyTransfersSettled()
    {
        bool _isTransferring = IsTransferring;
        if (_wasTransferring && !_isTransferring)
            onTransfersSettled?.Invoke();

        _wasTransferring = _isTransferring;
    }

    private void HandleCardRemoved(CardPile _pile, CardInstance _card)
    {
        _pendingSourcePiles[_card] = _pile;

        if (_pile == _deck.Hand && _handViews.TryGetValue(_card, out Card _view))
        {
            _handViews.Remove(_card);
            _arrivingViews.Remove(_view);
            _detachedViews[_card] = _view;
            _view.SetHotkey(string.Empty);
            _view.SetPlayable(false);
            _view.SetAffordable(true);

            if (_hoveredView == _view)
                _hoveredView = null;
            if (_draggedView == _view)
                _draggedView = null;
            if (_selectedCard == _card)
                _selectedCard = null;

            RefreshHandLayout();
        }

        RefreshPileCounts();
    }

    private void HandleCardAdded(CardPile _pile, CardInstance _card)
    {
        _pendingSourcePiles.TryGetValue(_card, out CardPile _sourcePile);
        _pendingSourcePiles.Remove(_card);

        if (_detachedViews.TryGetValue(_card, out Card _detachedView))
        {
            _detachedViews.Remove(_card);
            SendViewTo(_detachedView, _pile, _handTransferInterval);
        }
        else if (_sourcePile != null)
        {
            bool _isDrawnIntoHand = _pile == _deck.Hand;
            Card _view = SpawnCardView(_card, GetPileLocalPosition(_sourcePile), _isDrawnIntoHand ? 0f : _pileCardScale, _isDrawnIntoHand);
            SendViewTo(_view, _pile, _pile == _deck.Hand ? _handTransferInterval : _pileTransferInterval);
        }

        RefreshPileCounts();
    }

    private void SendViewTo(Card _view, CardPile _pile, float _transferInterval)
    {
        float _startTime = ScheduleTransfer(_transferInterval);
        _view.SetBlocksRaycasts(false);

        if (_pile == _deck.Hand)
        {
            _view.RectTransform.SetParent(_cardContainer, false);
            _view.RectTransform.SetAsLastSibling();
            _handViews[_view.Instance] = _view;
            _arrivingViews.Add(_view, new HandArrival { StartTime = _startTime });
            _view.SetFaceUp(true);
            RefreshHandLayout();
            return;
        }

        _view.RectTransform.SetParent(_flightLayer, false);
        _view.RectTransform.SetAsLastSibling();

        CardTrail _trail = LeanPool.Spawn(_trailPrefab, _flightLayer);
        _trail.rectTransform.SetAsFirstSibling();
        _trail.Follow(_view.RectTransform);

        _flyingViews.Add(_view);
        _view.FlyAlongArcTo(GetPileLocalPosition(_pile), _pileCardScale, _arcHeightRatio, _pileFlightDuration, _startTime, () =>
        {
            _trail.StopFollowing();
            LandInPile(_view);
        });
    }

    private void UpdateArrivals()
    {
        if (_arrivingViews.Count == 0)
            return;

        CollectOrderedHandViews();

        foreach (KeyValuePair<Card, HandArrival> _pair in _arrivingViews)
        {
            HandArrival _arrival = _pair.Value;
            if (_arrival.HasStarted || Time.time < _arrival.StartTime)
                continue;

            RectTransform _rectTransform = _pair.Key.RectTransform;
            _arrival.HasStarted = true;
            _arrival.StartTime = Time.time;
            _arrival.StartPosition = _rectTransform.anchoredPosition;
            _arrival.StartAngle = _rectTransform.localEulerAngles.z;
            _arrival.StartScale = _rectTransform.localScale.x;
            ComputeHandPose(_pair.Key, out _arrival.SmoothedSlotPosition, out _arrival.SmoothedSlotAngle, out _arrival.SmoothedSlotScale);
            _rectTransform.SetAsLastSibling();
        }

        float _slotSmoothing = _layoutDuration > 0f ? 1f - Mathf.Exp(-4f * Time.deltaTime / _layoutDuration) : 1f;

        _landedViews.Clear();
        foreach (KeyValuePair<Card, HandArrival> _pair in _arrivingViews)
        {
            HandArrival _arrival = _pair.Value;
            if (!_arrival.HasStarted)
                continue;

            float _progress = _flightDuration > 0f ? Mathf.Clamp01((Time.time - _arrival.StartTime) / _flightDuration) : 1f;
            float _easedProgress = 1f - (1f - _progress) * (1f - _progress);

            ComputeHandPose(_pair.Key, out Vector2 _slotPosition, out float _slotAngle, out float _slotScale);
            _arrival.SmoothedSlotPosition = Vector2.Lerp(_arrival.SmoothedSlotPosition, _slotPosition, _slotSmoothing);
            _arrival.SmoothedSlotAngle = Mathf.LerpAngle(_arrival.SmoothedSlotAngle, _slotAngle, _slotSmoothing);
            _arrival.SmoothedSlotScale = Mathf.Lerp(_arrival.SmoothedSlotScale, _slotScale, _slotSmoothing);

            _pair.Key.SnapTo(
                Vector2.Lerp(_arrival.StartPosition, _arrival.SmoothedSlotPosition, _easedProgress),
                Mathf.LerpAngle(_arrival.StartAngle, _arrival.SmoothedSlotAngle, _easedProgress),
                Mathf.Lerp(_arrival.StartScale, _arrival.SmoothedSlotScale, _easedProgress));

            if (_progress >= 1f)
                _landedViews.Add(_pair.Key);
        }

        foreach (Card _view in _landedViews)
            LandInHand(_view);
    }

    private void LandInHand(Card _view)
    {
        _arrivingViews.Remove(_view);
        _view.SetBlocksRaycasts(true);
        RefreshHandLayout();
    }

    private void LandInPile(Card _view)
    {
        _flyingViews.Remove(_view);
        DespawnCardView(_view);
    }

    private void DespawnUnclaimedDetachedViews()
    {
        if (_detachedViews.Count == 0)
            return;

        foreach (Card _view in _detachedViews.Values)
        {
            _view.SetBlocksRaycasts(false);
            _flyingViews.Add(_view);
            _view.FlyTo(_view.RectTransform.anchoredPosition, _view.RectTransform.localEulerAngles.z, 0f, _flightDuration, Time.time, () => LandInPile(_view));
        }

        _detachedViews.Clear();
    }

    private float ScheduleTransfer(float _transferInterval)
    {
        float _startTime = Mathf.Max(Time.time, _nextTransferStartTime);
        _nextTransferStartTime = _startTime + _transferInterval;

        return _startTime;
    }

    private Card SpawnCardView(CardInstance _card, Vector2 _localPosition, float _scale, bool _isFaceUp)
    {
        Card _view = LeanPool.Spawn(_cardPrefab, _cardContainer);
        _view.Initialize(_card, false);
        _view.SetFaceUp(_isFaceUp);
        _view.SnapTo(_localPosition, 0f, _scale);
        _view.SetBlocksRaycasts(true);
        _view.SetPlayable(false);
        _view.SetAffordable(true);
        _view.onPressed += HandleCardViewPressed;
        _view.onHoverChanged += HandleCardViewHoverChanged;

        return _view;
    }

    private void DespawnCardView(Card _view)
    {
        _view.onPressed -= HandleCardViewPressed;
        _view.onHoverChanged -= HandleCardViewHoverChanged;
        LeanPool.Despawn(_view);
    }

    private bool IsSettledInHand(Card _view)
    {
        return _handViews.ContainsValue(_view) && !_arrivingViews.ContainsKey(_view);
    }

    private void HandleCardViewPressed(Card _view)
    {
        if (!_isInteractable || _selectedCard != null || _draggedView != null || !IsSettledInHand(_view))
            return;

        _draggedView = _view;
        _hoveredView = null;
        _isDragButtonHeld = true;
        _pressScreenPosition = Mouse.current.position.ReadValue();
        RefreshHandLayout();
    }

    private void HandleCardViewHoverChanged(Card _view, bool _isHovered)
    {
        if (!_isInteractable || _selectedCard != null || _draggedView != null || !IsSettledInHand(_view))
            return;

        if (_isHovered)
            _hoveredView = _view;
        else if (_hoveredView == _view)
            _hoveredView = null;

        RefreshHandLayout();
    }

    private void CollectOrderedHandViews()
    {
        _orderedHandViews.Clear();
        foreach (CardInstance _card in _deck.Hand.Cards)
        {
            if (_card != _selectedCard && _handViews.TryGetValue(_card, out Card _view))
                _orderedHandViews.Add(_view);
        }
    }

    private void ComputeHandPose(Card _view, out Vector2 _position, out float _angle, out float _scale)
    {
        int _count = _orderedHandViews.Count;
        int _index = _orderedHandViews.IndexOf(_view);
        int _hoveredIndex = _orderedHandViews.IndexOf(_hoveredView);
        float _spacing = _count > 1 ? Mathf.Min(_cardSpacing, _maxHandWidth / (_count - 1)) : 0f;

        float _offsetFromCenter = _index - (_count - 1) * 0.5f;
        float _normalizedOffset = _count > 1 ? _offsetFromCenter / ((_count - 1) * 0.5f) : 0f;

        _position = new Vector2(_offsetFromCenter * _spacing, -_normalizedOffset * _normalizedOffset * _fanHeight);
        _angle = -_normalizedOffset * _maxFanAngle;
        _scale = 1f;

        if (_index == _hoveredIndex)
        {
            _position.y = _hoverHeight;
            _angle = 0f;
            _scale = _hoverScale;
        }
        else if (_hoveredIndex >= 0)
        {
            int _distanceToHovered = _index - _hoveredIndex;
            _position.x += Mathf.Sign(_distanceToHovered) * _hoverNeighbourPush / Mathf.Abs(_distanceToHovered);
        }
    }

    private void RefreshHandLayout()
    {
        if (_deck == null)
            return;

        CollectOrderedHandViews();

        for (int i = 0; i < _orderedHandViews.Count; i++)
        {
            Card _view = _orderedHandViews[i];
            bool _isArriving = _arrivingViews.ContainsKey(_view);

            if (!_isArriving)
                _view.RectTransform.SetSiblingIndex(i);

            if (_isArriving || _view == _draggedView)
                continue;

            ComputeHandPose(_view, out Vector2 _position, out float _angle, out float _scale);
            _view.MoveTo(_position, _angle, _scale, _layoutDuration);
        }

        foreach (KeyValuePair<Card, HandArrival> _pair in _arrivingViews)
        {
            if (_pair.Value.HasStarted)
                _pair.Key.RectTransform.SetAsLastSibling();
        }

        if (_hoveredView != null)
            _hoveredView.RectTransform.SetAsLastSibling();

        if (_draggedView != null)
            _draggedView.RectTransform.SetAsLastSibling();

        if (_selectedCard != null && _handViews.TryGetValue(_selectedCard, out Card _selectedView))
        {
            _selectedView.RectTransform.SetAsLastSibling();
            _selectedView.MoveTo(_selectedPosition, 0f, _selectedScale, _layoutDuration);
        }

        RefreshHotkeyLabels();
        RefreshPlayableHighlights();
    }

    private void RefreshPlayableHighlights()
    {
        foreach (Card _view in _handViews.Values)
        {
            bool _isAffordable = CanAfford(_view.Instance);
            _view.SetPlayable(_isInteractable && _isAffordable);
            _view.SetAffordable(_isAffordable);
        }
    }

    private void RefreshHotkeyLabels()
    {
        for (int i = 0; i < _deck.Hand.Count; i++)
        {
            if (!_handViews.TryGetValue(_deck.Hand.Cards[i], out Card _view))
                continue;

            bool _showsHotkey = _showHotkeyLabels && i < HotkeyDigitKeys.Length && !_arrivingViews.ContainsKey(_view) && _view.Instance != _selectedCard && _view != _draggedView;
            _view.SetHotkey(_showsHotkey ? ((i + 1) % 10).ToString(CultureInfo.InvariantCulture) : string.Empty);
        }
    }

    private void UpdateTargetingArrow()
    {
        if (_selectedCard == null || !_handViews.TryGetValue(_selectedCard, out Card _selectedView))
        {
            _targetingArrow.Hide();
            return;
        }

        float _halfCardHeight = _selectedView.RectTransform.rect.height * _selectedScale * 0.5f;
        Vector2 _arrowStart = _selectedPosition + Vector2.up * _halfCardHeight;

        _targetingArrow.Show(_arrowStart, GetPointerLocalPosition(), _isArrowOnValidTarget);
        _targetingArrow.transform.SetAsLastSibling();
    }

    private void RefreshPileCounts()
    {
        _drawPileCountLabel.text = _deck.DrawPile.Count.ToString(CultureInfo.InvariantCulture);
        _discardPileCountLabel.text = _deck.DiscardPile.Count.ToString(CultureInfo.InvariantCulture);
        _exhaustPileCountLabel.text = _deck.ExhaustPile.Count.ToString(CultureInfo.InvariantCulture);
    }

    private Vector2 GetPointerLocalPosition()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_cardContainer, Mouse.current.position.ReadValue(), EventCamera, out Vector2 _localPosition);
        return _localPosition;
    }

    private Vector2 GetPileLocalPosition(CardPile _pile)
    {
        return _cardContainer.InverseTransformPoint(GetPileAnchor(_pile).position);
    }

    private RectTransform GetPileAnchor(CardPile _pile)
    {
        if (_pile == _deck.DrawPile)
            return _drawPileAnchor;
        if (_pile == _deck.DiscardPile)
            return _discardPileAnchor;
        if (_pile == _deck.ExhaustPile)
            return _exhaustPileAnchor;

        return _cardContainer;
    }

    private IEnumerable<CardPile> EnumeratePiles()
    {
        yield return _deck.DrawPile;
        yield return _deck.Hand;
        yield return _deck.DiscardPile;
        yield return _deck.ExhaustPile;
    }
}
