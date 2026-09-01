using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Tracker : MonoBehaviour
    {
        
        [SerializeField] private Transform m_trackedT;
        [SerializeField] private Vector3 m_positionOffset;

        private Transform m_transform;

        void Awake()
        {
            m_transform = transform;
        }

        // Update is called once per frame
        void Update()
        {
            m_transform.position = m_trackedT.position + m_positionOffset;
        }
    }
}
