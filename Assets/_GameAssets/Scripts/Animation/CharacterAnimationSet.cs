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
    public float IdleSpeed => ResolveClipOwner(_set => _set.m_idle, "Idle").m_idleSpeed;
    public AnimationClip Run => ResolveClipOwner(_set => _set.m_run, "Run").m_run;
    public float RunSpeed => ResolveClipOwner(_set => _set.m_run, "Run").m_runSpeed;
    public AnimationClip Death => ResolveClipOwner(_set => _set.m_death, "Death").m_death;
    public float DeathSpeed => ResolveClipOwner(_set => _set.m_death, "Death").m_deathSpeed;

    public static CastStyle GetBaseStyle(CardType _cardType)
    {
        return _cardType == CardType.Attack ? CastStyle.Attack : CastStyle.Utility;
    }

    public AnimationMove GetCastMove(CastStyle _style, CardType _cardType)
    {
        CastStyle _baseStyle = GetBaseStyle(_cardType);
        CastStyle _wantedStyle = _style == CastStyle.Default ? _baseStyle : _style;

        if (TryFindCastMove(_wantedStyle, out AnimationMove _move, out _))
            return _move;

        if (TryFindCastMove(_baseStyle, out _move, out _))
            return _move;

        throw new InvalidOperationException($"Animation set '{name}' has no '{_baseStyle}' move, and neither does any of its parents.");
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
