using UnityEngine;

namespace Utils
{
    public class AnimatorRootMotion : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Entity _owner;
        [SerializeField] private bool _applyRootMotion = true;

        public bool ApplyRootMotion
        {
            get => _applyRootMotion;
            set => _applyRootMotion = value;
        }

        private void Reset()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            _owner = GetComponentInParent<Entity>();
        }

        private void OnAnimatorMove()
        {
            if (!_applyRootMotion || _rigidbody == null || _owner == null || _owner.Animator == null)
                return;

            MoveWithRootMotion();
        }

        private void MoveWithRootMotion()
        {
            Vector3 targetPosition = transform.position + _owner.Animator.deltaPosition;
            _rigidbody.MovePosition(targetPosition);

            Quaternion targetRotation = _rigidbody.rotation * _owner.Animator.deltaRotation;
            _rigidbody.MoveRotation(targetRotation);
        }
    }
}
