using UnityEngine;

public class EntityAE : MonoBehaviour
{
    private Entity m_entity;

    private void Awake()
    {
        m_entity = GetComponentInParent<Entity>();
    }

    public void OnTrigger()
    {
        if (m_entity != null && m_entity.TryGetModule(out EntityAnimationModule _animationModule))
            _animationModule.OnTrigger();
    }

    public void OnComplete()
    {
        if (m_entity != null && m_entity.TryGetModule(out EntityAnimationModule _animationModule))
            _animationModule.OnComplete();
    }
}
