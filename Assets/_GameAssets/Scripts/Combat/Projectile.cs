using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Deckbuilder.Combat
{
    public class Projectile : MonoBehaviour
    {
        private const float MinFlightDuration = 0.05f;

        [SerializeField, Min(0.1f)] private float m_speed = 18f;
        [SerializeField, Min(0f)] private float m_arcHeightPerUnit = 0.06f;
        [SerializeField] private GameObject m_launchVFX;
        [SerializeField] private GameObject m_impactVFX;
        [SerializeField, Min(0f)] private float m_vfxLifetime = 3f;
        [SerializeField, Required] private ParticleSystem[] m_flightParticles;
        [SerializeField, Required] private TrailRenderer[] m_trails;

        public void Launch(Vector3 _from, Vector3 _to, Vector3 _impactNormal, Action _onArrival)
        {
            Vector3 _direction = _to - _from;
            transform.SetPositionAndRotation(_from, _direction.sqrMagnitude > 0f ? Quaternion.LookRotation(_direction) : transform.rotation);

            foreach (TrailRenderer _trail in m_trails)
                _trail.emitting = true;

            foreach (ParticleSystem _particles in m_flightParticles)
                _particles.Play(false);

            if (m_launchVFX != null)
                SpawnVFX(m_launchVFX, _from, transform.rotation);

            StartCoroutine(Fly(_from, _to, _impactNormal, _onArrival));
        }

        private IEnumerator Fly(Vector3 _from, Vector3 _to, Vector3 _impactNormal, Action _onArrival)
        {
            float _distance = Vector3.Distance(_from, _to);
            float _duration = Mathf.Max(MinFlightDuration, _distance / m_speed);
            float _arcHeight = _distance * m_arcHeightPerUnit;
            float _elapsed = 0f;

            while (_elapsed < _duration)
            {
                _elapsed += Time.deltaTime;
                float _progress = Mathf.Clamp01(_elapsed / _duration);
                Vector3 _position = GetArcPoint(_from, _to, _arcHeight, _progress);
                Vector3 _velocity = _position - transform.position;

                if (_velocity.sqrMagnitude > 0f)
                    transform.rotation = Quaternion.LookRotation(_velocity);

                transform.position = _position;
                yield return null;
            }

            if (m_impactVFX != null)
                SpawnVFX(m_impactVFX, _to, Quaternion.FromToRotation(Vector3.up, _impactNormal));

            _onArrival();

            foreach (ParticleSystem _particles in m_flightParticles)
                _particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            float _linger = 0f;

            foreach (TrailRenderer _trail in m_trails)
            {
                _trail.emitting = false;
                _linger = Mathf.Max(_linger, _trail.time);
            }

            Destroy(gameObject, _linger);
        }

        private static Vector3 GetArcPoint(Vector3 _from, Vector3 _to, float _arcHeight, float _progress)
        {
            return Vector3.Lerp(_from, _to, _progress) + Vector3.up * (_arcHeight * 4f * _progress * (1f - _progress));
        }

        private void SpawnVFX(GameObject _prefab, Vector3 _position, Quaternion _rotation)
        {
            Destroy(Instantiate(_prefab, _position, _rotation), m_vfxLifetime);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_flightParticles = GetComponentsInChildren<ParticleSystem>(true);
            m_trails = GetComponentsInChildren<TrailRenderer>(true);
        }
#endif
    }
}
