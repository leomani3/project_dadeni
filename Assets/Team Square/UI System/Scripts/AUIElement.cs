using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AUIElement : MonoBehaviour
{
    [TitleGroup("Dependencies")]
    [SerializeField, Required] protected RectTransform m_content;

    protected virtual void Start()
    {
        Init();
    }

    public virtual void Init() { }

    public virtual void Show()
    {
        m_content.gameObject.SetActive(true);
    }
    
    public virtual void Hide()
    {
        m_content.gameObject.SetActive(false);
    }

    public virtual void SetActive(bool _active)
    {
        gameObject.SetActive(_active);
    }
}