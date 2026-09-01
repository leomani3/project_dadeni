using System;

namespace Stats
{
    [Serializable]
    public class StatModifier : AStatModifier
    {
        public float value;

        public StatModifier(EntityType _entityType, StatType _statType, float _value, ModifierType _type, string _id = null)
            : base(_entityType, _statType, _type, _id)
        {
            value = _value;
        }

        public StatModifier Copy() => new StatModifier(entityType, statType, value, type, id)
        {
            statSource  = statSource,
            step        = step,
            application = application
        };
    }
}
