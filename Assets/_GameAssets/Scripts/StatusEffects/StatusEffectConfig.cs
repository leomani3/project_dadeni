using UnityEngine;

namespace Deckbuilder.StatusEffects
{
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Deckbuilder/Status Effect")]
    public class StatusEffectConfig : ScriptableObject
    {
        [SerializeField] private string m_id;
        [SerializeField] private StatusEffectType m_type;
        [SerializeField] private string m_displayName;
        [SerializeField] private Sprite m_icon;
        [SerializeField, TextArea] private string m_description;
        [SerializeField] private StatusEffectDecayTiming m_decayTiming;
        [SerializeField] private bool m_isDebuff;
        [SerializeField] private float m_potency;

        public string Id => m_id;
        public StatusEffectType Type => m_type;
        public string DisplayName => m_displayName;
        public Sprite Icon => m_icon;
        public string Description => m_description;
        public StatusEffectDecayTiming DecayTiming => m_decayTiming;
        public bool IsDebuff => m_isDebuff;
        public float Potency => m_potency;
    }
}
