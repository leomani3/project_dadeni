using UnityEngine;

namespace Deckbuilder.StatusEffects
{
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Deckbuilder/Status Effect")]
    public class StatusEffectConfig : ScriptableObject
    {
        [SerializeField] private string m_id;
        [SerializeField] private string m_displayName;
        [SerializeField] private Sprite m_icon;
        [SerializeField, TextArea] private string m_description;

        public string Id => m_id;
        public string DisplayName => m_displayName;
        public Sprite Icon => m_icon;
        public string Description => m_description;
    }
}
