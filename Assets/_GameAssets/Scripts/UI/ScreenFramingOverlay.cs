using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ScreenFramingOverlay : MaskableGraphic
{
    [SerializeField] private bool _showCenterCross = true;
    [SerializeField] private bool _showReservedZones = true;
    [SerializeField, Range(0f, 0.45f)] private float _sidesRatio = 0.12f;
    [SerializeField, Range(0f, 0.45f)] private float _topRatio = 0.09f;
    [SerializeField, Range(0f, 0.45f)] private float _bottomRatio = 0.18f;
    [SerializeField] private Color _crossColor = new(1f, 0.65f, 0f, 0.9f);
    [SerializeField, Min(1f)] private float _crossThickness = 3f;
    [SerializeField] private Color _zoneColor = new(0.85f, 0.12f, 0.15f, 0.18f);
    [SerializeField] private Color _zoneBorderColor = new(0.92f, 0.18f, 0.22f, 0.95f);
    [SerializeField, Min(0f)] private float _zoneBorderThickness = 2f;

    public Rect SafeAreaViewport => new(_sidesRatio, _bottomRatio, 1f - _sidesRatio * 2f, 1f - _topRatio - _bottomRatio);

    protected override void OnEnable()
    {
        base.OnEnable();

        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper _vertexHelper)
    {
        _vertexHelper.Clear();

        if (!Application.isEditor)
            return;

        Rect _rect = GetPixelAdjustedRect();

        if (_showReservedZones)
            AppendReservedZones(_vertexHelper, _rect);

        if (_showCenterCross)
            AppendCenterCross(_vertexHelper, _rect);
    }

    private void AppendReservedZones(VertexHelper _vertexHelper, Rect _rect)
    {
        float _left = _rect.xMin + _rect.width * _sidesRatio;
        float _right = _rect.xMax - _rect.width * _sidesRatio;
        float _lower = _rect.yMin + _rect.height * _bottomRatio;
        float _upper = _rect.yMax - _rect.height * _topRatio;

        Color _fill = _zoneColor * color;

        AppendQuad(_vertexHelper, _rect.xMin, _rect.yMin, _left, _rect.yMax, _fill);
        AppendQuad(_vertexHelper, _right, _rect.yMin, _rect.xMax, _rect.yMax, _fill);
        AppendQuad(_vertexHelper, _left, _upper, _right, _rect.yMax, _fill);
        AppendQuad(_vertexHelper, _left, _rect.yMin, _right, _lower, _fill);

        if (_zoneBorderThickness <= 0f)
            return;

        Color _border = _zoneBorderColor * color;

        AppendQuad(_vertexHelper, _left, _lower, _left + _zoneBorderThickness, _upper, _border);
        AppendQuad(_vertexHelper, _right - _zoneBorderThickness, _lower, _right, _upper, _border);
        AppendQuad(_vertexHelper, _left, _upper - _zoneBorderThickness, _right, _upper, _border);
        AppendQuad(_vertexHelper, _left, _lower, _right, _lower + _zoneBorderThickness, _border);
    }

    private void AppendCenterCross(VertexHelper _vertexHelper, Rect _rect)
    {
        Color _color = _crossColor * color;
        float _half = _crossThickness * 0.5f;
        Vector2 _center = _rect.center;

        AppendQuad(_vertexHelper, _center.x - _half, _rect.yMin, _center.x + _half, _rect.yMax, _color);
        AppendQuad(_vertexHelper, _rect.xMin, _center.y - _half, _rect.xMax, _center.y + _half, _color);
    }

    private static void AppendQuad(VertexHelper _vertexHelper, float _xMin, float _yMin, float _xMax, float _yMax, Color _color)
    {
        if (_xMax <= _xMin || _yMax <= _yMin || _color.a <= 0f)
            return;

        int _firstVertex = _vertexHelper.currentVertCount;
        UIVertex _vertex = UIVertex.simpleVert;
        _vertex.color = _color;

        _vertex.position = new Vector3(_xMin, _yMin);
        _vertexHelper.AddVert(_vertex);
        _vertex.position = new Vector3(_xMin, _yMax);
        _vertexHelper.AddVert(_vertex);
        _vertex.position = new Vector3(_xMax, _yMax);
        _vertexHelper.AddVert(_vertex);
        _vertex.position = new Vector3(_xMax, _yMin);
        _vertexHelper.AddVert(_vertex);

        _vertexHelper.AddTriangle(_firstVertex, _firstVertex + 1, _firstVertex + 2);
        _vertexHelper.AddTriangle(_firstVertex, _firstVertex + 2, _firstVertex + 3);
    }
}
