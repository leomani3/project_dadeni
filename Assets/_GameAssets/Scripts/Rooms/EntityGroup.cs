using System.Collections.Generic;
using Deckbuilder.Combat;
using UnityEngine;
using Utils;

public class EntityGroup : MonoBehaviour
{
    private const int MaxPlacementAttempts = 30;
    private const float GoldenAngleDegrees = 137.5f;

    [SerializeField] private List<Entity> _entityPrefabs = new List<Entity>();
    [SerializeField] private float _spawnRadius = 3f;
    [SerializeField] private float _minDistanceBetweenEntities = 1.5f;

    [Header("debug")]
    [SerializeField] private Arena _arena;

    private readonly List<Entity> _entities = new List<Entity>();

    private void Awake()
    {
        Init(_entityPrefabs);
    }

    public void Init(IReadOnlyList<Entity> _entitiesToSpawn)
    {
        foreach (Entity _entityPrefab in _entitiesToSpawn)
            Spawn(_entityPrefab);
    }

    private Entity Spawn(Entity _entityPrefab)
    {
        Entity _entity = Instantiate(_entityPrefab, GetFreeSpawnPosition(), CusRandom.Rotation(RotationAxis.Y), transform);
        _entity.onFightQuery += OnEntityFightQuery;
        _entities.Add(_entity);

        return _entity;
    }

    private Vector3 GetFreeSpawnPosition()
    {
        for (int _attempt = 0; _attempt < MaxPlacementAttempts; _attempt++)
        {
            Vector3 _candidatePosition = transform.position + CusRandom.Disk(_spawnRadius);

            if (IsFarEnoughFromSpawnedEntities(_candidatePosition))
                return _candidatePosition;
        }

        return transform.position + GetSpiralOffset(_entities.Count);
    }

    private bool IsFarEnoughFromSpawnedEntities(Vector3 _candidatePosition)
    {
        float _squaredMinDistance = _minDistanceBetweenEntities * _minDistanceBetweenEntities;

        foreach (Entity _entity in _entities)
        {
            Vector3 _toEntity = _entity.transform.position - _candidatePosition;
            _toEntity.y = 0f;

            if (_toEntity.sqrMagnitude < _squaredMinDistance)
                return false;
        }

        return true;
    }

    private Vector3 GetSpiralOffset(int _index)
    {
        float _angle = _index * GoldenAngleDegrees * Mathf.Deg2Rad;
        float _radius = _minDistanceBetweenEntities * Mathf.Sqrt(_index);

        return new Vector3(Mathf.Cos(_angle) * _radius, 0f, Mathf.Sin(_angle) * _radius);
    }

    private void OnEntityFightQuery(Entity entity)
    {
        _arena.StartCombat(_entities);
        entity.onFightQuery -= OnEntityFightQuery;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Color _gizmoColor = new Color(0.3f, 0.8f, 1f);

        UnityEditor.Handles.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.1f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, _spawnRadius);

        UnityEditor.Handles.color = _gizmoColor;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _spawnRadius);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _minDistanceBetweenEntities * 0.5f);
    }
#endif
}
