using System;
using System.Collections;
using System.Collections.Generic;
using Deckbuilder.Cards;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

public class EntityAnimationModule : EntityModule
{
    private const string IdlePlaceholderClipName = "IdlePlaceholder";
    private const string RunPlaceholderClipName = "RunPlaceholder";
    private const string CastAttackPlaceholderClipName = "CastAttackPlaceholder";
    private const string CastUtilityPlaceholderClipName = "CastUtilityPlaceholder";
    private const string DeathPlaceholderClipName = "DeathPlaceholder";
    private const float CompletionSafetyMargin = 1f;
    private const float MaxWaitForLocomotion = 3f;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int CastSpeedParameter = Animator.StringToHash("CastSpeed");
    private static readonly int LocomotionSpeedParameter = Animator.StringToHash("LocomotionSpeed");
    private static readonly int DeathSpeedParameter = Animator.StringToHash("DeathSpeed");
    private static readonly int CastAttackTrigger = Animator.StringToHash("CastAttack");
    private static readonly int CastUtilityTrigger = Animator.StringToHash("CastUtility");
    private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
    private static readonly int CastAttackState = Animator.StringToHash("CastAttack");
    private static readonly int CastUtilityState = Animator.StringToHash("CastUtility");
    private static readonly int DeathState = Animator.StringToHash("Death");

    [SerializeField, Required] private Animator m_animator;
    [SerializeField, Required] private RuntimeAnimatorController m_controller;
    [SerializeField, Required] private CharacterAnimationSet m_animationSet;
    [SerializeField, Required] private Transform m_castOrigin;
    [SerializeField, Required] private Transform m_hitPoint;
    [SerializeField] private float m_speedDamping = 0.12f;

    private AnimatorOverrideController m_overrideController;
    private float m_targetLocomotionSpeed;
    private float m_idlePlaybackSpeed;
    private float m_runPlaybackSpeed;

    public Animator Animator => m_animator;
    public CharacterAnimationSet AnimationSet => m_animationSet;
    public Transform CastOrigin => m_castOrigin;
    public Transform HitPoint => m_hitPoint;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        if (m_overrideController != null)
            return;

