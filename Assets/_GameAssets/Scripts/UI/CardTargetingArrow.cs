using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CardTargetingArrow : MonoBehaviour
{
    [SerializeField, Required] private RectTransform _rectTransform;
    [SerializeField, Required] private Image _head;
    [SerializeField, Required] private Image _segmentTemplate;
    [SerializeField, Min(2)] private int _segmentCount = 14;
    [SerializeField] private float _headTipOffset;
    [SerializeField, Range(0f, 1f)] private float _curveBend = 0.75f;
    [SerializeField] private Vector2 _segmentScaleRange = new Vector2(0.45f, 1f);
    [SerializeField] private Color _idleColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color _validTargetColor = new Color(0.9f, 0.2f, 0.2f);

    private readonly List<Image> _segments = new List<Image>();

    private void Reset()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Show(Vector2 _fromLocalPosition, Vector2 _toLocalPosition, bool _isOnValidTarget)
    {
        EnsureSegments();
        gameObject.SetActive(true);

        Vector2 _controlPoint = ComputeControlPoint(_fromLocalPosition, _toLocalPosition);
        Color _color = _isOnValidTarget ? _validTargetColor : _idleColor;

        for (int i = 0; i < _segments.Count; i++)
        {
            float _t = (float)i / _segments.Count;
            PlaceAlongCurve(_segments[i].rectTransform, _fromLocalPosition, _controlPoint, _toLocalPosition, _t);
            _segments[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(_segmentScaleRange.x, _segmentScaleRange.y, _t);
            _segments[i].color = _color;
        }

        PlaceAlongCurve(_head.rectTransform, _fromLocalPosition, _controlPoint, _toLocalPosition, 1f, _headTipOffset);
        _head.color = _color;
        _head.rectTransform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void EnsureSegments()
    {
        if (_segments.Count == _segmentCount)
            return;

        _segmentTemplate.gameObject.SetActive(false);

        while (_segments.Count < _segmentCount)
        {
            Image _segment = Instantiate(_segmentTemplate, _rectTransform);
            _segment.gameObject.SetActive(true);
            _segments.Add(_segment);
        }
    }

    private Vector2 ComputeControlPoint(Vector2 _from, Vector2 _to)
    {
        Vector2 _midPoint = (_from + _to) * 0.5f;
        Vector2 _cornerAboveStart = new Vector2(_from.x, _to.y);

        return Vector2.Lerp(_midPoint, _cornerAboveStart, _curveBend);
    }

    private static void PlaceAlongCurve(RectTransform _element, Vector2 _from, Vector2 _control, Vector2 _to, float _t, float _forwardOffset = 0f)
    {
        Vector2 _position = EvaluateBezier(_from, _control, _to, _t);
        Vector2 _forward = EvaluateBezierTangent(_from, _control, _to, _t).normalized;

        _element.anchoredPosition = _position - _forward * _forwardOffset;
        _element.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_forward.y, _forward.x) * Mathf.Rad2Deg);
    }

    private static Vector2 EvaluateBezier(Vector2 _from, Vector2 _control, Vector2 _to, float _t)
    {
        float _inverse = 1f - _t;
        return _inverse * _inverse * _from + 2f * _inverse * _t * _control + _t * _t * _to;
    }

    private static Vector2 EvaluateBezierTangent(Vector2 _from, Vector2 _control, Vector2 _to, float _t)
    {
        Vector2 _tangent = 2f * (1f - _t) * (_control - _from) + 2f * _t * (_to - _control);
        return _tangent.sqrMagnitude > 0.0001f ? _tangent : Vector2.up;
    }
}
