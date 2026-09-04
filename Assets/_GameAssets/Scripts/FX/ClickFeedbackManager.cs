using System.Collections.Generic;
using MyBox;
using UnityEngine;

public class ClickFeedbackManager : Singleton<ClickFeedbackManager>
{
    [SerializeField] private float m_groundOffset = 0.03f;
    [SerializeField] private int m_maxSimultaneousFX = 8;
    [SerializeField] private ClickFeedbackSettings m_moveSettings = new();

    private readonly List<ClickFeedbackFX> m_instances = new();
    private int m_nextReusedIndex;
    
    public ClickFeedbackFX PlayMove(Vector3 _worldPosition)
    {
        return Play(_worldPosition, m_moveSettings);
    }

    public ClickFeedbackFX Play(Vector3 _worldPosition, ClickFeedbackSettings _settings)
    {
        if (_settings == null)
            return null;

        ClickFeedbackFX _fx = GetAvailableFX();
        _fx.Play(_worldPosition + Vector3.up * m_groundOffset, _settings);

        return _fx;
    }

    private ClickFeedbackFX GetAvailableFX()
    {
        foreach (ClickFeedbackFX _fx in m_instances)
        {
            if (!_fx.IsPlaying)
                return _fx;
        }

        if (m_instances.Count < Mathf.Max(1, m_maxSimultaneousFX))
            return CreateFX();

        m_nextReusedIndex = (m_nextReusedIndex + 1) % m_instances.Count;
        return m_instances[m_nextReusedIndex];
    }

    private ClickFeedbackFX CreateFX()
    {
        GameObject _fxObject = new($"ClickFeedbackFX_{m_instances.Count}");
        _fxObject.transform.SetParent(transform, false);
        _fxObject.SetActive(false);

        ClickFeedbackFX _fx = _fxObject.AddComponent<ClickFeedbackFX>();
        m_instances.Add(_fx);

        return _fx;
    }
}
