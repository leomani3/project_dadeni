using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Deckbuilder.Combat
{
    public class TurnCursor : MonoBehaviour
    {
        private static readonly Vector2[] CornerDirections =
        {
            new(-1f, 1f),
            new(1f, 1f),
            new(1f, -1f),
            new(-1f, -1f)
        };

        [SerializeField, Required] private SpriteRenderer[] m_corners = new SpriteRenderer[4];
        [SerializeField] private Color m_color = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField, Min(0f)] private float m_sizeRatio = 0.9f;
        [SerializeField, Min(0f)] private float m_cornerLengthRatio = 0.35f;
        [SerializeField] private float m_heightOffset = 0.02f;
        [SerializeField, Min(0f)] private float m_followSpeed = 12f;
        [SerializeField, Min(0f)] private float m_appearScale = 1.7f;
        [SerializeField, Min(0f)] private float m_appearDuration = 0.3f;
        [SerializeField] private Ease m_appearEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float m_breatheSpacingRatio = 0.1f;
        [SerializeField, Min(0f)] private float m_breatheScaleRatio = 0.12f;
        [SerializeField, Min(0f)] private float m_breatheDuration = 1.1f;
        [SerializeField] private Ease m_breatheEase = Ease.InOutSine;
        [SerializeField, Min(0f)] private float m_bobAmplitude = 0.03f;

        public Transform Target => m_target;

        private Transform m_target;
        private float m_cellSize = 1f;
        private float m_breatheRatio;
        private Tween m_appearTween;
        private Tween m_breatheTween;
        private Vector3 m_followedPosition;

        private void Awake()
        {
            foreach (SpriteRenderer _corner in m_corners)
                _corner.color = m_color;

            SetVisible(false);
            LayoutCorners();
            PlayBreatheTween();
        }

        private void OnDestroy()
        {
            KillAppearTween();

            if (m_breatheTween.IsActive())
                m_breatheTween.Kill();
        }

        public void SetCellSize(float _cellSize)
        {
            if (Mathf.Approximately(m_cellSize, _cellSize))
                return;

            m_cellSize = _cellSize;
            LayoutCorners();
        }

        public void SetTarget(Entity _entity)
        {
            bool _wasHidden = m_target == null;
            m_target = _entity != null ? _entity.transform : null;

            if (m_target == null)
            {
                Hide();
                return;
            }

            m_followedPosition = GetFollowedPosition();

            if (_wasHidden)
                transform.position = m_followedPosition;

            SetVisible(true);
            PlayAppearTween();
        }

        public void Hide()
        {
            m_target = null;
            SetVisible(false);

            KillAppearTween();
            transform.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            if (m_target == null)
                return;

            m_followedPosition = Vector3.Lerp(m_followedPosition, GetFollowedPosition(), 1f - Mathf.Exp(-m_followSpeed * Time.deltaTime));
            transform.position = m_followedPosition + Vector3.up * (GetBreatheSign() * m_bobAmplitude);
        }

        private Vector3 GetFollowedPosition()
        {
            return m_target.position + Vector3.up * m_heightOffset;
        }

        private float GetBreatheSign()
        {
            return Mathf.Lerp(-1f, 1f, m_breatheRatio);
        }

        private void SetVisible(bool _visible)
        {
            foreach (SpriteRenderer _corner in m_corners)
                _corner.enabled = _visible;
        }

        private void PlayBreatheTween()
        {
            m_breatheTween = DOVirtual.Float(0f, 1f, m_breatheDuration, SetBreatheRatio)
                .SetEase(m_breatheEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void SetBreatheRatio(float _ratio)
        {
            m_breatheRatio = _ratio;
            LayoutCorners();
        }

        private void KillAppearTween()
        {
            if (m_appearTween.IsActive())
                m_appearTween.Kill();
        }

        private void PlayAppearTween()
        {
            KillAppearTween();

            transform.localScale = Vector3.one * m_appearScale;
            m_appearTween = transform.DOScale(Vector3.one, m_appearDuration).SetEase(m_appearEase);
        }

        [Button]
        private void LayoutCorners()
        {
            float _breathe = GetBreatheSign();
            float _half = (m_sizeRatio + _breathe * m_breatheSpacingRatio) * m_cellSize * 0.5f;
            float _length = (m_cornerLengthRatio + _breathe * m_breatheScaleRatio * m_cornerLengthRatio) * m_cellSize;

            for (int _i = 0; _i < m_corners.Length; _i++)
            {
                SpriteRenderer _corner = m_corners[_i];
                Vector2 _direction = CornerDirections[_i];

                _corner.transform.localPosition = new Vector3(_direction.x * _half, _direction.y * _half, 0f);
                _corner.transform.localScale = Vector3.one * (_length / _corner.sprite.bounds.size.x);
            }
        }
    }
}
