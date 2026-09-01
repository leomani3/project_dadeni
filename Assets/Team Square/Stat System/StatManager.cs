using System.Collections.Generic;
using MyBox;
using UnityEngine;
using Utils;

namespace Stats
{
    public class StatManager : Singleton<StatManager>
    {
        public const int MAX_STEPS = 5;
        public const int MAX_APPLICATIONS = 5;

        [SerializeField] private List<EntityStatDefinition> m_entityStatDefinitions;

        [SerializeField] private SerializableDictionary<EntityType, Dictionary<StatType, Stat>> _definitionStatsByEntityType;
        [SerializeField] private SerializableDictionary<ScriptableObject, Dictionary<StatType, Stat[,]>> _definitionStatsBySource;
        [SerializeField] private SerializableDictionary<GameObject, Dictionary<StatType, Stat>> _instanceStatsByOwner;
        [SerializeField] private SerializableDictionary<GameObject, Dictionary<ScriptableObject, Dictionary<StatType, Stat[,]>>> _instanceSourceStatsByOwner;

        private SerializableDictionary<GameObject, EntityType> _entityTypeByOwner;

        protected void Awake()
        {
            _definitionStatsByEntityType = new SerializableDictionary<EntityType, Dictionary<StatType, Stat>>();
            _definitionStatsBySource     = new SerializableDictionary<ScriptableObject, Dictionary<StatType, Stat[,]>>();
            _instanceStatsByOwner        = new SerializableDictionary<GameObject, Dictionary<StatType, Stat>>();
            _instanceSourceStatsByOwner  = new SerializableDictionary<GameObject, Dictionary<ScriptableObject, Dictionary<StatType, Stat[,]>>>();
            _entityTypeByOwner           = new SerializableDictionary<GameObject, EntityType>();

            foreach (var definition in m_entityStatDefinitions)
            {
                if (definition == null) continue;
                _definitionStatsByEntityType[definition.entityType] = BuildStatDictionary(definition);
            }
        }

        public Stat GetDefinitionStat(EntityType entityType, StatType statType)
        {
            return GetOrCreateDefinitionStat(entityType, statType);
        }

        public Stat GetDefinitionStat(ScriptableObject source, StatType statType, int step = 0, int application = 0)
        {
            return GetOrCreateDefinitionStat(source, statType, step, application);
        }

        public float GetDefinitionValue(EntityType entityType, StatType statType)
        {
            return GetDefinitionStat(entityType, statType).Value;
        }

        public float GetDefinitionValue(ScriptableObject source, StatType statType, int step = 0, int application = 0)
        {
            return GetDefinitionStat(source, statType, step, application)?.Value ?? 0f;
        }

        public void AddDefinitionModifier(EntityType entityType, StatModifier mod)
        {
            foreach (var flag in entityType.GetFlags())
                GetDefinitionStat(flag, mod.statType).AddModifier(mod);
        }

        public void RemoveDefinitionModifier(EntityType entityType, StatModifier mod)
        {
            foreach (var flag in entityType.GetFlags())
                GetDefinitionStat(flag, mod.statType).RemoveModifier(mod);
        }

        public void AddDefinitionModifier(ScriptableObject source, StatModifier mod, int step = 0, int application = 0)
        {
            GetDefinitionStat(source, mod.statType, step, application)?.AddModifier(mod);
        }

        public void RemoveDefinitionModifier(ScriptableObject source, StatModifier mod, int step = 0, int application = 0)
        {
            GetDefinitionStat(source, mod.statType, step, application)?.RemoveModifier(mod);
        }

        public void AddDefinitionModifier(StatModifier mod)
        {
            if (mod.statSource != null)
                AddDefinitionModifier(mod.statSource, mod, mod.step, mod.application);
            else
                AddDefinitionModifier(mod.entityType, mod);
        }

        public void RemoveDefinitionModifier(StatModifier mod)
        {
            if (mod.statSource != null)
                RemoveDefinitionModifier(mod.statSource, mod, mod.step, mod.application);
            else
                RemoveDefinitionModifier(mod.entityType, mod);
        }

