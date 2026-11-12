using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public class CardTrail : MaskableGraphic
{
    private struct TrailPoint
    {
        public Vector2 Position;
        public float Time;
    }

    [SerializeField, Min(0.01f)] private float _lifetime = 0.45f;
    [SerializeField, Min(0f)] private float _headWidth = 22f;
    [SerializeField, Min(1f)] private float _glowWidthMultiplier = 2.4f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.35f;
    [SerializeField, Min(0f)] private float _minPointDistance = 3f;
    [SerializeField, Min(0f)] private float _headTaperLength = 14f;
    [SerializeField] private Gradient _colorAlongTrail = CreateDefaultGradient();

    private readonly List<TrailPoint> _points = new List<TrailPoint>();
    private readonly List<float> _distancesFromHead = new List<float>();
    private RectTransform _target;

    public void Follow(RectTransform _followedTarget)
    {
        _target = _followedTarget;
        _points.Clear();
        AddTargetPoint();
        SetVerticesDirty();
    }

    public void StopFollowing()
    {
        _target = null;
    }

    private void LateUpdate()
    {
        if (_target != null && !_target.gameObject.activeInHierarchy)
            StopFollowing();

        if (_target != null)
            AddTargetPoint();

        while (_points.Count > 0 && Time.time - _points[0].Time > _lifetime)
            _points.RemoveAt(0);

        SetVerticesDirty();

        if (_target == null && _points.Count == 0)
            LeanPool.Despawn(this);
    }

    private void AddTargetPoint()
    {
        Vector2 _position = rectTransform.InverseTransformPoint(_target.position);
        TrailPoint _point = new TrailPoint { Position = _position, Time = Time.time };

        if (_points.Count > 0 && Vector2.Distance(_points[_points.Count - 1].Position, _position) < _minPointDistance)
        {
            _points[_points.Count - 1] = new TrailPoint { Position = _position, Time = _points[_points.Count - 1].Time };
            return;
        }

        _points.Add(_point);
    }

    protected override void OnPopulateMesh(VertexHelper _vertexHelper)
    {
        _vertexHelper.Clear();

        if (_points.Count < 2)
            return;

        ComputeDistancesFromHead();
        AddRibbon(_vertexHelper, _headWidth * _glowWidthMultiplier, _glowAlpha);
        AddRibbon(_vertexHelper, _headWidth, 1f);
    }

    private void AddRibbon(VertexHelper _vertexHelper, float _width, float _alpha)
    {
        int _firstVertex = _vertexHelper.currentVertCount;

        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 _previous = _points[Mathf.Max(i - 1, 0)].Position;
            Vector2 _next = _points[Mathf.Min(i + 1, _points.Count - 1)].Position;
            Vector2 _direction = (_next - _previous).sqrMagnitude > 0.0001f ? (_next - _previous).normalized : Vector2.up;
            Vector2 _normal = new Vector2(-_direction.y, _direction.x);

            float _age = Mathf.Clamp01((Time.time - _points[i].Time) / _lifetime);
            float _positionFromHead = i == _points.Count - 1 ? 0f : 1f - (float)i / (_points.Count - 1);
            float _tailTaper = 1f - Mathf.Max(_age, _positionFromHead);
            float _headTaper = _headTaperLength > 0f ? Mathf.Sqrt(Mathf.Clamp01(_distancesFromHead[i] / _headTaperLength)) : 1f;
            float _halfWidth = _width * 0.5f * _tailTaper * _headTaper;
            Color _color = _colorAlongTrail.Evaluate(_age) * color;
            _color.a *= _alpha * _tailTaper;

            _vertexHelper.AddVert(_points[i].Position + _normal * _halfWidth, _color, Vector2.zero);
            _vertexHelper.AddVert(_points[i].Position - _normal * _halfWidth, _color, Vector2.zero);

            if (i == 0)
                continue;

            int _vertex = _firstVertex + i * 2;
            _vertexHelper.AddTriangle(_vertex - 2, _vertex - 1, _vertex);
            _vertexHelper.AddTriangle(_vertex - 1, _vertex + 1, _vertex);
        }
    }

    private void ComputeDistancesFromHead()
    {
        _distancesFromHead.Clear();
        for (int i = 0; i < _points.Count; i++)
            _distancesFromHead.Add(0f);

        for (int i = _points.Count - 2; i >= 0; i--)
            _distancesFromHead[i] = _distancesFromHead[i + 1] + Vector2.Distance(_points[i].Position, _points[i + 1].Position);
    }

    private static Gradient CreateDefaultGradient()
    {
        Gradient _gradient = new Gradient();
        _gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.88f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.25f), new GradientColorKey(new Color(0.7f, 0.1f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });

        return _gradient;
    }
}
