using System;
using UnityEngine;

namespace Stats
{
    [Serializable]
    public class LeveledStatModifier : AStatModifier
    {
        [SerializeField] private float[] m_values;

        public LeveledStatModifier(EntityType _entityType, StatType _statType, ModifierType _type, float[] _values, string _id = null)
            : base(_entityType, _statType, _type, _id)
        {
            m_values = _values;
        }

        public StatModifier GetModifierAtLevel(int level)
        {
            if (m_values == null || m_values.Length == 0)
            {
                Debug.LogWarning($"LeveledStatModifier '{id}' has no values defined. Returning modifier with value 0.");
                return BuildModifier(0f, ModifierType.Flat);
            }

            if (level < 0 || level >= m_values.Length)
            {
                Debug.LogWarning($"Level {level} is out of range [0, {m_values.Length - 1}] for LeveledStatModifier '{id}'. Returning modifier with value 0.");
                return BuildModifier(0f, ModifierType.Flat);
            }

            return BuildModifier(m_values[level], type);
        }

        public StatModifier GetLinearlyScaledModifierAtLevel(int level)
        {
            if (m_values == null || m_values.Length == 0)
            {
                Debug.LogWarning($"LeveledStatModifier '{id}' has no values defined. Returning modifier with value 0.");
                return BuildModifier(0f, ModifierType.Flat);
            }

            return BuildModifier(m_values[0] * level, type);
        }

        public void ResizeValues(int newSize)
        {
            if (newSize < 0) newSize = 0;

            var previousValues = m_values ?? Array.Empty<float>();
            var resizedValues = new float[newSize];

            for (int i = 0; i < Mathf.Min(previousValues.Length, newSize); i++)
                resizedValues[i] = previousValues[i];

            m_values = resizedValues;
        }

        private StatModifier BuildModifier(float value, ModifierType modifierType)
        {
            return new StatModifier(entityType, statType, value, modifierType, id)
            {
                statSource  = statSource,
                step        = step,
                application = application
            };
        }
    }
}
