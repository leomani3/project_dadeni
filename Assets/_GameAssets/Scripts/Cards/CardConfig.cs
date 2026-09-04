using System.Collections.Generic;
using Deckbuilder.Cards.Actions;
using UnityEngine;

namespace Deckbuilder.Cards
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Deckbuilder/Card Config")]
    public class CardConfig : ScriptableObject
    {
        [SerializeField] private string m_id;
        [SerializeField] private string m_title;
        [SerializeField] private Sprite m_illustration;
        [SerializeField] private AnimationClip m_castAnimation;

        [SerializeField] private ZoneDefinition m_targetZone;
        [SerializeField] private bool m_requiresLineOfSight;
        [SerializeField] private ZoneDefinition m_effectZone;
        [SerializeReference] private List<CardAction> m_actions = new();

        [SerializeField] private ZoneDefinition m_targetZoneUpgraded;
        [SerializeField] private bool m_requiresLineOfSightUpgraded;
        [SerializeField] private ZoneDefinition m_effectZoneUpgraded;
        [SerializeReference] private List<CardAction> m_actionsUpgraded = new();

        public string Id => m_id;
        public string Title => m_title;
        public Sprite Illustration => m_illustration;

        public ZoneDefinition TargetZone => m_targetZone;
        public bool RequiresLineOfSight => m_requiresLineOfSight;

        public ZoneDefinition EffectZone => m_effectZone;

        public AnimationClip CastAnimation => m_castAnimation;

        public IReadOnlyList<CardAction> Actions => m_actions;

        public ZoneDefinition TargetZoneUpgraded => m_targetZoneUpgraded;
        public bool RequiresLineOfSightUpgraded => m_requiresLineOfSightUpgraded;

        public ZoneDefinition EffectZoneUpgraded => m_effectZoneUpgraded;

        public IReadOnlyList<CardAction> ActionsUpgraded => m_actionsUpgraded;
    }
}
