using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PanelController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [TitleGroup("Dependencies")]
    [SerializeField, Required] private RectTransform m_contentRect;

    [TitleGroup("Pan Settings")]
    [SerializeField] private float m_panSpeed = 1f;
    [SerializeField] private Vector2 m_basePanLimits = new Vector2(2000f, 1500f);

    [TitleGroup("Zoom Settings")]
    [SerializeField] private float m_scrollSpeed = 0.1f;
    [SerializeField] private Vector2 m_minMaxScroll = new Vector2(0.5f, 2f);

    [TitleGroup("Variables")]
    [SerializeField] private bool m_isControlling = false;
    private bool m_isPanning = false;
    private Vector2 m_lastMousePosition;

    private void Update()
    {
        if (!m_isControlling) return;
        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            m_isPanning = true;
            m_lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
            m_isPanning = false;

        // Pan avec la molette enfoncée
        if (m_isPanning)
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 delta = currentMousePosition - m_lastMousePosition;
            Vector2 newPosition = m_contentRect.anchoredPosition + delta * m_panSpeed;
            
            float currentScale = m_contentRect.transform.localScale.x;
            Vector2 adjustedLimits = GetAdjustedPanLimits(currentScale);
            
            newPosition.x = Mathf.Clamp(newPosition.x, -adjustedLimits.x, adjustedLimits.x);
            newPosition.y = Mathf.Clamp(newPosition.y, -adjustedLimits.y, adjustedLimits.y);

            m_contentRect.anchoredPosition = newPosition;
            m_lastMousePosition = currentMousePosition;
        }
    }

    private Vector2 GetAdjustedPanLimits(float scale)
    {
        return m_basePanLimits * scale;
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y / 120f;

        if (scroll != 0f)
        {
            Vector3 currentScale = m_contentRect.transform.localScale;
            float newScale = currentScale.x + scroll * m_scrollSpeed;
            
            newScale = Mathf.Clamp(newScale, m_minMaxScroll.x, m_minMaxScroll.y);

            m_contentRect.transform.localScale = Vector3.one * newScale;
            
            Vector2 adjustedLimits = GetAdjustedPanLimits(newScale);
            Vector2 clampedPosition = m_contentRect.anchoredPosition;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, -adjustedLimits.x, adjustedLimits.x);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, -adjustedLimits.y, adjustedLimits.y);
            m_contentRect.anchoredPosition = clampedPosition;
        }
    }

    public void CenterOnNode(RectTransform nodeRect)
    {
        if (nodeRect == null) return;
        
        Vector3 nodeScreenPos = nodeRect.position;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 offset = new Vector2(nodeScreenPos.x - screenCenter.x, nodeScreenPos.y - screenCenter.y);
        Vector2 newPosition = m_contentRect.anchoredPosition - offset;
        
        m_contentRect.anchoredPosition = newPosition;
    }

    // Optionnel : Support du drag via Event System
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            m_isPanning = true;
            m_lastMousePosition = Mouse.current.position.ReadValue();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            m_isPanning = false;
        }
    }

    // Méthode pour réinitialiser la vue
    [ContextMenu("Reset View")]
    public void ResetView()
    {
        m_contentRect.anchoredPosition = Vector2.zero;
        m_contentRect.transform.localScale = Vector3.one;
    }
}