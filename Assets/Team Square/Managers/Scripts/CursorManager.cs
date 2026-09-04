using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : Singleton<CursorManager>
{
    [SerializeField] private Texture2D m_normalTexture;
    [SerializeField] private Vector2 m_normalHotspot = new(3f, 3f);
    [SerializeField] private Texture2D m_combatTexture;
    [SerializeField] private Vector2 m_combatHotspot = new(24f, 25f);
    [SerializeField] private Texture2D m_handTexture;
    [SerializeField] private Vector2 m_handHotspot = new(19f, 4f);
    [SerializeField] private LayerMask m_hoverLayerMask = ~0;
    [SerializeField] private float m_hoverDistance = 500f;

    private readonly List<CursorTarget> m_hoveredUITargets = new();
    private CursorType m_currentType = CursorType.Normal;
    private bool m_applied;

    public CursorType CurrentType => m_currentType;

    private void Start()
    {
        ApplyCursor(CursorType.Normal, true);
    }

    private void Update()
    {
        ApplyCursor(ResolveHoveredType(), false);
    }

    public void RegisterUITarget(CursorTarget _target)
    {
        if (_target == null || m_hoveredUITargets.Contains(_target))
            return;

        m_hoveredUITargets.Add(_target);
    }

    public void UnregisterUITarget(CursorTarget _target)
    {
        m_hoveredUITargets.Remove(_target);
    }

    private CursorType ResolveHoveredType()
    {
        for (int _i = m_hoveredUITargets.Count - 1; _i >= 0; _i--)
        {
            CursorTarget _target = m_hoveredUITargets[_i];

            if (_target == null || !_target.isActiveAndEnabled)
            {
                m_hoveredUITargets.RemoveAt(_i);
                continue;
            }

            return _target.CursorType;
        }

        return ResolveWorldHoveredType();
    }

    private CursorType ResolveWorldHoveredType()
    {
        if (Mouse.current == null)
            return CursorType.Normal;

        Camera _camera = CameraManager.Instance != null ? CameraManager.Instance.MainCam : Camera.main;
        if (_camera == null)
            return CursorType.Normal;

        Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(_ray, out RaycastHit _hit, m_hoverDistance, m_hoverLayerMask))
            return CursorType.Normal;

        CursorTarget _target = _hit.collider.GetComponentInParent<CursorTarget>();
        return _target != null ? _target.CursorType : CursorType.Normal;
    }

    private void ApplyCursor(CursorType _type, bool _force)
    {
        if (m_applied && !_force && _type == m_currentType)
            return;

        m_currentType = _type;
        m_applied = true;

        Cursor.SetCursor(GetTexture(_type), GetHotspot(_type), CursorMode.ForceSoftware);
    }

    private Texture2D GetTexture(CursorType _type)
    {
        return _type switch
        {
            CursorType.Combat => m_combatTexture,
            CursorType.Hand => m_handTexture,
            _ => m_normalTexture
        };
    }

    private Vector2 GetHotspot(CursorType _type)
    {
        return _type switch
        {
            CursorType.Combat => m_combatHotspot,
            CursorType.Hand => m_handHotspot,
            _ => m_normalHotspot
        };
    }
}
