using System;
using MyBox;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Utils;

public class UIManager : Singleton<UIManager>
{
    [SerializeField, ReadOnly] private SerializableDictionary<Type, CanvasHandler> m_canvases;
    [SerializeField] private CanvasHandler m_defaultCanvas;

    public bool IsOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private void Awake()
    {
        SetupCanvases();
        
        m_defaultCanvas.Open();
    }

    private void SetupCanvases()
    {
        m_canvases = new SerializableDictionary<Type, CanvasHandler>();

        foreach (CanvasHandler canvas in GetComponentsInChildren<CanvasHandler>(true))
        {
            Type type = canvas.GetType();

            if (!m_canvases.ContainsKey(type))
            {
                m_canvases.Add(type, canvas);
                canvas.Init();
            }
            else
            {
                this.LogWarning($"CanvasHandler type already registered: {type}");
            }
        }
    }
    
    public T GetCanvas<T>() where T : CanvasHandler
    {
        if (m_canvases.TryGetValue(typeof(T), out CanvasHandler canvas))
            return canvas as T;

        this.LogWarning($"CanvasHandler of type {typeof(T)} not found.");
        return null;
    }

    public void CloseAllCanvases()
    {
        foreach (CanvasHandler canvas in m_canvases.Values)
            canvas.Close();
    }
}