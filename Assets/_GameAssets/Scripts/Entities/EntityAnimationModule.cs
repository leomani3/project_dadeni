using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class EntityAnimationModule : EntityModule
{
    private const string CastTriggerName = "Cast";
    private const string SpeedParameterName = "Speed";
    private const string CastPlaceholderClipName = "CastPlaceholder";
    private const float CompletionSafetyMargin = 1f;

    [SerializeField] private Animator m_animator;
    [SerializeField] private float m_speedDamping = 0.12f;

    private AnimatorOverrideController m_overrideController;
    private Action m_pendingTriggerCallback;
    private bool m_pendingCompletion;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        if (m_animator != null && m_overrideController == null && m_animator.runtimeAnimatorController != null)
        {
            m_overrideController = BuildOverrideController(m_animator.runtimeAnimatorController);
            m_animator.runtimeAnimatorController = m_overrideController;
        }
    }

    public void SetLocomotionSpeed(float _normalizedSpeed)
    {
        if (m_animator == null)
            return;

        m_animator.SetFloat(SpeedParameterName, _normalizedSpeed, m_speedDamping, Time.deltaTime);
    }

    public IEnumerator PlayCast(AnimationClip _clip, Action _onTrigger)
    {
        if (m_animator == null || m_overrideController == null || _clip == null)
        {
            _onTrigger?.Invoke();
            yield break;
        }

        m_overrideController[CastPlaceholderClipName] = _clip;
        m_pendingTriggerCallback = _onTrigger;
        m_pendingCompletion = true;

        m_animator.SetTrigger(CastTriggerName);

        float _safetyTimeout = _clip.length + CompletionSafetyMargin;
        float _elapsed = 0f;

        while (m_pendingCompletion && _elapsed < _safetyTimeout)
        {
            _elapsed += Time.deltaTime;
            yield return null;
        }

        if (m_pendingCompletion)
        {
            this.LogError($"Clip '{_clip.name}' never fired an 'OnComplete' animation event. Forcing completion so the action queue doesn't stall.");
            m_pendingCompletion = false;
        }

        if (m_pendingTriggerCallback != null)
        {
            this.LogError($"Clip '{_clip.name}' finished without firing an 'OnTrigger' animation event. Card effects were never applied.");
            m_pendingTriggerCallback = null;
        }
    }

    public void OnTrigger()
    {
        m_pendingTriggerCallback?.Invoke();
        m_pendingTriggerCallback = null;
    }

    public void OnComplete()
    {
        m_pendingCompletion = false;
    }

    private static AnimatorOverrideController BuildOverrideController(RuntimeAnimatorController _source)
    {
        AnimatorOverrideController _authoredOverride = _source as AnimatorOverrideController;

        if (_authoredOverride == null)
            return new AnimatorOverrideController(_source);

        AnimatorOverrideController _runtimeOverride = new AnimatorOverrideController(_authoredOverride.runtimeAnimatorController);

        List<KeyValuePair<AnimationClip, AnimationClip>> _authoredClips = new List<KeyValuePair<AnimationClip, AnimationClip>>(_authoredOverride.overridesCount);
        _authoredOverride.GetOverrides(_authoredClips);
        _runtimeOverride.ApplyOverrides(_authoredClips);

        return _runtimeOverride;
    }
}
