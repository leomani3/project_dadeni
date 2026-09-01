using System;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private float m_autoSaveInterval = 5;

    private float m_timeCpt;
    
    void Awake()
    {
        GameData.Instance.Init();
    }

    private void Update()
    {
        m_timeCpt += Time.deltaTime;
        if (m_timeCpt > m_autoSaveInterval && GameData.Instance.isDirty)
        {
            GameData.Instance.Save();
            m_timeCpt = 0;
        }
    }

    private void OnApplicationQuit()
    {
        GameData.Instance.Save();
    }
}