        m_overrideController = BuildOverrideController();
        m_animator.runtimeAnimatorController = m_overrideController;
        m_idlePlaybackSpeed = m_animationSet.IdleSpeed;
        m_runPlaybackSpeed = m_animationSet.RunSpeed;
    }

    public void SetLocomotionSpeed(float _normalizedSpeed)
    {
        m_targetLocomotionSpeed = _normalizedSpeed;
    }

    private void Update()
    {
        m_animator.SetFloat(SpeedParameter, m_targetLocomotionSpeed, m_speedDamping, Time.deltaTime);
        m_animator.SetFloat(LocomotionSpeedParameter, Mathf.Lerp(m_idlePlaybackSpeed, m_runPlaybackSpeed, m_animator.GetFloat(SpeedParameter)));
    }

    public override void Cleanup()
    {
        base.Cleanup();

        m_targetLocomotionSpeed = 0f;
        m_animator.SetFloat(SpeedParameter, 0f);
    }

    public IEnumerator PlayCast(CardType _cardType, CastStyle _style, Action<AnimationMove> _onImpact)
    {
        AnimationMove _move = m_animationSet.GetCastMove(_style, _cardType, out float _castSpeed);
        bool _isAttack = CharacterAnimationSet.GetBaseStyle(_cardType) == CastStyle.Attack;
        int _stateHash = _isAttack ? CastAttackState : CastUtilityState;

        yield return WaitForLocomotion();

        m_overrideController[_isAttack ? CastAttackPlaceholderClipName : CastUtilityPlaceholderClipName] = _move.Clip;
        m_animator.SetFloat(CastSpeedParameter, _castSpeed);
        m_animator.SetTrigger(_isAttack ? CastAttackTrigger : CastUtilityTrigger);

        bool _entered = false;
        bool _impacted = false;
        float _safetyTimeout = _move.Clip.length / _castSpeed + CompletionSafetyMargin;
        float _elapsed = 0f;

        while (_elapsed < _safetyTimeout)
        {
            yield return null;
            _elapsed += Time.deltaTime;

            if (!TryGetStateTime(_stateHash, out float _normalizedTime))
            {
                if (_entered)
                    break;

                continue;
            }

            _entered = true;

            if (!_impacted && _normalizedTime >= _move.ImpactTime)
            {
                _impacted = true;
                _onImpact(_move);
            }

            if (_normalizedTime >= 1f)
                break;
        }

        if (!_entered)
            this.LogError($"The Animator never entered its cast state for move '{_move.name}'. Applying the card anyway so the action queue doesn't stall.");

        if (!_impacted)
            _onImpact(_move);
    }

    public IEnumerator PlayDeath()
    {
        float _deathSpeed = m_animationSet.DeathSpeed;

        m_animator.SetFloat(DeathSpeedParameter, _deathSpeed);
        m_animator.Play(DeathState, 0, 0f);
        yield return new WaitForSeconds(m_animationSet.Death.length / _deathSpeed);
    }

    private IEnumerator WaitForLocomotion()
    {
        float _elapsed = 0f;

        while (m_animator.IsInTransition(0) || m_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != LocomotionState)
        {
            if (_elapsed > MaxWaitForLocomotion)
            {
                this.LogError("The Animator did not return to Locomotion before the next cast. Starting the cast anyway.");
                yield break;
            }

            _elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool TryGetStateTime(int _stateHash, out float _normalizedTime)
    {
        if (m_animator.IsInTransition(0))
        {
            AnimatorStateInfo _next = m_animator.GetNextAnimatorStateInfo(0);

            if (_next.shortNameHash == _stateHash)
            {
                _normalizedTime = _next.normalizedTime;
                return true;
            }
        }

        AnimatorStateInfo _current = m_animator.GetCurrentAnimatorStateInfo(0);
        _normalizedTime = _current.normalizedTime;
        return _current.shortNameHash == _stateHash;
    }

    private AnimatorOverrideController BuildOverrideController()
    {
        AnimatorOverrideController _overrideController = new AnimatorOverrideController(m_controller);

        Dictionary<string, AnimationClip> _clipsByPlaceholder = new Dictionary<string, AnimationClip>
        {
            { IdlePlaceholderClipName, m_animationSet.Idle },
            { RunPlaceholderClipName, m_animationSet.Run },
            { DeathPlaceholderClipName, m_animationSet.Death },
            { CastAttackPlaceholderClipName, m_animationSet.GetCastMove(CastStyle.Attack, CardType.Attack).Clip },
            { CastUtilityPlaceholderClipName, m_animationSet.GetCastMove(CastStyle.Utility, CardType.Skill).Clip }
        };

        List<KeyValuePair<AnimationClip, AnimationClip>> _overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(_overrideController.overridesCount);
        _overrideController.GetOverrides(_overrides);

        HashSet<string> _found = new HashSet<string>();

        for (int _i = 0; _i < _overrides.Count; _i++)
        {
            AnimationClip _original = _overrides[_i].Key;

            if (!_clipsByPlaceholder.TryGetValue(_original.name, out AnimationClip _clip))
                continue;

            _overrides[_i] = new KeyValuePair<AnimationClip, AnimationClip>(_original, _clip);
            _found.Add(_original.name);
        }

        foreach (string _placeholder in _clipsByPlaceholder.Keys)
        {
            if (!_found.Contains(_placeholder))
                throw new InvalidOperationException($"'{m_controller.name}' has no state using a clip named '{_placeholder}'. A placeholder clip was renamed or removed.");
        }

        _overrideController.ApplyOverrides(_overrides);
        return _overrideController;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        m_animator = GetComponentInChildren<Animator>(true);
    }

    private void OnValidate()
    {
        if (m_animator == null)
            m_animator = GetComponentInChildren<Animator>(true);
    }
#endif
}
