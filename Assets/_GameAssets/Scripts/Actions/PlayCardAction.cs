using System;
using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Cards;
using Deckbuilder.Cards.Actions;
using Deckbuilder.Combat;
using Deckbuilder.Grid;
using UnityEngine;
using Utils;

namespace Deckbuilder.Actions
{
    public class PlayCardAction : IEntityAction
    {
        private readonly CombatManager m_combatManager;
        private readonly CardInstance m_card;
        private readonly Entity m_caster;
        private readonly GridCell m_targetCell;
        private int m_projectilesInFlight;

        public PlayCardAction(CombatManager _combatManager, CardInstance _card, Entity _caster, GridCell _targetCell)
        {
            m_combatManager = _combatManager;
            m_card = _card;
            m_caster = _caster;
            m_targetCell = _targetCell;
        }

        public IEnumerator Execute()
        {
            if (m_caster.TryGetModule(out EntityCombatMoverModule _combatMover))
                _combatMover.FaceTowards(m_targetCell.transform.position);

            m_caster.TryGetModule(out EntityAnimationModule _animationModule);
            yield return _animationModule.PlayCast(m_card.Config.Type, m_card.Config.CastStyle, _move => Release(_move, _animationModule));

            while (m_projectilesInFlight > 0)
                yield return null;
        }

        private void Release(AnimationMove _move, EntityAnimationModule _casterAnimation)
        {
            if (!CardExecutor.TryResolve(m_combatManager, m_card, m_caster, m_targetCell, out CardActionContext _context))
            {
                m_caster.LogWarning($"Failed to play '{m_card.Title}' on {m_targetCell.name}.");
                return;
            }

            CardExecutor.ApplyOnRelease(_context);

            if (_move.Delivery == ImpactDelivery.Instant)
            {
                CardExecutor.ApplyToZone(_context);
                return;
            }

            Vector3 _origin = _casterAnimation.CastOrigin.position;
            List<Entity> _targets = CardExecutor.GetAffectedEntities(_context);

            if (_move.Delivery == ImpactDelivery.ProjectileToTargetCell || _targets.Count == 0)
            {
                LaunchProjectile(_move.Projectile, _origin, m_targetCell.transform.position, Vector3.up, () => CardExecutor.ApplyToZone(_context));
                return;
            }

            int _remaining = _targets.Count;

            foreach (Entity _target in _targets)
            {
                _target.TryGetModule(out EntityAnimationModule _targetAnimation);
                Vector3 _hitPoint = _targetAnimation.HitPoint.position;
                Vector3 _impactNormal = Vector3.ProjectOnPlane(_origin - _hitPoint, Vector3.up).normalized;

                LaunchProjectile(_move.Projectile, _origin, _hitPoint, _impactNormal, () =>
                {
                    CardExecutor.ApplyToEntity(_context, _target);

                    if (--_remaining == 0)
                        CardExecutor.ApplyToTargetCell(_context);
                });
            }
        }

        private void LaunchProjectile(Projectile _prefab, Vector3 _from, Vector3 _to, Vector3 _impactNormal, Action _onArrival)
        {
            m_projectilesInFlight++;

            Projectile _projectile = UnityEngine.Object.Instantiate(_prefab, _from, Quaternion.identity);
            _projectile.Launch(_from, _to, _impactNormal, () =>
            {
                m_projectilesInFlight--;
                _onArrival();
            });
        }
    }
}
