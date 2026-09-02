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

        [SerializeField] private ZoneDefinition m_targetZone;
        [SerializeField] private bool m_requiresLineOfSight;

        [SerializeField] private ZoneDefinition m_effectZone;

        [SerializeField] private AnimationClip m_castAnimation;

        [SerializeReference] private List<CardAction> m_actions = new();

        public string Id => m_id;
        public string Title => m_title;
        public Sprite Illustration => m_illustration;

        public ZoneDefinition TargetZone => m_targetZone;
        public bool RequiresLineOfSight => m_requiresLineOfSight;

        public ZoneDefinition EffectZone => m_effectZone;

        public AnimationClip CastAnimation => m_castAnimation;

        public IReadOnlyList<CardAction> Actions => m_actions;
    }
}
