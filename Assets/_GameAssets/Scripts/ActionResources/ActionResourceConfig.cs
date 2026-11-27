using Stats;
using UnityEngine;

namespace Deckbuilder.ActionResources
{
    [CreateAssetMenu(fileName = "NewActionResource", menuName = "Deckbuilder/Action Resource")]
    public class ActionResourceConfig : ScriptableObject
    {
        [SerializeField] private string m_id;
        [SerializeField] private string m_displayName;
        [SerializeField] private string m_shortName;
        [SerializeField] private Color m_color = Color.white;
        [SerializeField] private StatType m_maxStat;
        [SerializeField] private bool m_startsCombatFull = true;
        [SerializeField] private bool m_refillsOnTurnStart = true;
        [SerializeField, Min(0)] private int m_gainOnTurnEnd;

        public string Id => m_id;
        public string DisplayName => m_displayName;
        public string ShortName => m_shortName;
        public Color Color => m_color;
        public StatType MaxStat => m_maxStat;
        public bool StartsCombatFull => m_startsCombatFull;
        public bool RefillsOnTurnStart => m_refillsOnTurnStart;
        public int GainOnTurnEnd => m_gainOnTurnEnd;
    }
}
