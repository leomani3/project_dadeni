using UnityEngine;
using Utils;
using Utils.Playable;

public class RotatorPlayable : MonoBehaviour, IPlayable
{
    [SerializeField] private IRotator[] m_rotators;
    [SerializeField] private PlayFlags m_playFlags = PlayFlags.Manual;
    public PlayFlags PlayFlags => m_playFlags;

    public void Play()
    {
        if (m_rotators != null)
        {
            foreach (var rotator in m_rotators)
            {
                if (rotator != null)
                    rotator.Play();
            }
        }
    }
}
