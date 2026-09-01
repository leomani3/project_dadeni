using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utils.UI
{
    public class UIContainer : MonoBehaviour
    {
        public event Action<UIContainer> OnCloseComplete;
        
        [TitleGroup("Dependencies")]
        [SerializeField, Required] public GameObject m_content;

        [TitleGroup("Settings")]
        [SerializeField] protected bool m_enableByDefault;

        [TitleGroup("Variables")]
        [SerializeField, ReadOnly] protected bool m_isOpen;

        [TitleGroup("Show Animation")]
        [SerializeField] private bool m_enableFadeIn;
        [SerializeField] private bool m_enableMoveIn;
        [SerializeField, ShowIf("@m_enableMoveIn")] private Vector2 m_moveInVector;
        [SerializeField, ShowIf("@m_enableMoveIn || m_enableFadeIn")] private float m_showDuration = 0.25f;

        [TitleGroup("Hide Animation")]
        [SerializeField] private bool m_enableFadeOut;
        [SerializeField] private bool m_enableMoveOut;
        [SerializeField, ShowIf("@m_enableMoveOut")] private Vector2 m_moveOutVector;
        [SerializeField, ShowIf("@m_enableMoveOut || m_enableFadeOut")] private float m_hideDuration = 0.25f;

        private CanvasGroup m_contentCanvasGroup;
        private RectTransform m_contentRectTransform;
        private Tween m_fadeTween;
        private Tween m_moveTween;
        private Tween m_waitTween;
        private Vector2 m_initialContentAnchoredPos;
        private bool m_isClosing;

        public bool IsOpen => m_isOpen;
        public bool EnableByDefault => m_enableByDefault;

        private void Awake()
        {
            m_contentRectTransform = m_content.GetComponent<RectTransform>();

            m_contentCanvasGroup = m_content.GetComponent<CanvasGroup>();
            if (m_contentCanvasGroup == null)
                m_contentCanvasGroup = m_content.AddComponent<CanvasGroup>();
        }

        public virtual void Init()
        {
            m_initialContentAnchoredPos = m_contentRectTransform.anchoredPosition;

            foreach (AUIElement item in m_contentRectTransform.GetComponentsInChildren<AUIElement>())
                item.Init();
        }

        public virtual void Open()
        {
            if (m_isOpen) return;

            if (m_isClosing)
            {
                if (m_waitTween.IsActive()) m_waitTween.Kill();
                m_isClosing = false;
            }

            m_isOpen = true;

            if (m_enableFadeIn)
            {
                if (m_fadeTween.IsActive()) m_fadeTween.Kill();
                m_contentCanvasGroup.alpha = 0;
                m_fadeTween = m_contentCanvasGroup.DOFade(1, m_showDuration);
            }
            else
            {
                m_contentCanvasGroup.alpha = 1;
            }

            if (m_enableMoveIn)
            {
                if (m_moveTween.IsActive()) m_moveTween.Kill();
                m_contentRectTransform.anchoredPosition = m_initialContentAnchoredPos - m_moveInVector;
                m_moveTween = m_contentRectTransform.DOAnchorPos(m_initialContentAnchoredPos, m_showDuration);
            }
        }

        public virtual void Close()
        {
            if (m_isClosing || !m_isOpen) return;

            m_isOpen = false;
            m_isClosing = true;

            if (!m_enableFadeOut && !m_enableMoveOut)
            {
                m_contentCanvasGroup.alpha = 0;
                SetClosed();
                return;
            }

            if (m_enableFadeOut)
            {
                if (m_fadeTween.IsActive()) m_fadeTween.Kill();
                m_fadeTween = m_contentCanvasGroup.DOFade(0, m_hideDuration);
            }

            if (m_enableMoveOut)
            {
                if (m_moveTween.IsActive()) m_moveTween.Kill();
                m_contentRectTransform.anchoredPosition = m_initialContentAnchoredPos;
                m_moveTween = m_contentRectTransform.DOAnchorPos(m_initialContentAnchoredPos + m_moveOutVector, m_hideDuration);
            }

            m_waitTween = DOVirtual.DelayedCall(m_hideDuration, SetClosed);
        }
        
        public void ForceClose()
        {
            m_isOpen = false;
            m_isClosing = false;
            m_contentCanvasGroup.alpha = 0;
            if (m_contentRectTransform != null)
                m_contentRectTransform.anchoredPosition = m_initialContentAnchoredPos;
        }

        private void SetClosed()
        {
            m_isClosing = false;
            OnCloseComplete?.Invoke(this);
        }
    }
}