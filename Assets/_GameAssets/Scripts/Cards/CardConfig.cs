using System.Collections.Generic;
using Deckbuilder.ActionResources;
using Deckbuilder.Cards.Actions;
using UnityEngine;

namespace Deckbuilder.Cards
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Deckbuilder/Card Config")]
    public class CardConfig : ScriptableObject
    {
        [SerializeField] private string m_id;
        [SerializeField] private string m_title;
        [SerializeField] private CardType m_type;
        [SerializeField] private Sprite m_illustration;
        [SerializeField] private CastStyle m_castStyle;

        [SerializeField, Min(0)] private int m_cost;
        [SerializeField] private ActionResourceConfig m_costResource;
        [SerializeField] private bool m_exhausts;
        [SerializeField] private ZoneDefinition m_targetZone;
        [SerializeField] private bool m_requiresLineOfSight;
        [SerializeField] private ZoneDefinition m_effectZone;
        [SerializeReference] private List<CardAction> m_actions = new();

        [SerializeField, Min(0)] private int m_costUpgraded;
        [SerializeField] private ActionResourceConfig m_costResourceUpgraded;
        [SerializeField] private bool m_exhaustsUpgraded;
        [SerializeField] private ZoneDefinition m_targetZoneUpgraded;
        [SerializeField] private bool m_requiresLineOfSightUpgraded;
        [SerializeField] private ZoneDefinition m_effectZoneUpgraded;
        [SerializeReference] private List<CardAction> m_actionsUpgraded = new();

        public string Id => m_id;
        public string Title => m_title;
        public CardType Type => m_type;
        public Sprite Illustration => m_illustration;
        public CastStyle CastStyle => m_castStyle;

        public int GetCost(bool _upgraded)
        {
            return _upgraded ? m_costUpgraded : m_cost;
        }

        public ActionResourceConfig GetCostResource(bool _upgraded)
        {
            return _upgraded ? m_costResourceUpgraded : m_costResource;
        }

        public bool GetExhausts(bool _upgraded)
        {
            return _upgraded ? m_exhaustsUpgraded : m_exhausts;
        }

        public ZoneDefinition GetTargetZone(bool _upgraded)
        {
            return _upgraded ? m_targetZoneUpgraded : m_targetZone;
        }

        public bool GetRequiresLineOfSight(bool _upgraded)
        {
            return _upgraded ? m_requiresLineOfSightUpgraded : m_requiresLineOfSight;
        }

        public ZoneDefinition GetEffectZone(bool _upgraded)
        {
            return _upgraded ? m_effectZoneUpgraded : m_effectZone;
        }

        public IReadOnlyList<CardAction> GetActions(bool _upgraded)
        {
            return _upgraded ? m_actionsUpgraded : m_actions;
        }
    }
}
