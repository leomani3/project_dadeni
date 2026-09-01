using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using UnityEngine.EventSystems;


public class CustomButton : AUIElement, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    #region Actions
    [TitleGroup("Actions")]
    public Action<int> onClick;
    public Action<int> onHoverEnter;
    public Action<int> onHoverExit;
    #endregion

    #region Dependencies
    [TitleGroup("Dependencies"), Required]
    [SerializeField] protected Button m_button;
    [TitleGroup("Dependencies")]
    [SerializeField] protected RectTransform m_lockedContent;
    [TitleGroup("Dependencies")]
    [SerializeField] private GameObject m_highlightObject;
    #endregion

    #region Settings    
    [TitleGroup("Settings")]
    [ReadOnly]
    [SerializeField] protected int m_index;
    public int Index => m_index;
    public GameConfig.UISettings UISettings => GameConfig.Instance.uiSettings;
    
    #endregion

    #region Var
    protected Sequence m_tweenSequence = null;
    protected Sequence m_hoverSequence = null;
    protected bool m_isLocked;
    protected bool m_isHovered;
    protected bool m_isPressed;
    #endregion


    public override void Init()
    {
        // this.Log("Init");
    }

    public virtual void Init(int _index)
    {
        // this.Log($"button init {_index}");
        SetIndex(_index);
    }

    public virtual void SetLock(bool _isLocked)
    {
        m_isLocked = _isLocked;

        if (m_lockedContent != null)
            m_lockedContent.gameObject.SetActive(_isLocked);
    }

    public virtual void SetInteractible(bool _isInteractible)
    {
        m_button.interactable = _isInteractible;
        
        // Reset scale if becoming non-interactable while hovered
        if (!_isInteractible && m_isHovered)
        {
            m_isHovered = false;
            m_isPressed = false;
            ResetHoverScale();
        }
    }

    [Button]
    public virtual void SetHighlighted(bool _highlighted)
    {
        if (m_highlightObject != null)
            m_highlightObject.gameObject.SetActive(_highlighted);
    }

    public virtual void UpdateValues() { }


    public virtual void Lock()
    {
        this.Log($"Lock");
    }

    public virtual void Unlock()
    {
        this.Log($"Unlock");
    }

    public virtual void PlayClickSound()
    {
        SoundManager.Instance.PlaySound(SoundKeys.ui_button_click_negative);
        
    }
    
    public virtual void PlayNegativeClickSound()
    {
        SoundManager.Instance.PlaySound(SoundKeys.ui_button_click_negative);
    }


    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!m_button.interactable)
            return;

        if (m_isLocked)
            return;

        m_isPressed = true;
        
        // Scale down on press
        if (m_hoverSequence != null && m_hoverSequence.IsActive())
            m_hoverSequence.Complete();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!m_button.interactable)
            return;

        if (m_isLocked)
            return;

        if (!m_isPressed)
            return;

        m_isPressed = false;

        // Bounce back on release
        if (UISettings.bounceOnClick)
            ClickBounce();
        else
        {
            // If no bounce, just return to hover or normal scale
            Vector3 targetScale = m_isHovered ? UISettings.hoverScale : Vector3.one;
            if (m_hoverSequence != null && m_hoverSequence.IsActive())
                m_hoverSequence.Complete();
  
            
            m_hoverSequence = DOTween.Sequence().SetUpdate(true);
            m_hoverSequence.Join(m_content.DOScale(targetScale, UISettings.hoverScaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true));
        }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // this.Log($"on click index {m_index}. invo: {onClick?.GetInvocationList()[0]}");
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!m_button.interactable)
            return;

        if (m_isLocked)
        {
            LockedClickBounce();
            PlayNegativeClickSound();
            return;
        }

        onClick?.Invoke(m_index);
        PlayClickSound();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!m_button.interactable || m_isLocked) return;

        ScaleOnHoverEnter();
        
        m_isHovered = true;
        onHoverEnter?.Invoke(m_index);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!m_button.interactable || m_isLocked) return;   
        
        m_isHovered = false;
        m_isPressed = false;
        onHoverExit?.Invoke(m_index);
        
        ScaleOnHoverExit();
    }


    protected virtual void ScaleOnHoverEnter()
    {
        // Kill existing hover tween
        if (m_hoverSequence != null && m_hoverSequence.IsActive())
            m_hoverSequence.Complete();
        
        m_hoverSequence = DOTween.Sequence().SetUpdate(true);
        m_hoverSequence.Join(m_content.DOPunchScale(Vector3.one * 0.3f, 0.15f, 1));
        m_hoverSequence.Join(m_content.DOPunchRotation(Vector3.forward * -5f, 0.15f, 1));
    }

    protected virtual void ScaleOnHoverExit()
    {
        // Kill existing hover tween
        if (m_hoverSequence != null && m_hoverSequence.IsActive())
            m_hoverSequence.Complete();
        
        m_hoverSequence = DOTween.Sequence().SetUpdate(true);
        m_hoverSequence.Join(m_content.DOScale(Vector3.one, 0.15f));
    }

    protected virtual void ResetHoverScale()
    {
        // Kill existing hover tween
        if (m_hoverSequence != null && m_hoverSequence.IsActive())
            m_hoverSequence.Kill();
        
        m_content.localScale = Vector3.one;
    }

    public void ClickBounce()
    {
        if (m_tweenSequence != null && m_tweenSequence.IsActive() && m_tweenSequence.IsPlaying()) return;
        
        Vector3 targetScale = m_isHovered ? UISettings.hoverScale : Vector3.one;
        
        m_tweenSequence = DOTween.Sequence().SetUpdate(true);
        m_tweenSequence.Append(m_content.DOScale(UISettings.clickBounceScale, UISettings.clickScaleDuration).SetEase(Ease.OutQuad));
        m_tweenSequence.Append(m_content.DOScale(targetScale, UISettings.clickScaleDuration).SetEase(Ease.OutQuad));
    }

    public void LockedClickBounce()
    {
        if (m_tweenSequence != null) if (m_tweenSequence.IsPlaying()) return;
        m_tweenSequence = DOTween.Sequence().SetUpdate(true);
        m_tweenSequence.Append(m_lockedContent.DOShakeAnchorPos(UISettings.lockedShakeDuration, UISettings.lockedShakeStrenght, UISettings.lockedShakeVibrato, 90, false, true, ShakeRandomnessMode.Harmonic).SetEase(Ease.OutQuad));
    }

    public void NegativeClickBounce()
    {
        if (m_tweenSequence != null && m_tweenSequence.IsActive() && m_tweenSequence.IsPlaying()) return;
        m_tweenSequence = DOTween.Sequence().SetUpdate(true);
        m_tweenSequence.Append(m_content.DOShakeAnchorPos(UISettings.lockedShakeDuration, UISettings.lockedShakeStrenght, UISettings.lockedShakeVibrato, 90, false, true, ShakeRandomnessMode.Harmonic).SetEase(Ease.OutQuad));
    }

    public void Hide(bool _isHidden)
    {
        this.Log($"Hide {_isHidden}");
        m_content.gameObject.SetActive(!_isHidden);
        m_button.interactable = !_isHidden;
        
        // Reset hover state when hiding
        if (_isHidden)
        {
            m_isHovered = false;
            m_isPressed = false;
            ResetHoverScale();
        }
    }

    public void SetIndex(int _index)
    {
        m_index = _index;
    }

    protected virtual void CheckButtonInteractible() { }
}