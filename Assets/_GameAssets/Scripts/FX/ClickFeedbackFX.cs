using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class ClickFeedbackFX : MonoBehaviour
{
    private const int RingSegmentCount = 48;
    private const int ChevronPointCount = 3;

    private static Material s_lineMaterial;

    private LineRenderer m_ring;
    private LineRenderer m_pulseRing;
    private LineRenderer[] m_chevrons;
    private ClickFeedbackSettings m_settings;
    private Tween m_tween;

    public bool IsPlaying => gameObject.activeSelf;

    public void Play(Vector3 _worldPosition, ClickFeedbackSettings _settings)
    {
        m_settings = _settings;

        transform.SetPositionAndRotation(_worldPosition, Quaternion.Euler(90f, 0f, 0f));
        gameObject.SetActive(true);

        BuildRenderers();
        KillTween();
        ApplyProgress(0f);

        m_tween = DOTween.To(() => 0f, ApplyProgress, 1f, Mathf.Max(0.01f, m_settings.duration))
            .SetEase(m_settings.ease)
            .SetUpdate(true)
            .OnComplete(Stop);
    }

    public void Stop()
    {
        m_tween = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void KillTween()
    {
        if (m_tween != null && m_tween.IsActive())
            m_tween.Kill();

        m_tween = null;
    }

    private void BuildRenderers()
    {
        if (s_lineMaterial == null)
        {
            s_lineMaterial = new Material(Shader.Find("Sprites/Default"));
            s_lineMaterial.renderQueue = (int)RenderQueue.Transparent;
        }

        if (m_ring == null)
            m_ring = CreateLine("Ring", RingSegmentCount, true);

        if (m_pulseRing == null)
            m_pulseRing = CreateLine("PulseRing", RingSegmentCount, true);

        int _wantedChevronCount = m_settings.useChevrons ? Mathf.Max(0, m_settings.chevronCount) : 0;

        if (m_chevrons != null && m_chevrons.Length == _wantedChevronCount)
            return;

        if (m_chevrons != null)
        {
            foreach (LineRenderer _chevron in m_chevrons)
                Destroy(_chevron.gameObject);
        }

        m_chevrons = new LineRenderer[_wantedChevronCount];

        for (int i = 0; i < _wantedChevronCount; i++)
            m_chevrons[i] = CreateLine($"Chevron_{i}", ChevronPointCount, false);
    }

    private LineRenderer CreateLine(string _name, int _pointCount, bool _loop)
    {
        GameObject _lineObject = new(_name);
        _lineObject.transform.SetParent(transform, false);

        LineRenderer _line = _lineObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = _loop;
        _line.positionCount = _pointCount;
        _line.alignment = LineAlignment.TransformZ;
        _line.textureMode = LineTextureMode.Stretch;
        _line.numCapVertices = 2;
        _line.numCornerVertices = 2;
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.lightProbeUsage = LightProbeUsage.Off;
        _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _line.sharedMaterial = s_lineMaterial;

        return _line;
    }

    private void ApplyProgress(float _progress)
    {
        float _alpha = GetAlpha(_progress);

        Color _ringColor = m_settings.color;
        _ringColor.a *= _alpha;

        float _ringRadius = Mathf.Lerp(m_settings.startRadius, m_settings.endRadius, _progress);
        UpdateCircle(m_ring, _ringRadius, _ringColor, m_settings.lineWidth);

        m_pulseRing.enabled = m_settings.usePulseRing;

        if (m_settings.usePulseRing)
        {
            Color _pulseColor = m_settings.color;
            _pulseColor.a *= _alpha * m_settings.pulseAlphaScale;

            float _pulseRadius = Mathf.Lerp(m_settings.endRadius, m_settings.startRadius * m_settings.pulseEndRadiusScale, _progress);
            UpdateCircle(m_pulseRing, _pulseRadius, _pulseColor, m_settings.lineWidth * m_settings.pulseWidthScale);
        }

        float _chevronRadius = Mathf.Lerp(m_settings.startRadius * m_settings.chevronStartRadiusScale, m_settings.endRadius * m_settings.chevronEndRadiusScale, _progress);
        UpdateChevrons(_chevronRadius, _ringColor);
    }

    private float GetAlpha(float _progress)
    {
        float _opaqueRatio = Mathf.Clamp01(m_settings.opaqueRatio);

        if (_progress <= _opaqueRatio)
            return 1f;

        return 1f - (_progress - _opaqueRatio) / Mathf.Max(0.0001f, 1f - _opaqueRatio);
    }

    private void UpdateCircle(LineRenderer _line, float _radius, Color _color, float _width)
    {
        for (int i = 0; i < RingSegmentCount; i++)
        {
            float _angle = i / (float)RingSegmentCount * Mathf.PI * 2f;
            _line.SetPosition(i, new Vector3(Mathf.Cos(_angle) * _radius, Mathf.Sin(_angle) * _radius, 0f));
        }

        ApplyLineStyle(_line, _color, _width);
    }

    private void UpdateChevrons(float _radius, Color _color)
    {
        if (m_chevrons == null)
            return;

        float _angleOffset = m_settings.chevronAngleOffset * Mathf.Deg2Rad;

        for (int i = 0; i < m_chevrons.Length; i++)
        {
            float _angle = i / (float)m_chevrons.Length * Mathf.PI * 2f + _angleOffset;

            Vector3 _outward = new(Mathf.Cos(_angle), Mathf.Sin(_angle), 0f);
            Vector3 _side = new(-_outward.y, _outward.x, 0f);

            LineRenderer _chevron = m_chevrons[i];
            _chevron.SetPosition(0, _outward * _radius + _side * m_settings.chevronSize);
            _chevron.SetPosition(1, _outward * (_radius - m_settings.chevronSize));
            _chevron.SetPosition(2, _outward * _radius - _side * m_settings.chevronSize);

            ApplyLineStyle(_chevron, _color, m_settings.lineWidth);
        }
    }

    private void ApplyLineStyle(LineRenderer _line, Color _color, float _width)
    {
        _line.startColor = _color;
        _line.endColor = _color;
        _line.startWidth = _width;
        _line.endWidth = _width;
    }
}
