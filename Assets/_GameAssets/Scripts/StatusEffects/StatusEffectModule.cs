using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.StatusEffects
{
    [RequireComponent(typeof(EntityHealthModule))]
    public class StatusEffectModule : EntityModule
    {
        public Action<StatusEffect> OnStatusEffectChanged;
        public Action<StatusEffect> OnStatusEffectRemoved;

        private readonly Dictionary<StatusEffectType, StatusEffect> _statusEffects = new Dictionary<StatusEffectType, StatusEffect>();
        private readonly List<StatusEffect> _statusEffectsSnapshot = new List<StatusEffect>();
        private EntityHealthModule _healthModule;

        public IReadOnlyCollection<StatusEffect> StatusEffects => _statusEffects.Values;

        public override void OnAllModuleInitialized()
        {
            base.OnAllModuleInitialized();
            Owner.TryGetModule(out _healthModule);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _statusEffects.Clear();
            OnStatusEffectChanged = null;
            OnStatusEffectRemoved = null;
        }

        public override void OnCombatExit()
        {
            base.OnCombatExit();
            RemoveAllStatusEffects();
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();
            DealPoisonDamage();
            DecayStatusEffects(StatusEffectDecayTiming.TurnStart);
        }

        public override void OnTurnEnd()
        {
            base.OnTurnEnd();
            DecayStatusEffects(StatusEffectDecayTiming.TurnEnd);
        }

        public bool TryGetStatusEffect(StatusEffectType type, out StatusEffect statusEffect)
        {
            return _statusEffects.TryGetValue(type, out statusEffect);
        }

        public void ApplyStatusEffect(StatusEffectConfig config, int stacks)
        {
            if (config.IsDebuff && stacks > 0 && TryNegateWithArtifact())
                return;

            if (!_statusEffects.TryGetValue(config.Type, out StatusEffect statusEffect))
            {
                statusEffect = new StatusEffect(config);
                _statusEffects.Add(config.Type, statusEffect);
            }

            if (stacks > 0 && config.DecayTiming == StatusEffectDecayTiming.TurnEnd && Owner.IsTakingTurn)
                statusEffect.SkipNextDecay = true;

            AddStacks(statusEffect, stacks);
        }

        private bool TryNegateWithArtifact()
        {
            if (!_statusEffects.TryGetValue(StatusEffectType.Artifact, out StatusEffect artifact))
                return false;

            AddStacks(artifact, -1);
            return true;
        }

        private void DealPoisonDamage()
        {
            if (_statusEffects.TryGetValue(StatusEffectType.Poison, out StatusEffect poison))
                _healthModule.TakeDamage(poison.Stacks * poison.Config.Potency, false, ignoreVulnerable: true, ignoreBlock: true);
        }

        private void DecayStatusEffects(StatusEffectDecayTiming timing)
        {
            TakeStatusEffectsSnapshot();

            foreach (StatusEffect statusEffect in _statusEffectsSnapshot)
            {
                if (statusEffect.Config.DecayTiming != timing)
                    continue;

                if (statusEffect.SkipNextDecay)
                    statusEffect.SkipNextDecay = false;
                else
                    AddStacks(statusEffect, -1);
            }
        }

        private void RemoveAllStatusEffects()
        {
            TakeStatusEffectsSnapshot();

            foreach (StatusEffect statusEffect in _statusEffectsSnapshot)
                RemoveStatusEffect(statusEffect);
        }

        private void TakeStatusEffectsSnapshot()
        {
            _statusEffectsSnapshot.Clear();
            _statusEffectsSnapshot.AddRange(_statusEffects.Values);
        }

        private void AddStacks(StatusEffect statusEffect, int amount)
        {
            statusEffect.Stacks += amount;

            if (statusEffect.IsExpired)
                RemoveStatusEffect(statusEffect);
            else
                OnStatusEffectChanged?.Invoke(statusEffect);
        }

        private void RemoveStatusEffect(StatusEffect statusEffect)
        {
            _statusEffects.Remove(statusEffect.Config.Type);
            OnStatusEffectRemoved?.Invoke(statusEffect);
        }
    }

    public class StatusEffect
    {
        public StatusEffect(StatusEffectConfig config)
        {
            Config = config;
        }

        public StatusEffectConfig Config { get; }
        public int Stacks { get; internal set; }
        public bool SkipNextDecay { get; internal set; }

        public bool IsExpired => Config.DecayTiming == StatusEffectDecayTiming.Never ? Stacks == 0 : Stacks <= 0;
    }
}
