using System;
using DG.Tweening;
using MPUIKIT;
using MyBox;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class TutorialUIManager : MyBox.Singleton<TutorialUIManager>
{
    [Header("Highlight References")]
    [SerializeField] private MPImage m_highlightImage;
    [SerializeField] private Image m_highlightBackground;
    [SerializeField] private CanvasGroup m_highlightCanvasGroup;
    
    [Header("Text Bubble References")]
    [SerializeField] private RectTransform m_textBubble;
    [SerializeField] private TextMeshProUGUI m_textBubbleTitle;
    [SerializeField] private TextMeshProUGUI m_textBubbleText;
    [SerializeField] private CanvasGroup m_textBubbleCanvasGroup;
    [SerializeField, Required] private GameObject m_clickToContinueObject;
    
    [Header("Highlight Settings")]
    [SerializeField] private float m_highlightInitialScale = 10f;
    [SerializeField] private float m_highlightAnimationDuration = 0.5f;
    [SerializeField] private Ease m_highlightEase = Ease.Linear;
    
    [Header("Highlight Breathe Settings")]
    [SerializeField] private float m_highlightBreatheScale = 1.1f;
    [SerializeField] private float m_highlightBreatheDuration = 1f;
    
    [Header("Text Bubble Settings")]
    [SerializeField] private float m_textBubbleInitialScale = 0f;
    [SerializeField] private float m_textBubbleAnimationDuration = 0.5f;
    [SerializeField] private Ease m_textBubbleEase = Ease.OutBack;
    [SerializeField] private float m_textBubbleHideDuration = 0.3f;
    [SerializeField] private Ease m_textBubbleHideEase = Ease.InBack;
    
    [Header("Text Bubble Breathe Settings")]
    [SerializeField] private float m_textBubbleBreatheScale = 1.02f;
    [SerializeField] private float m_textBubbleBreatheDuration = 2f;
    
    private Tween m_highlightFadeTween;
    private Tween m_highlightScaleTween;
    private Tween m_highlightBreatheTween;
    
    private Tween m_textBubbleFadeTween;
    private Tween m_textBubbleScaleTween;
    private Tween m_textBubbleBreatheTween;
    

    private void Start()
    {
        m_highlightCanvasGroup.alpha = 0;
        m_highlightBackground.enabled = false;
        
        m_textBubbleCanvasGroup.alpha = 0;
        m_textBubble.gameObject.SetActive(false);
    }

    public void SpawnHighlight(Vector3 screenPosition, float finalScale, bool hideOnComplete = true, bool enableBackground = false, Vector3 offset = default)
    {
        if (m_highlightScaleTween.IsActive())
            m_highlightScaleTween.Kill();
        
        if (m_highlightFadeTween.IsActive())
            m_highlightFadeTween.Kill();
        
        if (m_highlightBreatheTween.IsActive())
            m_highlightBreatheTween.Kill();

        m_highlightImage.transform.position = screenPosition + offset;
        m_highlightImage.transform.localScale = Vector3.one * m_highlightInitialScale;
        
        m_highlightBackground.enabled = enableBackground;
        // m_highlightCanvasGroup.alpha = 1;
        m_highlightFadeTween = m_highlightCanvasGroup.DOFade(1, m_highlightAnimationDuration).SetEase(m_highlightEase).SetUpdate(true);
        
        m_highlightImage.gameObject.SetActive(true);
        
        // m_highlightFadeTween = m_highlightImage.DOFade(1, m_highlightAnimationDuration).SetEase(m_highlightEase).SetUpdate(true);
        m_highlightScaleTween = m_highlightImage.transform.DOScale(finalScale, m_highlightAnimationDuration).SetEase(m_highlightEase).SetUpdate(true);
        
        m_highlightScaleTween.onComplete += () =>
        {
            if (hideOnComplete)
            {
                m_highlightImage.gameObject.SetActive(false);
                m_highlightCanvasGroup.alpha = 0;
                m_highlightBackground.enabled = false;
            }
            else
            {
                // Start breathing animation
                m_highlightBreatheTween = m_highlightImage.transform.DOScale(finalScale * m_highlightBreatheScale, m_highlightBreatheDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        };
    }
    
    public void SpawnHighlightWorld(Vector3 worldPosition, float finalScale, bool hideOnComplete = true, bool enableBackground = false, Vector3 offset = default)
    {
        Vector3 screenPosition = CameraManager.Instance.MainCam.WorldToScreenPoint(worldPosition);
        SpawnHighlight(screenPosition, finalScale, hideOnComplete, enableBackground, offset);
    }
    
    public void SpawnTextBubble(Vector3 screenPosition, string title, string text, bool showClickToContinue = false)
    {
        // this.Log($"Spawning text bubble at {screenPosition} with title: {title} and text: {text}. clickToContinue: {showClickToContinue}");
        if (m_textBubbleScaleTween.IsActive())
            m_textBubbleScaleTween.Kill();
        
        if (m_textBubbleFadeTween.IsActive())
            m_textBubbleFadeTween.Kill();
        
        if (m_textBubbleBreatheTween.IsActive())
            m_textBubbleBreatheTween.Kill();

        m_textBubble.position = screenPosition;
        m_textBubble.localScale = Vector3.one * m_textBubbleInitialScale;
        m_textBubbleTitle.text = title;
        m_textBubbleText.text = text;
        
        m_textBubbleCanvasGroup.alpha = 0;
        m_textBubble.gameObject.SetActive(true);
        
        m_textBubbleFadeTween = m_textBubbleCanvasGroup.DOFade(1, m_textBubbleAnimationDuration).SetEase(m_textBubbleEase).SetUpdate(true);
        m_textBubbleScaleTween = m_textBubble.DOScale(1f, m_textBubbleAnimationDuration).SetEase(m_textBubbleEase).SetUpdate(true);
        
        m_textBubbleScaleTween.onComplete += () =>
        {
            // Start subtle breathing animation
            m_textBubbleBreatheTween = m_textBubble.DOScale(m_textBubbleBreatheScale, m_textBubbleBreatheDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        };

        m_clickToContinueObject.SetActive(showClickToContinue);
    }

    public void DespawnAllTutos()
    {
        DespawnHighlight();
        DespawnTextBubble();
    }
    
    public void DespawnTextBubble()
    {
        if (m_textBubbleScaleTween.IsActive())
            m_textBubbleScaleTween.Kill();
        
        if (m_textBubbleFadeTween.IsActive())
            m_textBubbleFadeTween.Kill();
        
        if (m_textBubbleBreatheTween.IsActive())
            m_textBubbleBreatheTween.Kill();
        
        m_textBubbleFadeTween = m_textBubbleCanvasGroup.DOFade(0, m_textBubbleHideDuration).SetEase(m_textBubbleHideEase).SetUpdate(true);
        m_textBubbleScaleTween = m_textBubble.DOScale(0f, m_textBubbleHideDuration).SetEase(m_textBubbleHideEase).SetUpdate(true);
        
        m_textBubbleScaleTween.onComplete += () =>
        {
            m_textBubble.gameObject.SetActive(false);
        };
    }
    
    public void DespawnHighlight()
    {
        if (m_highlightScaleTween.IsActive())
            m_highlightScaleTween.Kill();
        
        if (m_highlightFadeTween.IsActive())
            m_highlightFadeTween.Kill();
        
        if (m_highlightBreatheTween.IsActive())
            m_highlightBreatheTween.Kill();
        
        m_highlightFadeTween = m_highlightCanvasGroup.DOFade(0, m_highlightAnimationDuration).SetEase(m_highlightEase).SetUpdate(true);
        m_highlightScaleTween = m_highlightImage.transform.DOScale(0f, m_highlightAnimationDuration).SetEase(m_highlightEase).SetUpdate(true);
        
        m_highlightScaleTween.onComplete += () =>
        {
            m_highlightImage.gameObject.SetActive(false);
            m_highlightBackground.enabled = false;
        };
    }
    
    private void ClampTextBubbleToScreen()
    {
        Canvas canvas = m_textBubble.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector3 position = m_textBubble.position;
        Rect bubbleRect = m_textBubble.rect;
        
        // Calculate bubble bounds in screen space
        float width = bubbleRect.width;
        float height = bubbleRect.height;
        
        // Clamp X position
        if (position.x - width < 0)
        {
            position.x = width;
        }
        else if (position.x + width > Screen.width)
        {
            position.x = Screen.width - width;
        }
        
        // Clamp Y position
        if (position.y - height < 0)
        {
            position.y = height;
        }
        else if (position.y + height > Screen.height)
        {
            position.y = Screen.height - height;
        }
        
        m_textBubble.position = position;
    }
}