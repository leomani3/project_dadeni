using UnityEngine;

[RequireComponent(typeof(Entity))]
public abstract class EntityModule : MonoBehaviour
{
    private Entity _ownerEntity;

    public Entity Owner => _ownerEntity;

    internal void Initialize(Entity owner)
    {
        _ownerEntity = owner;
        OnInitialize();
    }

    protected virtual void OnInitialize() { }
    public virtual void OnAllModuleInitialized() { }
    public virtual void Cleanup() { }
    public virtual void OnCombatEnter() { }
    public virtual void OnCombatExit() { }
    public virtual void CacheReferences() { }
}
