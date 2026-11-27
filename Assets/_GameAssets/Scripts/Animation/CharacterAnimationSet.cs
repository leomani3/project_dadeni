using System;
using System.Collections.Generic;
using Deckbuilder.Cards;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimSet_New", menuName = "Deckbuilder/Animation/Character Animation Set")]
public class CharacterAnimationSet : ScriptableObject
{
    private const int MaxParentDepth = 16;

    [Serializable]
    public struct CastEntry
    {
        public CastStyle style;
        public AnimationMove move;
        [Min(0.05f)] public float speed;
    }

    [SerializeField] private CharacterAnimationSet m_parent;
    [SerializeField] private AnimationClip m_idle;
    [SerializeField, Min(0.05f)] private float m_idleSpeed = 1f;
    [SerializeField] private AnimationClip m_run;
    [SerializeField, Min(0.05f)] private float m_runSpeed = 1f;
    [SerializeField] private AnimationClip m_death;
    [SerializeField, Min(0.05f)] private float m_deathSpeed = 1f;
    [SerializeField] private List<CastEntry> m_casts = new List<CastEntry>();

    public CharacterAnimationSet Parent => m_parent;
    public AnimationClip Idle => ResolveClipOwner(_set => _set.m_idle, "Idle").m_idle;
    public float IdleSpeed => m_idleSpeed;
    public AnimationClip Run => ResolveClipOwner(_set => _set.m_run, "Run").m_run;
    public float RunSpeed => m_runSpeed;
    public AnimationClip Death => ResolveClipOwner(_set => _set.m_death, "Death").m_death;
    public float DeathSpeed => m_deathSpeed;

    public static CastStyle GetBaseStyle(CardType _cardType)
    {
        return _cardType == CardType.Attack ? CastStyle.Attack : CastStyle.Utility;
    }

    public AnimationMove GetCastMove(CastStyle _style, CardType _cardType)
    {
        return GetCastMove(_style, _cardType, out _);
    }

    public AnimationMove GetCastMove(CastStyle _style, CardType _cardType, out float _speed)
    {
        CastStyle _baseStyle = GetBaseStyle(_cardType);
        CastStyle _playedStyle = _style == CastStyle.Default ? _baseStyle : _style;

        if (!TryFindCastMove(_playedStyle, out AnimationMove _move, out _))
        {
            _playedStyle = _baseStyle;

            if (!TryFindCastMove(_baseStyle, out _move, out _))
                throw new InvalidOperationException($"Animation set '{name}' has no '{_baseStyle}' move, and neither does any of its parents.");
        }

        _speed = GetCastSpeed(_playedStyle);
        return _move;
    }

    public float GetCastSpeed(CastStyle _style)
    {
        foreach (CastEntry _entry in m_casts)
        {
            if (_entry.style == _style)
                return _entry.speed;
        }

        return 1f;
    }

    public bool TryFindCastMove(CastStyle _style, out AnimationMove _move, out CharacterAnimationSet _owner)
    {
        int _depth = 0;

        for (CharacterAnimationSet _set = this; _set != null; _set = _set.m_parent)
        {
            if (++_depth > MaxParentDepth)
                throw new InvalidOperationException($"Animation set '{name}' has a parent loop.");

            foreach (CastEntry _entry in _set.m_casts)
            {
                if (_entry.style != _style || _entry.move == null)
                    continue;

                _move = _entry.move;
                _owner = _set;
                return true;
            }
        }

        _move = null;
        _owner = null;
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int _i = 0; _i < m_casts.Count; _i++)
        {
            if (m_casts[_i].speed > 0f)
                continue;

            CastEntry _entry = m_casts[_i];
            _entry.speed = 1f;
            m_casts[_i] = _entry;
        }
    }
#endif

    private CharacterAnimationSet ResolveClipOwner(Func<CharacterAnimationSet, AnimationClip> _select, string _slotName)
    {
        int _depth = 0;

        for (CharacterAnimationSet _set = this; _set != null; _set = _set.m_parent)
        {
            if (++_depth > MaxParentDepth)
                throw new InvalidOperationException($"Animation set '{name}' has a parent loop.");

            if (_select(_set) != null)
                return _set;
        }

        throw new InvalidOperationException($"Animation set '{name}' has no '{_slotName}' clip, and neither does any of its parents.");
    }
}
