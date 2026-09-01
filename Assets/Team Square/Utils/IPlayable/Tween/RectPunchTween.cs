using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Utils;
using System.Collections;
using System;


namespace Utils.Playable
{
    public class RectPunchTween : MonoBehaviour, IPlayable
    {
        [TitleGroup("Dependencies")]
        [SerializeField, Required] private RectTransform m_rectTransform;
        [TitleGroup("Settings")]
        public PlayFlags PlayFlags => PlayFlags.Manual;
        [Obsolete("AutoStart is now handled by the PlayWhen system. Set this to false and use the appropriate PlayWhen flag to control when the punch occurs.")]
        [SerializeField] private bool m_autoStart = true;
        [Space]
        [SerializeField] private float m_punchDuration = 1f;
        [SerializeField] private float m_punchScale = 1.1f;
        [Space]
        [SerializeField] private Ease m_ease = Ease.InOutSine;
        [SerializeField] private float m_easeAmplitude = 0.5f;
        [SerializeField] private float m_easePeriod = 1f;
        [Space]
        [SerializeField] private int m_loops = 1;
        [SerializeField] private float m_loopDelay = 0f;

        private Tween m_tween;
        private Coroutine m_loopDelayCoroutine;

        void Start()
        {
            if (m_autoStart)
                Play();
        }

        [Button]
        public void Play()
        {
            // this.Log("StartPunch");

            /// Reset
            if (m_tween != null && m_tween.IsActive())
                m_tween.Kill();
            StopAllCoroutines();

            /// New Punch
            if (m_loopDelay > 0)
            {
                m_loopDelayCoroutine = StartCoroutine(LoopDelayCR());
            }
            else
                m_tween = m_rectTransform.DOPunchScale(Vector3.one * m_punchScale, m_punchDuration)
                    .SetEase(m_ease, m_easeAmplitude, m_easePeriod)
                    .SetLoops(m_loops, LoopType.Restart);
        }

        private IEnumerator LoopDelayCR()
        {
            for (int i = 0; i != m_loops; i++)
            {
                m_tween = m_rectTransform.DOPunchScale(Vector3.one * m_punchScale, m_punchDuration)
                    .SetEase(m_ease, m_easeAmplitude, m_easePeriod);

                yield return new WaitForSeconds(m_loopDelay);
            }
        }
    }
}
