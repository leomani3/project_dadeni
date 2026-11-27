using Deckbuilder.Combat;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Move_New", menuName = "Deckbuilder/Animation/Move")]
public class AnimationMove : ScriptableObject
{
    [SerializeField, Required] private AnimationClip m_clip;
    [SerializeField, Range(0f, 1f)] private float m_impactTime = 0.5f;
    [SerializeField, Min(0.05f)] private float m_speed = 1f;
    [SerializeField] private ImpactDelivery m_delivery = ImpactDelivery.Instant;
    [SerializeField] private Projectile m_projectile;

    public AnimationClip Clip => m_clip;
    public float ImpactTime => m_impactTime;
    public float Speed => m_speed;
    public float Duration => m_clip.length / m_speed;
    public ImpactDelivery Delivery => m_delivery;
    public Projectile Projectile => m_projectile;
}
