using System;
using System.Collections.Generic;
using System.Globalization;
using Deckbuilder.Cards.Actions;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Deckbuilder.Cards
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private const string UPGRADED_TITLE_SUFFIX = "+";
        private const string EXHAUST_KEYWORD = "Exhaust";

        public event Action<Card> onPressed;
        public event Action<Card, bool> onHoverChanged;

        [SerializeField, Required] private RectTransform _rectTransform;
        [SerializeField, Required] private CanvasGroup _canvasGroup;
        [SerializeField, Required] private RectTransform _flipPivot;
        [SerializeField, Required] private GameObject _front;
        [SerializeField, Required] private GameObject _back;
        [SerializeField, Required] private TMP_Text _titleLabel;
        [SerializeField, Required] private TMP_Text _costLabel;
        [SerializeField, Required] private TMP_Text _descriptionLabel;
        [SerializeField, Required] private TMP_Text _hotkeyLabel;
        [SerializeField] private Color _upgradeHighlightColor = new Color(0.36f, 0.9f, 0.36f);
        [SerializeField, Range(0f, 1f)] private float _flipPortionOfFlight = 0.3f;
        [SerializeField, Range(0.01f, 1f)] private float _shrinkPortionOfFlight = 0.35f;

        private bool _highlightUpgradeDifferences;
        private Sequence _motionTween;

        public CardInstance Instance { get; private set; }
        public RectTransform RectTransform => _rectTransform;
        public bool IsFaceUp { get; private set; } = true;

        private CardConfig Config => Instance.Config;

        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(CardInstance _instance, bool _highlightDifferences)
        {
            Instance = _instance;
            _highlightUpgradeDifferences = Instance.IsUpgraded && _highlightDifferences;

            _titleLabel.text = BuildTitle();
            _costLabel.text = HighlightIfChanged(FormatValue(Instance.Cost), FormatValue(Config.GetCost(false)));
            _descriptionLabel.text = BuildDescription();
            SetHotkey(string.Empty);
        }

        public void SetBlocksRaycasts(bool _blocksRaycasts)
        {
            _canvasGroup.blocksRaycasts = _blocksRaycasts;
        }

        public void SetHotkey(string _hotkey)
        {
            _hotkeyLabel.text = _hotkey;
            _hotkeyLabel.gameObject.SetActive(!string.IsNullOrEmpty(_hotkey));
        }

        public void SetFaceUp(bool _isFaceUp)
        {
            IsFaceUp = _isFaceUp;
            _front.SetActive(_isFaceUp);
            _back.SetActive(!_isFaceUp);
        }

        public void SnapTo(Vector2 _anchoredPosition, float _angle, float _scale)
        {
            _motionTween?.Kill();
            _flipPivot.localScale = Vector3.one;
            _rectTransform.anchoredPosition = _anchoredPosition;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, _angle);
            _rectTransform.localScale = Vector3.one * _scale;
        }

        public void MoveTo(Vector2 _anchoredPosition, float _angle, float _scale, float _duration)
        {
            _motionTween?.Kill();
            _flipPivot.localScale = Vector3.one;

            _motionTween = DOTween.Sequence()
                .Join(_rectTransform.DOAnchorPos(_anchoredPosition, _duration))
                .Join(_rectTransform.DOLocalRotate(new Vector3(0f, 0f, _angle), _duration))
                .Join(_rectTransform.DOScale(_scale, _duration))
                .SetEase(Ease.OutCubic);
        }

        public void FlyTo(Vector2 _anchoredPosition, float _endAngle, float _endScale, float _duration, float _startTime, Action _onComplete)
        {
            _motionTween?.Kill();
            _flipPivot.localScale = Vector3.one;

            _motionTween = DOTween.Sequence()
                .Insert(0f, _rectTransform.DOAnchorPos(_anchoredPosition, _duration).SetEase(Ease.OutQuad))
                .Insert(0f, _rectTransform.DOScale(_endScale, _duration).SetEase(Ease.OutQuad))
                .Insert(0f, _rectTransform.DOLocalRotate(new Vector3(0f, 0f, _endAngle), _duration).SetEase(Ease.OutQuad))
                .SetDelay(Mathf.Max(0f, _startTime - Time.time))
                .OnComplete(() => _onComplete?.Invoke());
        }

        public void FlyAlongArcTo(Vector2 _anchoredPosition, float _inFlightScale, float _arcHeightRatio, float _duration, float _startTime, Action _onComplete)
        {
            _motionTween?.Kill();
            _flipPivot.localScale = Vector3.one;

            Vector2 _startPosition = _rectTransform.anchoredPosition;
            Vector2 _controlPoint = ComputeArcControlPoint(_startPosition, _anchoredPosition, _arcHeightRatio);
            float _startScale = _rectTransform.localScale.x;

            _motionTween = DOTween.Sequence()
                .Insert(0f, DOVirtual.Float(0f, 1f, _duration, _progress => PlaceAlongArc(_startPosition, _controlPoint, _anchoredPosition, _startScale, _inFlightScale, _progress)).SetEase(Ease.InOutSine));

            if (IsFaceUp)
                InsertFlipToFaceDown(_motionTween, _duration * _flipPortionOfFlight * 0.5f);

            _motionTween
                .SetDelay(Mathf.Max(0f, _startTime - Time.time))
                .OnStart(() =>
                {
                    _startPosition = _rectTransform.anchoredPosition;
                    _startScale = _rectTransform.localScale.x;
                    _controlPoint = ComputeArcControlPoint(_startPosition, _anchoredPosition, _arcHeightRatio);
                })
                .OnComplete(() => _onComplete?.Invoke());
        }

        private void PlaceAlongArc(Vector2 _start, Vector2 _control, Vector2 _end, float _startScale, float _inFlightScale, float _progress)
        {
            float _inverse = 1f - _progress;
            Vector2 _position = _inverse * _inverse * _start + 2f * _inverse * _progress * _control + _progress * _progress * _end;
            Vector2 _tangent = 2f * _inverse * (_control - _start) + 2f * _progress * (_end - _control);

            float _shrinkProgress = Mathf.Clamp01(_progress / _shrinkPortionOfFlight);
            float _scale = Mathf.Lerp(_startScale, _inFlightScale, 1f - (1f - _shrinkProgress) * (1f - _shrinkProgress));

            _rectTransform.anchoredPosition = _position;
            _rectTransform.localScale = Vector3.one * _scale;

            if (_tangent.sqrMagnitude > 0.01f)
                _rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_tangent.y, _tangent.x) * Mathf.Rad2Deg - 90f);
        }

        private static Vector2 ComputeArcControlPoint(Vector2 _start, Vector2 _end, float _arcHeightRatio)
        {
            float _distance = Vector2.Distance(_start, _end);
            return new Vector2(Mathf.Lerp(_start.x, _end.x, 0.35f), Mathf.Max(_start.y, _end.y) + _distance * _arcHeightRatio);
        }

        public void OnPointerEnter(PointerEventData _eventData)
        {
            onHoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            onHoverChanged?.Invoke(this, false);
        }

        public void OnPointerDown(PointerEventData _eventData)
        {
            if (_eventData.button == PointerEventData.InputButton.Left)
                onPressed?.Invoke(this);
        }

        private void OnDisable()
        {
            _motionTween?.Kill();
        }

        private void InsertFlipToFaceDown(Sequence _sequence, float _halfFlipDuration)
        {
            _sequence.Insert(0f, _flipPivot.DOScaleX(0f, _halfFlipDuration).SetEase(Ease.InQuad));
            _sequence.InsertCallback(_halfFlipDuration, () => SetFaceUp(false));
            _sequence.Insert(_halfFlipDuration, _flipPivot.DOScaleX(1f, _halfFlipDuration).SetEase(Ease.OutQuad));
        }

        private string BuildTitle()
        {
            return Instance.IsUpgraded ? Config.Title + Highlight(UPGRADED_TITLE_SUFFIX) : Config.Title;
        }

        private string BuildDescription()
        {
            IReadOnlyList<CardAction> _displayedActions = Instance.Actions;
            IReadOnlyList<CardAction> _baseActions = Config.GetActions(false);
            List<string> _lines = new List<string>(_displayedActions.Count + 1);

            for (int i = 0; i < _displayedActions.Count; i++)
            {
                CardAction _comparedAction = i < _baseActions.Count ? _baseActions[i] : null;
                _lines.Add(BuildActionLine(_displayedActions[i], _comparedAction));
            }

            if (Instance.Exhausts)
                _lines.Add(Config.GetExhausts(false) ? EXHAUST_KEYWORD : Highlight(EXHAUST_KEYWORD));

            return string.Join("\n", _lines);
        }

        private string BuildActionLine(CardAction _action, CardAction _comparedAction)
        {
            object[] _values = _action.DescriptionValues;
            bool _isSameKindOfAction = _comparedAction != null && _comparedAction.GetType() == _action.GetType() && _comparedAction.DescriptionFormat == _action.DescriptionFormat;

            if (!_isSameKindOfAction)
                return Highlight(Format(_action.DescriptionFormat, _values));

            object[] _comparedValues = _comparedAction.DescriptionValues;
            object[] _displayedValues = new object[_values.Length];
            for (int i = 0; i < _values.Length; i++)
                _displayedValues[i] = HighlightIfChanged(FormatValue(_values[i]), FormatValue(_comparedValues[i]));

            return Format(_action.DescriptionFormat, _displayedValues);
        }

        private string HighlightIfChanged(string _displayedText, string _comparedText)
        {
            return _displayedText == _comparedText ? _displayedText : Highlight(_displayedText);
        }

        private string Highlight(string _text)
        {
            if (!_highlightUpgradeDifferences)
                return _text;

            return $"<color=#{ColorUtility.ToHtmlStringRGBA(_upgradeHighlightColor)}>{_text}</color>";
        }

        private static string Format(string _format, object[] _values)
        {
            return string.Format(CultureInfo.InvariantCulture, _format, _values);
        }

        private static string FormatValue(object _value)
        {
            return Convert.ToString(_value, CultureInfo.InvariantCulture);
        }

        [Button]
        private void DebugInitialize(CardConfig _config, bool _isUpgraded, bool _highlightDifferences)
        {
            Initialize(new CardInstance(_config, _isUpgraded), _highlightDifferences);
        }
    }
}
