using Deckbuilder.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Utils;

namespace Deckbuilder.Player
{
    public class PlayerRoomController : MonoBehaviour
    {
        [SerializeField] private Entity m_entity;
        [SerializeField] private EntityMoveModule m_moveModule;
        [SerializeField] private Arena m_arena;

        [SerializeField] private LayerMask m_groundLayerMask = ~0;
        [SerializeField] private LayerMask m_entityLayerMask = ~0;
        [SerializeField] private float m_distanceToStartCombat = 1.5f;

        private Entity m_combatTarget;

        private void Reset()
        {
            m_entity = GetComponent<Entity>();
            m_moveModule = GetComponent<EntityMoveModule>();
        }

        private void Update()
        {
            if (m_moveModule == null || Mouse.current == null || !Mouse.current.leftButton.isPressed)
                return;

            if (IsCombatRunning())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            bool _isNewClick = Mouse.current.leftButton.wasPressedThisFrame;

            if (_isNewClick)
            {
                m_combatTarget = null;

                if (TryGetEnemyUnderCursor(out Entity _enemy))
                {
                    MoveToEnemy(_enemy);
                    return;
                }
            }

            if (m_combatTarget != null)
                return;

            if (TryGetGroundPointUnderCursor(out Vector3 _groundPoint))
                MoveToGround(_groundPoint, _isNewClick);
        }

        private bool IsCombatRunning()
        {
            Arena _arena = m_arena;
            return _arena != null && _arena.IsCombatRunning;
        }

        private void MoveToEnemy(Entity _enemy)
        {
            m_combatTarget = _enemy;
            m_moveModule.MoveTo(_enemy.transform.position, m_distanceToStartCombat, StartCombatWithTarget);

            PlayClickFeedback(_enemy.transform.position);
        }

        private void MoveToGround(Vector3 _groundPoint, bool _isNewClick)
        {
            m_moveModule.MoveTo(_groundPoint);

            if (_isNewClick)
                PlayClickFeedback(_groundPoint);
        }

        private void StartCombatWithTarget()
        {
            Entity _enemy = m_combatTarget;
            m_combatTarget = null;

            if (_enemy == null)
                return;

            if (m_arena == null)
            {
                this.LogError("No Arena assigned, the combat cannot start.");
                return;
            }

            m_arena.StartCombat();
        }

        private void PlayClickFeedback(Vector3 _worldPosition)
        {
            if (ClickFeedbackManager.Instance != null)
                ClickFeedbackManager.Instance.PlayMove(_worldPosition);
        }

        private bool TryGetEnemyUnderCursor(out Entity _enemy)
        {
            _enemy = null;

            if (!TryRaycastUnderCursor(m_entityLayerMask, out RaycastHit _hit))
                return false;

            Entity _hitEntity = _hit.collider.GetComponentInParent<Entity>();
            if (_hitEntity == null || _hitEntity == m_entity)
                return false;

            if (!_hitEntity.TryGetModule(out EntityTeamModule _teamModule) || _teamModule.Team != Team.Enemy)
                return false;

            _enemy = _hitEntity;
            return true;
        }

        private bool TryGetGroundPointUnderCursor(out Vector3 _groundPoint)
        {
            _groundPoint = Vector3.zero;

            if (!TryRaycastUnderCursor(m_groundLayerMask, out RaycastHit _hit))
                return false;

            if (m_entity != null && m_entity.Collider != null && _hit.collider == m_entity.Collider)
                return false;

            _groundPoint = _hit.point;
            return true;
        }

        private bool TryRaycastUnderCursor(LayerMask _layerMask, out RaycastHit _hit)
        {
            _hit = default;

            Camera _camera = CameraManager.Instance != null ? CameraManager.Instance.MainCam : Camera.main;
            if (_camera == null)
                return false;

            Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return Physics.Raycast(_ray, out _hit, Mathf.Infinity, _layerMask);
        }
    }
}
