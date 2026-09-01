using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Utils;

namespace Utils.Playable
{
    public class RectBreathingTween : MonoBehaviour, IPlayable
    {
        [TitleGroup("Dependencies")]
        [SerializeField, Required] private RectTransform m_rectTransform;
        [TitleGroup("Settings")]
        [SerializeField] private bool m_autoStart = true;
        [SerializeField] private float m_breathingDuration = 1f;
        [SerializeField] private float m_breathingScale = 1.1f;

        void Start()
        {
            if (m_autoStart)
                Play();
        }

        public void Play()
        {
            // this.Log("StartBreathing");
            m_rectTransform.DOScale(m_breathingScale, m_breathingDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
}