        public void RegisterInstance(GameObject owner, EntityType entityType)
        {
            var instanceStats = new Dictionary<StatType, Stat>();

            if (_definitionStatsByEntityType.TryGetValue(entityType, out var definitionStats))
            {
                foreach (var (statType, definitionStat) in definitionStats)
                {
                    var instanceStat = new Stat(definitionStat.Value);
                    _ = instanceStat.Value;
                    definitionStat.OnValueChanged += instanceStat.SetBaseValueAndRecalculate;
                    instanceStats[statType] = instanceStat;
                }
            }

            _instanceStatsByOwner[owner] = instanceStats;
            _entityTypeByOwner[owner] = entityType;
        }

        public void UnregisterInstance(GameObject owner)
        {
            if (_instanceStatsByOwner.TryGetValue(owner, out var instanceStats))
            {
                if (_entityTypeByOwner.TryGetValue(owner, out var entityType) &&
                    _definitionStatsByEntityType.TryGetValue(entityType, out var definitionStats))
                {
                    foreach (var (statType, instanceStat) in instanceStats)
                    {
                        if (definitionStats.TryGetValue(statType, out var definitionStat))
                            definitionStat.OnValueChanged -= instanceStat.SetBaseValueAndRecalculate;
                    }
                }

                _instanceStatsByOwner.Remove(owner);
                _entityTypeByOwner.Remove(owner);
            }

            UnsubscribeInstanceSourceStats(owner);
        }

        public Stat GetInstanceStat(GameObject owner, StatType statType)
        {
            return GetOrCreateInstanceStat(owner, statType);
        }

        public Stat GetInstanceStat(GameObject owner, ScriptableObject source, StatType statType, int step = 0, int application = 0)
        {
            return GetOrCreateInstanceStat(owner, source, statType, step, application);
        }

        public float GetInstanceValue(GameObject owner, StatType statType)
        {
            return GetInstanceStat(owner, statType).Value;
        }

        public float GetInstanceValue(GameObject owner, ScriptableObject source, StatType statType, int step = 0, int application = 0)
        {
            return GetInstanceStat(owner, source, statType, step, application)?.Value ?? 0f;
        }

        public void AddInstanceModifier(GameObject owner, StatModifier mod)
        {
            GetInstanceStat(owner, mod.statType).AddModifier(mod);
        }

        public void RemoveInstanceModifier(GameObject owner, StatModifier mod)
        {
            GetInstanceStat(owner, mod.statType).RemoveModifier(mod);
        }

        public void AddInstanceModifier(GameObject owner, ScriptableObject source, StatModifier mod, int step = 0, int application = 0)
        {
            GetInstanceStat(owner, source, mod.statType, step, application)?.AddModifier(mod);
        }

        public void RemoveInstanceModifier(GameObject owner, ScriptableObject source, StatModifier mod, int step = 0, int application = 0)
        {
            GetInstanceStat(owner, source, mod.statType, step, application)?.RemoveModifier(mod);
        }

        public void RecalculateAllStats()
        {
            foreach (var statsByType in _definitionStatsByEntityType.Values)
                foreach (var stat in statsByType.Values)
                    stat.ForceRecalculate();

            foreach (var statsByType in _instanceStatsByOwner.Values)
                foreach (var stat in statsByType.Values)
                    stat.ForceRecalculate();

            foreach (var statsByType in _definitionStatsBySource.Values)
                foreach (var statGrid in statsByType.Values)
                    foreach (var stat in statGrid)
                        stat?.ForceRecalculate();
        }

        private Dictionary<StatType, Stat> BuildStatDictionary(EntityStatDefinition definition)
        {
            var stats = new Dictionary<StatType, Stat>();
            foreach (var (statType, baseValue) in definition.baseValues)
                stats[statType] = new Stat(baseValue);
            return stats;
        }

        private static bool IsCellInRange(int step, int application)
        {
            if (step < 0 || step >= MAX_STEPS || application < 0 || application >= MAX_APPLICATIONS)
            {
                Debug.LogWarning($"[StatManager] Cell ({step}, {application}) is out of range. Max is ({MAX_STEPS - 1}, {MAX_APPLICATIONS - 1}).");
                return false;
            }
            return true;
        }

