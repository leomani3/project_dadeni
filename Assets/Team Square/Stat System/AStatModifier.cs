using System;
using UnityEngine;

namespace Stats
{
    public enum ModifierType
    {
        Flat,
        Percentage,
        Multiplier
    }

    [Serializable]
    public abstract class AStatModifier
    {
        public string id;
        public EntityType entityType;
        public ScriptableObject statSource;
        [Range(0, StatManager.MAX_STEPS - 1)] public int step = 0;
        [Range(0, StatManager.MAX_APPLICATIONS - 1)] public int application = 0;
        public StatType statType;
        public ModifierType type;

        protected AStatModifier(EntityType _entityType, StatType _statType, ModifierType _type, string _id = null)
        {
            id         = _id;
            entityType = _entityType;
            statType   = _statType;
            type       = _type;
        }
    }
}
