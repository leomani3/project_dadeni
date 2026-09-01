using UnityEngine;
using Utils;

namespace Stats
{
    [CreateAssetMenu(menuName = "Stats/Entity Stat Definition")]
    public class EntityStatDefinition : ScriptableObject
    {
        public EntityType entityType;
        public SerializableDictionary<StatType, float> baseValues;
    }
}
