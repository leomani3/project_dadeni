using UnityEngine;
using UnityEngine.EventSystems;

public class CursorTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CursorType m_cursorType = CursorType.Hand;

    public CursorType CursorType => m_cursorType;

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.RegisterUITarget(this);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.UnregisterUITarget(this);
    }

    private void OnDisable()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.UnregisterUITarget(this);
    }
}
