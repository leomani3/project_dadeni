using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stats
{
    [Serializable]
    public class Stat
    {
        private float m_baseValue;
        private readonly List<StatModifier> m_modifiers = new();
        private bool m_isDirty = true;

        [SerializeField] private float m_cachedValue;

        public float Value
        {
            get
            {
                if (m_isDirty) Recalculate();
                return m_cachedValue;
            }
        }

        public event Action<float> OnValueChanged;

        public Stat(float baseValue = 0f)
        {
            m_baseValue = baseValue;
            m_cachedValue = m_baseValue;
        }

        public float GetSummedModifierValueOfType(ModifierType modifierType)
        {
            float summedValue = 0f;
            foreach (var modifier in m_modifiers)
            {
                if (modifier.type == modifierType)
                    summedValue += modifier.value;
            }
            return summedValue;
        }

        public void SetBaseValue(float value)
        {
            m_baseValue = value;
            MarkDirty();
        }

        public void SetBaseValueAndRecalculate(float value)
        {
            m_baseValue = value;
            Recalculate();
            OnValueChanged?.Invoke(m_cachedValue);
        }

        public void AddModifier(StatModifier mod)
        {
            var copy = mod.Copy();

            if (copy.id != null)
            {
                var existing = m_modifiers.Find(m => m.id == copy.id);
                if (existing != null)
                {
                    existing.value = copy.value;
                    MarkDirty();
                    return;
                }
            }

            m_modifiers.Add(copy);
            MarkDirty();
        }

        public void RemoveModifier(StatModifier mod)
        {
            if (mod.id != null)
                m_modifiers.RemoveAll(m => m.id == mod.id);
            else
                m_modifiers.RemoveAll(m => m.statType == mod.statType
                                        && m.value == mod.value
                                        && m.type == mod.type);
            MarkDirty();
        }

        public void ForceRecalculate()
        {
            Recalculate();
        }

        private void MarkDirty()
        {
            m_isDirty = true;
            OnValueChanged?.Invoke(Value);
        }

        private void Recalculate()
        {
            float flatTotal = m_baseValue;
            float addedPercentTotal = 0f;
            float multiplierTotal = 1f;

            foreach (var modifier in m_modifiers)
            {
                if (modifier.type == ModifierType.Flat) flatTotal += modifier.value;
                if (modifier.type == ModifierType.Percentage) addedPercentTotal += modifier.value;
                if (modifier.type == ModifierType.Multiplier) multiplierTotal *= modifier.value;
            }

            m_cachedValue = flatTotal * (1f + addedPercentTotal / 100f) * multiplierTotal;
            m_isDirty = false;
        }
    }
}
