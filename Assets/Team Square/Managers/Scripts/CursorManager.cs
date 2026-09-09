using MyBox;
using UnityEngine;

public class CursorManager : Singleton<CursorManager>
{
    [SerializeField] private Texture2D m_normalTexture;
    [SerializeField] private Vector2 m_normalHotspot = new(3f, 3f);
    [SerializeField] private Texture2D m_combatTexture;
    [SerializeField] private Vector2 m_combatHotspot = new(24f, 25f);
    [SerializeField] private Texture2D m_handTexture;
    [SerializeField] private Vector2 m_handHotspot = new(19f, 4f);

    private CursorType? m_appliedType;

    private void Start()
    {
        ApplyCursor(CursorType.Normal);
    }

    public void ApplyCursor(CursorType _type)
    {
        if (m_appliedType == _type)
            return;

        m_appliedType = _type;
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
