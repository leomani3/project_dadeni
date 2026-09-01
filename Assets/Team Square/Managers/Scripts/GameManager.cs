using Sirenix.OdinInspector;
using Utils.UI;
using System.Threading.Tasks;
using UnityEngine;
using System;
using Lean.Pool;
using MyBox;
using UnityEngine.SceneManagement;

public partial class GameManager : Singleton<GameManager>
{
    public Action OnRunStart;
    public Action OnRunEnd;
    
    [TitleGroup("Dependencies")]
    private GameConfig m_gameConfig;
    private GameData m_gameData;
    
    [TitleGroup("Settings")]
    [SerializeField] private float m_resetDelay = 3;
    [SerializeField] private float m_startDelay = 3;
    
    private bool m_isPaused = false;
    public bool IsPaused => m_isPaused;
    
    void Start()
    {
        m_gameConfig = GameConfig.Instance;
        m_gameData = GameData.Instance;
        
        TutorialManager.Instance.Init();
    
        SoundManager.Instance.PlayMusic(SoundKeys.music);
        SoundManager.Instance.PlayAmbient(SoundKeys.ambient);
    }
    
    [Button]
    public async void ResetRun()
    {
        CameraController.Instance.SetControl(false);
    
    
        FadeManager.Instance.FadeIn(() =>
        {
            DespawnPooledObjectAndTuto();
    
            foreach (CurrencyAsset currency in m_gameConfig.gameSettings.resetedCurrency)
            {
                m_gameData.DepleteCurrency(currency);
            }
            m_gameData.ResetRun();
            
        }, m_resetDelay);
    }
    
    public void StartRun()
    {
        SetPause(false);
        
        CameraController.Instance.SetControl(false);
    
        DespawnPooledObjectAndTuto();
    
        if (SceneManager.GetActiveScene().name == "MainScene")
        {
            CameraController.Instance.SetControl(true);
            OnRunStart?.Invoke();
        }
    }
    
    private void DespawnPooledObjectAndTuto()
    {
        LeanPool.DespawnAll();
        TutorialUIManager.Instance.DespawnAllTutos();
    }
    
    public void SetPause(bool paused)
    {
        m_isPaused = paused;
    
        if (CameraController.Instance != null)
            CameraController.Instance.SetControl(!paused);
    }
}