        private Stat GetOrCreateDefinitionStat(EntityType entityType, StatType statType)
        {
            if (!_definitionStatsByEntityType.TryGetValue(entityType, out var statsByType))
            {
                statsByType = new Dictionary<StatType, Stat>();
                _definitionStatsByEntityType[entityType] = statsByType;
            }

            if (!statsByType.TryGetValue(statType, out var stat))
            {
                stat = new Stat(0f);
                statsByType[statType] = stat;
            }

            return stat;
        }

        private Stat GetOrCreateDefinitionStat(ScriptableObject source, StatType statType, int step, int application)
        {
            if (source == null || !IsCellInRange(step, application)) return null;

            if (!_definitionStatsBySource.TryGetValue(source, out var statsByType))
            {
                statsByType = new Dictionary<StatType, Stat[,]>();
                _definitionStatsBySource[source] = statsByType;
            }

            if (!statsByType.TryGetValue(statType, out var statGrid))
            {
                statGrid = new Stat[MAX_STEPS, MAX_APPLICATIONS];
                statsByType[statType] = statGrid;
            }

            statGrid[step, application] ??= new Stat(0f);

            return statGrid[step, application];
        }

        private Stat GetOrCreateInstanceStat(GameObject owner, StatType statType)
        {
            if (!_instanceStatsByOwner.TryGetValue(owner, out var statsByType))
            {
                statsByType = new Dictionary<StatType, Stat>();
                _instanceStatsByOwner[owner] = statsByType;
            }

            if (!statsByType.TryGetValue(statType, out var stat))
            {
                _entityTypeByOwner.TryGetValue(owner, out var entityType);
                var definitionStat = GetOrCreateDefinitionStat(entityType, statType);
                stat = new Stat(definitionStat.Value);
                definitionStat.OnValueChanged += stat.SetBaseValueAndRecalculate;
                statsByType[statType] = stat;
            }

            return stat;
        }

        private Stat GetOrCreateInstanceStat(GameObject owner, ScriptableObject source, StatType statType, int step, int application)
        {
            if (source == null || !IsCellInRange(step, application)) return null;

            if (!_instanceSourceStatsByOwner.TryGetValue(owner, out var statsBySource))
            {
                statsBySource = new Dictionary<ScriptableObject, Dictionary<StatType, Stat[,]>>();
                _instanceSourceStatsByOwner[owner] = statsBySource;
            }

            if (!statsBySource.TryGetValue(source, out var statsByType))
            {
                statsByType = new Dictionary<StatType, Stat[,]>();
                statsBySource[source] = statsByType;
            }

            if (!statsByType.TryGetValue(statType, out var statGrid))
            {
                statGrid = new Stat[MAX_STEPS, MAX_APPLICATIONS];
                statsByType[statType] = statGrid;
            }

            if (statGrid[step, application] == null)
            {
                var definitionStat = GetOrCreateDefinitionStat(source, statType, step, application);
                var instanceStat = new Stat(definitionStat.Value);
                _ = instanceStat.Value;
                definitionStat.OnValueChanged += instanceStat.SetBaseValueAndRecalculate;
                statGrid[step, application] = instanceStat;
            }

            return statGrid[step, application];
        }

        private void UnsubscribeInstanceSourceStats(GameObject owner)
        {
            if (!_instanceSourceStatsByOwner.TryGetValue(owner, out var statsBySource)) return;

            foreach (var (source, statsByType) in statsBySource)
            {
                foreach (var (statType, instanceGrid) in statsByType)
                {
                    for (int step = 0; step < MAX_STEPS; step++)
                    {
                        for (int application = 0; application < MAX_APPLICATIONS; application++)
                        {
                            Stat instanceStat = instanceGrid[step, application];
                            if (instanceStat == null) continue;

                            if (_definitionStatsBySource.TryGetValue(source, out var definitionStatsByType) &&
                                definitionStatsByType.TryGetValue(statType, out var definitionGrid) &&
                                definitionGrid[step, application] != null)
                            {
                                definitionGrid[step, application].OnValueChanged -= instanceStat.SetBaseValueAndRecalculate;
                            }
                        }
                    }
                }
            }

            _instanceSourceStatsByOwner.Remove(owner);
        }
    }
}
