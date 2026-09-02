using UnityEngine;

namespace Deckbuilder.Cards
{
    public abstract class CardActionDefinition : ScriptableObject
    {
        [SerializeField] private float m_weightPerUnit = 1f;

        public float WeightPerUnit => m_weightPerUnit;
    }
}
