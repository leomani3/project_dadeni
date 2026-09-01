using Sirenix.OdinInspector;
using UnityEngine;

namespace Utils.Playable
{
    public class AnimPlayable : MonoBehaviour, IPlayable
    {
        [SerializeField, Required] private Animator m_animator;
        [SerializeField] private string m_animName = "Play";

        public void Play()
        {
            m_animator.Play(m_animName);
        }
    }
}