using UnityEngine;

namespace Utils.Playable
{
    public class VFXPlayable : MonoBehaviour, IPlayable
    {
        [SerializeField] private ParticleSystem[] m_interactVFX;
        [SerializeField] private PlayFlags m_playFlags = PlayFlags.Manual;
        public PlayFlags PlayFlags => m_playFlags;

        public void Play()
        {
            if (m_interactVFX != null)
            {
                foreach (var vfx in m_interactVFX)
                {
                    if (vfx != null)
                        vfx.Play();
                }
            }
        }
    }
}