using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;
using Utils.UI;

public class CanvasHandler : MonoBehaviour
{
    [TitleGroup("Dependencies"), Required] [SerializeField]
    private Canvas m_canvas;

    private UIContainer[] m_containers;
    private int m_remainingContainers;
    private bool m_isClosing;
    private bool m_isOpen;

    public virtual void Init()
    {
        m_containers = GetComponentsInChildren<UIContainer>(true);

        foreach (UIContainer container in m_containers)
            container.Init();

        m_canvas.enabled = false;
    }

    [Button]
    public virtual void Open()
    {
        if (m_isOpen) return;

        if (m_isClosing)
        {
            foreach (UIContainer container in m_containers)
                container.OnCloseComplete -= OnContainerClosed;

            m_isClosing = false;
        }

        m_isOpen = true;
        m_canvas.enabled = true;

        foreach (UIContainer container in m_containers)
        {
            if (container.EnableByDefault)
                container.Open();
            else
                container.ForceClose();
        }
    }

    [Button]
    public virtual void Close()
    {
        if (m_isClosing || !m_isOpen) return;

        m_isOpen = false;
        m_isClosing = true;
        m_remainingContainers = 0;

        foreach (UIContainer container in m_containers)
        {
            if (!container.IsOpen) continue;

            m_remainingContainers++;
            container.OnCloseComplete += OnContainerClosed;
            container.Close();
        }

        if (m_remainingContainers == 0)
        {
            m_canvas.enabled = false;
            m_isClosing = false;
        }
    }

    private void OnContainerClosed(UIContainer container)
    {
        container.OnCloseComplete -= OnContainerClosed;

        if (--m_remainingContainers > 0) return;

        m_canvas.enabled = false;
        m_isClosing = false;
    }

    public T GetContainer<T>() where T : UIContainer
    {
        T container = m_containers.OfType<T>().FirstOrDefault();

        if (container == null)
            this.LogWarning($"Container of type {typeof(T)} not found in {name}.");

        return container;
    }
}