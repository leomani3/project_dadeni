using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Playable;

namespace Utils
{
    [RequireComponent(typeof(Transform))]
    public class IRotator : MonoBehaviour, Playable.IPlayable
    {
        [Title("Settings")]
        public PlayFlags PlayFlags { get; } = PlayFlags.Manual;
        [SerializeField] private Vector3 speed = new Vector3(0, 90f, 0);
        [SerializeField] private Vector3 m_interactSpeed = new Vector3(0, 180f, 0);
        [SerializeField] private float m_lerpSpeed = .1f;


        [TitleGroup("Variables")]
        private Vector3 m_currentSpeed;

        private void Start()
        {
            m_currentSpeed = speed;
        }

        void Update()
        {
            transform.Rotate(m_currentSpeed * Time.deltaTime);
            m_currentSpeed = Vector3.Lerp(m_currentSpeed, speed, m_lerpSpeed * Time.deltaTime);
        }

        public void Play()
        {
            // this.Log("Interact called.");
            m_currentSpeed = m_interactSpeed;
        }
    }
}